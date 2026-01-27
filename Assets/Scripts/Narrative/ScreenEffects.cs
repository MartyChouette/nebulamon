using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    /// <summary>
    /// Screen effects for narrative sequences.
    /// Provides flash, fade, shake, and other screen-level effects.
    /// </summary>
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [Header("Flash Effect")]
        [SerializeField] private Image flashOverlay;
        [SerializeField] private Color flashColor = Color.white;

        [Header("Fade Effect")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Shake Effect")]
        [SerializeField] private float defaultShakeIntensity = 10f;
        [SerializeField] private float defaultShakeDuration = 0.3f;
        [SerializeField] private Transform shakeTarget; // Usually the main camera or canvas

        private Vector3 _originalShakePosition;
        private Coroutine _shakeCoroutine;
        private Coroutine _flashCoroutine;
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Cache original position for shake
            if (shakeTarget != null)
                _originalShakePosition = shakeTarget.localPosition;

            // Initialize overlays as invisible
            if (flashOverlay != null)
            {
                flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
                flashOverlay.gameObject.SetActive(true);
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
                fadeOverlay.gameObject.SetActive(true);
            }
        }

        #region Flash

        /// <summary>
        /// Quick flash effect (like getting hit or a bright attack).
        /// </summary>
        public void Flash(float duration = 0.2f)
        {
            Flash(flashColor, duration);
        }

        /// <summary>
        /// Flash with a specific color.
        /// </summary>
        public void Flash(Color color, float duration = 0.2f)
        {
            if (flashOverlay == null) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(FlashRoutine(color, duration));
        }

        /// <summary>
        /// Coroutine version for yield return.
        /// </summary>
        public IEnumerator FlashAsync(float duration = 0.2f)
        {
            return FlashAsync(flashColor, duration);
        }

        public IEnumerator FlashAsync(Color color, float duration = 0.2f)
        {
            if (flashOverlay == null) yield break;

            // Quick flash in
            flashOverlay.color = new Color(color.r, color.g, color.b, 1);

            // Fade out
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1 - (elapsed / duration);
                flashOverlay.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            flashOverlay.color = new Color(color.r, color.g, color.b, 0);
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            yield return FlashAsync(color, duration);
            _flashCoroutine = null;
        }

        #endregion

        #region Fade

        /// <summary>
        /// Fade to black (or fade color).
        /// </summary>
        public IEnumerator FadeOut(float duration = 1f)
        {
            return FadeToAlpha(1f, duration);
        }

        /// <summary>
        /// Fade from black (or fade color) to clear.
        /// </summary>
        public IEnumerator FadeIn(float duration = 1f)
        {
            return FadeToAlpha(0f, duration);
        }

        /// <summary>
        /// Fade to a specific alpha.
        /// </summary>
        public IEnumerator FadeToAlpha(float targetAlpha, float duration)
        {
            if (fadeOverlay == null) yield break;

            float startAlpha = fadeOverlay.color.a;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, targetAlpha);
        }

        /// <summary>
        /// Immediately set fade state.
        /// </summary>
        public void SetFade(float alpha)
        {
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
        }

        #endregion

        #region Shake

        /// <summary>
        /// Shake the screen.
        /// </summary>
        public void Shake(float duration = -1, float intensity = -1)
        {
            if (shakeTarget == null) return;

            if (duration < 0) duration = defaultShakeDuration;
            if (intensity < 0) intensity = defaultShakeIntensity;

            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);

            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        /// <summary>
        /// Coroutine version for yield return.
        /// </summary>
        public IEnumerator ShakeAsync(float duration = -1, float intensity = -1)
        {
            if (shakeTarget == null) yield break;

            if (duration < 0) duration = defaultShakeDuration;
            if (intensity < 0) intensity = defaultShakeIntensity;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Decreasing intensity over time
                float currentIntensity = intensity * (1 - elapsed / duration);

                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f) * currentIntensity,
                    Random.Range(-1f, 1f) * currentIntensity,
                    0
                );

                shakeTarget.localPosition = _originalShakePosition + offset;
                yield return null;
            }

            shakeTarget.localPosition = _originalShakePosition;
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            yield return ShakeAsync(duration, intensity);
            _shakeCoroutine = null;
        }

        /// <summary>
        /// Stop any active shake.
        /// </summary>
        public void StopShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            if (shakeTarget != null)
                shakeTarget.localPosition = _originalShakePosition;
        }

        #endregion

        #region Combo Effects

        /// <summary>
        /// Impact effect - flash and shake together.
        /// </summary>
        public IEnumerator Impact(float shakeDuration = 0.2f, float shakeIntensity = 15f)
        {
            Flash(Color.white, 0.1f);
            yield return ShakeAsync(shakeDuration, shakeIntensity);
        }

        /// <summary>
        /// Dramatic zoom effect (requires camera).
        /// </summary>
        public IEnumerator DramaticPause(float duration = 0.5f)
        {
            // Flash
            if (flashOverlay != null)
            {
                flashOverlay.color = new Color(1, 1, 1, 0.5f);
            }

            yield return new WaitForSeconds(duration);

            // Fade flash out
            if (flashOverlay != null)
            {
                float elapsed = 0;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.5f, 0, elapsed / 0.2f);
                    flashOverlay.color = new Color(1, 1, 1, alpha);
                    yield return null;
                }
                flashOverlay.color = new Color(1, 1, 1, 0);
            }
        }

        #endregion
    }
}
