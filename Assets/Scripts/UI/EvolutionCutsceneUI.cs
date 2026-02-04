using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class EvolutionCutsceneUI : MonoBehaviour
    {
        public static EvolutionCutsceneUI Instance { get; private set; }

        [Header("Display")]
        public GameObject panel;
        public Image monsterImage;
        public TMP_Text evolutionText;
        public Button continueButton;

        [Header("Timing")]
        public float flashDuration = 0.3f;
        public float shakeDuration = 0.4f;
        public float shakeIntensity = 15f;
        public float holdDuration = 1.5f;

        private bool _waitingForInput;

        private void Awake()
        {
            Instance = this;
            if (panel) panel.SetActive(false);
        }

        /// <summary>
        /// Play the full evolution cutscene. Yield on this in a coroutine.
        /// </summary>
        public IEnumerator PlayEvolution(MonsterDefinition oldDef, MonsterDefinition newDef)
        {
            if (panel) panel.SetActive(true);

            // Show old monster
            if (monsterImage && oldDef != null)
                monsterImage.sprite = oldDef.battleSprite;

            if (evolutionText)
                evolutionText.text = $"{oldDef?.displayName ?? "???"} is evolving...";

            yield return new WaitForSeconds(holdDuration);

            // Flash + shake
            var fx = ScreenEffects.Instance;
            if (fx != null)
            {
                fx.Flash(Color.white, flashDuration);
                yield return fx.ShakeAsync(shakeDuration, shakeIntensity);
            }
            else
            {
                yield return new WaitForSeconds(flashDuration + shakeDuration);
            }

            // Swap to new sprite
            if (monsterImage && newDef != null)
                monsterImage.sprite = newDef.battleSprite;

            if (evolutionText)
                evolutionText.text = $"It became {newDef?.displayName ?? "???"}!";

            // Wait for player input
            _waitingForInput = true;
            if (continueButton)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => _waitingForInput = false);
            }

            while (_waitingForInput)
                yield return null;

            if (continueButton) continueButton.gameObject.SetActive(false);
            if (panel) panel.SetActive(false);
        }
    }
}
