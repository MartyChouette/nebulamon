using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nebula
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Overlay")]
        [Tooltip("A full-screen Image used for tinting. Should cover entire canvas.")]
        public Image tintOverlay;

        [Header("Gradient (0=midnight, 0.5=noon, 1=midnight)")]
        public Gradient dayNightGradient;

        [Header("Settings")]
        [Range(0f, 0.5f)] public float maxAlpha = 0.25f;

        [Header("Excluded Scenes")]
        [Tooltip("Scenes where tinting is disabled (e.g., Battle, Menu).")]
        public string[] excludedScenes = { "BattleScreen", "Menu" };

        private void Awake()
        {
            if (dayNightGradient == null || dayNightGradient.colorKeys.Length == 0)
            {
                dayNightGradient = new Gradient();
                dayNightGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0f),    // midnight - blue
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),    // sunrise - orange
                        new GradientColorKey(Color.white, 0.5f),                     // noon - clear
                        new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.75f),    // sunset - orange
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1f),     // midnight again
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.5f, 0.25f),
                        new GradientAlphaKey(0f, 0.5f),
                        new GradientAlphaKey(0.5f, 0.75f),
                        new GradientAlphaKey(1f, 1f),
                    }
                );
            }
        }

        private void Update()
        {
            if (tintOverlay == null) return;

            if (!GameSettings.GetBool(GameSettings.Keys.DayNightEnabled))
            {
                tintOverlay.color = Color.clear;
                return;
            }

            // Check excluded scenes
            string currentScene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < excludedScenes.Length; i++)
            {
                if (currentScene == excludedScenes[i])
                {
                    tintOverlay.color = Color.clear;
                    return;
                }
            }

            // Map real time to 0..1 fraction of day
            var now = System.DateTime.Now;
            float fraction = (now.Hour * 3600f + now.Minute * 60f + now.Second) / 86400f;

            Color tint = dayNightGradient.Evaluate(fraction);
            tint.a = tint.a * maxAlpha;
            tintOverlay.color = tint;
        }
    }
}
