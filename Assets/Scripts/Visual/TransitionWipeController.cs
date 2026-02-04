using System.Collections;
using UnityEngine;

namespace Nebula
{
    public class TransitionWipeController : MonoBehaviour
    {
        public static TransitionWipeController Instance { get; private set; }

        public enum WipeType { Iris = 0, Blinds = 1, ColumnDissolve = 2 }

        [Header("Shader")]
        public Shader wipeShader;

        [Header("Defaults")]
        public Color wipeColor = Color.black;
        public int bandCount = 8;
        public int columnCount = 10;

        private Material _mat;
        private RenderTexture _wipeRT;
        private bool _wipeActive;
        private float _currentProgress;
        private int _currentWipeType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Play a wipe transition. isIn = true means wipe reveals (progress 1->0), false means wipe covers (0->1).
        /// </summary>
        public IEnumerator PlayWipe(WipeType type, float duration, bool isIn)
        {
            if (wipeShader == null) yield break;
            if (_mat == null) _mat = new Material(wipeShader);

            _currentWipeType = (int)type;
            _wipeActive = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _currentProgress = isIn ? (1f - t) : t;
                yield return null;
            }

            _currentProgress = isIn ? 0f : 1f;

            // If we wiped out (covered screen), keep it until the next wipe-in
            if (isIn) _wipeActive = false;
        }

        /// <summary>
        /// Immediately clear the wipe overlay.
        /// </summary>
        public void ClearWipe()
        {
            _wipeActive = false;
            _currentProgress = 0f;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!_wipeActive || _mat == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            _mat.SetFloat("_Progress", _currentProgress);
            _mat.SetColor("_Color", wipeColor);
            _mat.SetFloat("_WipeType", _currentWipeType);
            _mat.SetFloat("_BandCount", bandCount);
            _mat.SetFloat("_ColumnCount", columnCount);

            Graphics.Blit(src, dest, _mat);
        }
    }
}
