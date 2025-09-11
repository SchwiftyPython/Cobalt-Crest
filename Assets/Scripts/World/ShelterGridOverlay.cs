using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    /// Simple debug overlay that visualizes the shelter grid as a heatmap (blue→cyan).
    /// Toggle visibility with SimConfig.showShelterOverlay or press 'G'.
    public class ShelterGridOverlay : MonoBehaviour
    {
        private RawImage img;
        private Texture2D tex;
        private ShelterGrid grid;
        private int seenStamp = -1;
        private bool visible;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            grid = ShelterGrid.Instance ?? FindObjectOfType<ShelterGrid>();
            MakeUI();
            visible = ConfigService.Instance?.Sim?.showShelterOverlay ?? false;
            ApplyVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                visible = !visible;
                ApplyVisibility();
            }

            if (!visible)
            {
                return;
            }

            if (grid == null)
            {
                grid = ShelterGrid.Instance;
            }

            if (grid == null)
            {
                return;
            }

            if (tex == null || tex.width != grid.Width || tex.height != grid.Height)
            {
                RebuildTexture();
            }

            if (seenStamp != grid.FrameStamp)
            {
                seenStamp = grid.FrameStamp;
                RefreshPixels();
            }
        }

        private void MakeUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }

            var panel = new GameObject("ShelterOverlay");
            panel.transform.SetParent(canvas.transform, false);
            img = panel.AddComponent<RawImage>();
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.color = Color.white;
            RebuildTexture();
        }

        private void ApplyVisibility()
        {
            if (img != null)
            {
                img.enabled = visible;
            }
        }

        private void RebuildTexture()
        {
            if (grid == null)
            {
                return;
            }

            tex = new Texture2D(Mathf.Max(2, grid.Width), Mathf.Max(2, grid.Height), TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            
            img.texture = tex;
            seenStamp = -1;
        }

        private void RefreshPixels()
        {
            if (tex == null || grid == null)
            {
                return;
            }

            var cols = new Color32[tex.width * tex.height];

            // draw y up
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    // sample directly from grid by reconstructing world pos at cell centers
                    var d = SampleCell(x, y);

                    // blue (low) → cyan/white (high)
                    var c = Color.Lerp(new Color(0.05f, 0.15f, 0.9f, 0.0f), new Color(0.0f, 1.0f, 1.0f, 0.0f), d);
                    c.a = Mathf.Lerp(0.08f, 0.35f, d); // alpha by density
                    cols[y * tex.width + x] = (Color32)c;
                }
            }

            tex.SetPixels32(cols);
            tex.Apply(false);
        }

        private float SampleCell(int x, int y)
        {
            // direct access to grid's current array via Sample01 at cell center
            if (grid == null)
            {
                return 0f;
            }

            var wx = Mathf.Lerp(grid.WorldMin.x, grid.WorldMax.x, x / Mathf.Max(1f, (grid.Width - 1f)));
            var wy = Mathf.Lerp(grid.WorldMin.y, grid.WorldMax.y, y / Mathf.Max(1f, (grid.Height - 1f)));
            return grid.Sample01(new Vector2(wx, wy));
        }
    }
}