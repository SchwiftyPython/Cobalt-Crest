using Audio;
using Entities;
using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ensures the Phase 1 scene is fully functional without manual setup.
/// Creates managers, UI, and initial spawns at runtime.
/// </summary>
public static class AutoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindObjectOfType<EcosystemManager>() == null)
        {
            new GameObject("EcosystemManager").AddComponent<EcosystemManager>();
        }

        if (Object.FindObjectOfType<EnvironmentManager>() == null)
        {
            new GameObject("EnvironmentManager").AddComponent<EnvironmentManager>();
        }

        EnsureCamera();                

        if (Object.FindObjectOfType<Canvas>() == null)
        {
            UICreator.CreateUI();
        }

        if (Producer.All.Count == 0 && Herbivore.All.Count == 0 && Carnivore.All.Count == 0)
        {
            Spawner.SpawnInitial();
        }
        
        if ((ConfigService.Instance?.Sim?.enableAudio ?? true) && Object.FindObjectOfType<AudioService>() == null)
        {
            new GameObject("AudioService").AddComponent<AudioService>();
        }
    }

    private static void EnsureCamera()
    {
        var cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);

            Vector2 min = Spawner.WorldMin, max = Spawner.WorldMax;
            var center = (min + max) * 0.5f;
            cam.transform.position = new Vector3(center.x, center.y, -10f);

            var aspect = Mathf.Max(0.1f, (float)Screen.width / Mathf.Max(1, Screen.height));
            var halfH = (max.y - min.y) * 0.5f + 1f;
            var halfW = (max.x - min.x) * 0.5f + 1f;
            cam.orthographicSize = Mathf.Max(halfH, halfW / aspect);

            if (Object.FindObjectOfType<AudioListener>() == null)
            {
                camGO.AddComponent<AudioListener>();
            }
        }
        else
        {
            cam.orthographic = true;
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreInit()
    {
        if (Object.FindObjectOfType<ConfigService>() == null)
        {
            new GameObject("ConfigService").AddComponent<ConfigService>();
        }
    }
}

/// <summary>Creates the Canvas, EventSystem, Time Controls, and Population Graph.</summary>
public static class UICreator
{
    static Font GetBuiltinUiFont()
    {
        // Unity 2022/2023+: Arial.ttf is no longer valid. Use LegacyRuntime.ttf.
        // Fallback: try Arial for older editors just in case.
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    
    public static void CreateUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Time Controls Panel
        var panel = new GameObject("TimeControls");
        panel.transform.SetParent(canvasGO.transform, false);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0,0,0,0.3f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(16, 16);
        rt.sizeDelta = new Vector2(240, 60);

        CreateButton(panel.transform, "Pause", new Vector2(40, 30), () => EcosystemManager.SimulationSpeed = 0f);
        CreateButton(panel.transform, "Play", new Vector2(120, 30), () => EcosystemManager.SimulationSpeed = 1f);
        CreateButton(panel.transform, "Fast", new Vector2(200, 30), () => EcosystemManager.SimulationSpeed = 5f);

        // Population Graph (bottom-right)
        var graphPanel = new GameObject("PopulationGraphPanel");
        graphPanel.transform.SetParent(canvasGO.transform, false);
        var gpRt = graphPanel.AddComponent<RectTransform>();
        var gpImg = graphPanel.AddComponent<Image>();
        gpImg.color = new Color(0,0,0,0.3f);
        gpRt.anchorMin = new Vector2(1, 0);
        gpRt.anchorMax = new Vector2(1, 0);
        gpRt.pivot = new Vector2(1, 0);
        gpRt.anchoredPosition = new Vector2(-16, 16);
        gpRt.sizeDelta = new Vector2(480, 180);

        var raw = new GameObject("Graph").AddComponent<RawImage>();
        raw.transform.SetParent(graphPanel.transform, false);
        var rawRt = raw.GetComponent<RectTransform>();
        rawRt.anchorMin = new Vector2(0, 0);
        rawRt.anchorMax = new Vector2(1, 1);
        rawRt.offsetMin = new Vector2(8, 8);
        rawRt.offsetMax = new Vector2(-8, -8);

        var graph = raw.gameObject.AddComponent<PopulationGraph>();
        graph.width = 460;
        graph.sampleInterval = 0.25f;

        // Legend (very simple)
        CreateLegend(graphPanel.transform);
    }

    private static void CreateLegend(Transform parent)
    {
        var legend = new GameObject("Legend");
        legend.transform.SetParent(parent, false);
        var rt = legend.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(8, -8);
        rt.sizeDelta = new Vector2(200, 60);

        LegendRow(legend.transform, new Vector2(0, 0), Color.green, "Producers");
        LegendRow(legend.transform, new Vector2(0, -20), Color.cyan, "Herbivores");
        LegendRow(legend.transform, new Vector2(0, -40), Color.red, "Carnivores");
    }

    private static void LegendRow(Transform parent, Vector2 pos, Color color, string label)
    {
        var row = new GameObject(label + "_Row");
        row.transform.SetParent(parent, false);
        var rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,1);
        rt.anchorMax = new Vector2(0,1);
        rt.pivot = new Vector2(0,1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180, 18);

        var swatch = new GameObject("Swatch").AddComponent<Image>();
        swatch.transform.SetParent(row.transform, false);
        var srt = swatch.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0,0.5f);
        srt.anchorMax = new Vector2(0,0.5f);
        srt.pivot = new Vector2(0,0.5f);
        srt.anchoredPosition = new Vector2(8, 0);
        srt.sizeDelta = new Vector2(14, 14);
        swatch.color = color;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(row.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0,0.5f);
        trt.anchorMax = new Vector2(0,0.5f);
        trt.pivot = new Vector2(0,0.5f);
        trt.anchoredPosition = new Vector2(28, 0);
        trt.sizeDelta = new Vector2(140, 16);
        var text = txtGO.AddComponent<Text>();
        text.text = label;
        text.font = GetBuiltinUiFont();
        text.fontSize = 12;
        text.color = Color.white;
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(label);
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(60, 28);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var text = txtGO.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetBuiltinUiFont();
        text.fontSize = 14;
        text.color = Color.white;
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }
}
