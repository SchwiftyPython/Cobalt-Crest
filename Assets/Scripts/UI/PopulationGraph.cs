using UnityEngine;
using UnityEngine.UI;

/// <summary>Lightweight scrolling population graph for Producers/Herbivores/Carnivores.</summary>
[RequireComponent(typeof(RawImage))]
public class PopulationGraph : MonoBehaviour
{
    public int width = 460;
    public float sampleInterval = 0.25f;
    [Range(0.01f, 1f)] public float autoscaleLerp = 0.2f;
    public Color backgroundColor = new Color(0.09f, 0.09f, 0.09f, 1f);
    public Color gridColor = new Color(1f, 1f, 1f, 0.05f);
    public int gridYLines = 4;

    private RawImage _raw;
    private Texture2D _tex;
    private float _timer;
    private int _cursorX;
    private int _height;
    private int[] _lastY = new int[3];
    private float _displayMax = 10f;

    private void Awake()
    {
        _raw = GetComponent<RawImage>();
        var rt = (RectTransform)transform;
        _height = Mathf.Max(120, Mathf.RoundToInt(rt.rect.height));
        if (width < 64) width = 64;

        _tex = new Texture2D(width, _height, TextureFormat.RGBA32, false, false);
        _tex.wrapMode = TextureWrapMode.Clamp;
        _tex.filterMode = FilterMode.Point;

        var fill = new Color32[width * _height];
        for (int i = 0; i < fill.Length; i++) fill[i] = backgroundColor;
        _tex.SetPixels32(fill);
        _tex.Apply();

        _raw.texture = _tex;
        for (int i = 0; i < _lastY.Length; i++) _lastY[i] = -1;
        DrawGrid();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < sampleInterval) return;
        _timer = 0f;

        int prod = SafeCount("Producers");
        int herb = SafeCount("Herbivores");
        int carn = SafeCount("Carnivores");
        int currentMax = Mathf.Max(1, prod, herb, carn);
        _displayMax = Mathf.Lerp(_displayMax, Mathf.Max(currentMax, 10), autoscaleLerp);

        AdvanceColumn();
        DrawColumnBackground(_cursorX);
        Plot(0, prod, Color.green);
        Plot(1, herb, Color.cyan);
        Plot(2, carn, Color.red);
        _tex.Apply(false);
    }

    private int SafeCount(string id)
    {
        if (EcosystemManager.Instance == null) return 0;
        return EcosystemManager.Instance.GetCount(id);
    }

    private void AdvanceColumn() => _cursorX = (_cursorX + 1) % width;

    private void DrawColumnBackground(int x)
    {
        for (int y = 0; y < _height; y++) _tex.SetPixel(x, y, backgroundColor);
        if (_cursorX % 80 == 0) for (int y = 0; y < _height; y++) _tex.SetPixel(x, y, gridColor);
    }

    private void DrawGrid()
    {
        if (gridYLines <= 0) return;
        for (int i = 1; i <= gridYLines; i++)
        {
            int y = Mathf.RoundToInt((i / (float)(gridYLines + 1)) * (_height - 1));
            for (int x = 0; x < width; x++) _tex.SetPixel(x, y, gridColor);
        }
        _tex.Apply(false);
    }

    private void Plot(int idx, int count, Color color)
    {
        float norm = Mathf.Clamp01(count / Mathf.Max(1f, _displayMax));
        int y = Mathf.Clamp(Mathf.RoundToInt(norm * (_height - 1)), 0, _height - 1);
        int lastY = _lastY[idx]; if (lastY < 0) lastY = y;
        int y0 = Mathf.Min(lastY, y);
        int y1 = Mathf.Max(lastY, y);
        for (int yy = y0; yy <= y1; yy++) _tex.SetPixel(_cursorX, yy, color);
        _lastY[idx] = y;
    }
}
