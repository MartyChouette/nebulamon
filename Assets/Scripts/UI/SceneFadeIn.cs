using System.Collections;
using UnityEngine;

namespace Nebula
{
    public class SceneFadeIn : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private float fadeInTime = 0.5f;

        private void Start()
        {
            if (overlay == null) return;
            StartCoroutine(Fade(1f, 0f, fadeInTime));
        }

        private IEnumerator Fade(float from, float to, float dur)
        {
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = true;

            float t = 0f;
            dur = Mathf.Max(0.01f, dur);

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                overlay.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            overlay.alpha = to;
            overlay.blocksRaycasts = false;
            if (Mathf.Approximately(to, 0f))
                overlay.gameObject.SetActive(false);
        }
    }
}
