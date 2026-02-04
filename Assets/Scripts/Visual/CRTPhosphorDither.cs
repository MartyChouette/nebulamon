using UnityEngine;

namespace Nebula
{
    [RequireComponent(typeof(Camera))]
    public class CRTPhosphorDither : MonoBehaviour
    {
        [Header("Shader")]
        public Shader crtShader;

        [Header("Settings")]
        [Range(0f, 1f)] public float maskStrength = 0.4f;
        [Range(0f, 1f)] public float ditherStrength = 0.3f;
        [Range(0f, 1f)] public float glowStrength = 0.15f;
        [Range(1f, 8f)] public float pixelScale = 3f;
        public int maskType = 0; // 0 = aperture grille, 1 = shadow mask

        private Material _mat;

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!GameSettings.GetBool(GameSettings.Keys.CRTPhosphorDitherEnabled))
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (crtShader == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (_mat == null) _mat = new Material(crtShader);

            _mat.SetFloat("_MaskType", maskType);
            _mat.SetFloat("_MaskStrength", maskStrength);
            _mat.SetFloat("_DitherStrength", ditherStrength);
            _mat.SetFloat("_GlowStrength", glowStrength);
            _mat.SetFloat("_PixelScale", pixelScale);

            Graphics.Blit(src, dest, _mat);
        }
    }
}
