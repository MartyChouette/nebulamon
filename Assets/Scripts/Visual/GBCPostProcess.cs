using UnityEngine;

namespace Nebula
{
    [RequireComponent(typeof(Camera))]
    public class GBCPostProcess : MonoBehaviour
    {
        [Header("Shader")]
        public Shader gbcShader;

        [Header("Palette Presets")]
        public GBCPalettePreset[] presets = new GBCPalettePreset[]
        {
            new() { name = "Classic Green",
                color0 = new Color(0.06f, 0.22f, 0.06f),
                color1 = new Color(0.19f, 0.38f, 0.19f),
                color2 = new Color(0.55f, 0.67f, 0.06f),
                color3 = new Color(0.61f, 0.74f, 0.06f) },
            new() { name = "Grayscale",
                color0 = new Color(0.05f, 0.05f, 0.05f),
                color1 = new Color(0.33f, 0.33f, 0.33f),
                color2 = new Color(0.66f, 0.66f, 0.66f),
                color3 = new Color(0.95f, 0.95f, 0.95f) },
            new() { name = "Pocket",
                color0 = new Color(0.0f, 0.0f, 0.0f),
                color1 = new Color(0.33f, 0.33f, 0.24f),
                color2 = new Color(0.67f, 0.67f, 0.53f),
                color3 = new Color(0.78f, 0.82f, 0.67f) },
        };

        [System.Serializable]
        public struct GBCPalettePreset
        {
            public string name;
            public Color color0;
            public Color color1;
            public Color color2;
            public Color color3;
        }

        private Material _mat;

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!GameSettings.GetBool(GameSettings.Keys.GBCPaletteEnabled))
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (gbcShader == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (_mat == null) _mat = new Material(gbcShader);

            int idx = Mathf.Clamp(
                GameSettings.GetInt(GameSettings.Keys.GBCPaletteIndex),
                0, presets.Length - 1);

            var p = presets[idx];
            _mat.SetColor("_Color0", p.color0);
            _mat.SetColor("_Color1", p.color1);
            _mat.SetColor("_Color2", p.color2);
            _mat.SetColor("_Color3", p.color3);

            Graphics.Blit(src, dest, _mat);
        }
    }
}
