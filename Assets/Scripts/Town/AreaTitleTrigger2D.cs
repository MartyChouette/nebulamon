using System.Collections;
using UnityEngine;
using TMPro;

namespace Nebula
{
    public class AreaTitleTrigger2D : MonoBehaviour
    {
        [SerializeField] private string areaName = "ORBITAL SPACEPORT";
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TMP_Text bannerText;

        [Header("Timings")]
        [SerializeField] private float fadeIn = 0.25f;
        [SerializeField] private float hold = 1.2f;
        [SerializeField] private float fadeOut = 0.35f;

        private Coroutine _co;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        private void Start()
        {
            if (bannerGroup != null) bannerGroup.alpha = 0f;
            if (bannerGroup != null) bannerGroup.gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (bannerGroup == null || bannerText == null) yield break;

            bannerText.text = areaName;
            bannerGroup.gameObject.SetActive(true);

            yield return Fade(0f, 1f, fadeIn);
            yield return new WaitForSeconds(hold);
            yield return Fade(1f, 0f, fadeOut);

            bannerGroup.gameObject.SetActive(false);
        }

        private IEnumerator Fade(float a, float b, float dur)
        {
            float t = 0f;
            dur = Mathf.Max(0.0001f, dur);

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                bannerGroup.alpha = Mathf.Lerp(a, b, t);
                yield return null;
            }

            bannerGroup.alpha = b;
        }
    }
}
