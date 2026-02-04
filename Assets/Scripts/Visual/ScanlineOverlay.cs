using UnityEngine;

namespace Nebula
{
    [RequireComponent(typeof(Camera))]
    public class ScanlineOverlay : MonoBehaviour
    {
        [Header("Shader")]
        public Shader scanlineShader;

        [Header("Settings")]
        [Range(1f, 6f)] public float lineWidth = 2f;
        [Range(0f, 1f)] public float darkness = 0.3f;

        private Material _mat;

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!GameSettings.GetBool(GameSettings.Keys.ScanlineEnabled))
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (scanlineShader == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (_mat == null) _mat = new Material(scanlineShader);

            _mat.SetFloat("_LineWidth", lineWidth);
            _mat.SetFloat("_Darkness", darkness);

            Graphics.Blit(src, dest, _mat);
        }
    }
}
