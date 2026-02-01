using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Binds Options UI controls (that YOU build) to saved settings.
    /// Accessible-friendly behavior:
    /// - Uses SetValueWithoutNotify on load (prevents "jump" & spam)
    /// - Values persist via GameSettings (which wraps PlayerPrefs)
    /// - Audio sliders map 0..1 to dB on an AudioMixer via GameSettings
    ///
    /// Note: Unity doesn't provide true screen-reader narration out of the box,
    /// so accessibility here focuses on keyboard/gamepad navigation, clarity,
    /// and sane defaults.
    /// </summary>
    public class OptionsMenuBinder : MonoBehaviour
    {
        [Header("Audio Mixer (recommended)")]
        [SerializeField] private AudioMixer audioMixer;

        [Tooltip("Exposed params in AudioMixer")]
        [SerializeField] private string masterParam = "MasterVol";
        [SerializeField] private string battleFxParam = "BattleFXVol";
        [SerializeField] private string voiceParam = "VoiceVol";
        [SerializeField] private string textFxParam = "TextFXVol";
        [SerializeField] private string worldMusicParam = "WorldMusicVol";
        [SerializeField] private string battleMusicParam = "BattleMusicVol";

        [Header("Audio Sliders (0..1)")]
        [SerializeField] private Slider master;
        [SerializeField] private Slider battleFx;
        [SerializeField] private Slider speaking;
        [SerializeField] private Slider textFx;
        [SerializeField] private Slider worldMusic;
        [SerializeField] private Slider battleMusic;

        [Header("Optional: show numeric values beside sliders")]
        [SerializeField] private TMP_Text masterValueText;
        [SerializeField] private TMP_Text battleFxValueText;
        [SerializeField] private TMP_Text speakingValueText;
        [SerializeField] private TMP_Text textFxValueText;
        [SerializeField] private TMP_Text worldMusicValueText;
        [SerializeField] private TMP_Text battleMusicValueText;

        [Header("Modern accessibility / QoL toggles (examples)")]
        [SerializeField] private Toggle subtitles;
        [SerializeField] private Toggle reduceFlashing;
        [SerializeField] private Toggle reduceShake;
        [SerializeField] private Toggle highContrastUI;
        [SerializeField] private Toggle dyslexiaFont;
        [SerializeField] private Toggle battleAnimations;
        [SerializeField] private Toggle typeEffectivenessHints;
        [SerializeField] private Toggle autosave;

        [Header("Text settings (examples)")]
        [SerializeField] private Slider textSize;   // e.g. 1..4 whole numbers
        [SerializeField] private Slider textSpeed;  // e.g. 1..5 whole numbers
        [SerializeField] private TMP_Text textSizeValueText;
        [SerializeField] private TMP_Text textSpeedValueText;

        [Header("Optional: Fonts")]
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private TMP_FontAsset dyslexiaFriendlyFont;

        private void OnEnable()
        {
            GameSettings.InitIfNeeded();
            LoadUIFromPrefs();
            HookUIEvents();
            ApplyAll();
            GameSettings.OnSettingChanged += OnSettingChangedExternally;
        }

        private void OnDisable()
        {
            UnhookUIEvents();
            GameSettings.OnSettingChanged -= OnSettingChangedExternally;
        }

        private void OnSettingChangedExternally(string key)
        {
            LoadUIFromPrefs();
        }

        private void LoadUIFromPrefs()
        {
            SetSlider(master, GameSettings.Keys.Master01, masterValueText);
            SetSlider(worldMusic, GameSettings.Keys.WorldMusic01, worldMusicValueText);
            SetSlider(battleMusic, GameSettings.Keys.BattleMusic01, battleMusicValueText);
            SetSlider(battleFx, GameSettings.Keys.BattleFx01, battleFxValueText);
            SetSlider(speaking, GameSettings.Keys.Voice01, speakingValueText);
            SetSlider(textFx, GameSettings.Keys.TextFx01, textFxValueText);

            SetToggle(subtitles, GameSettings.Keys.SubtitlesEnabled);
            SetToggle(reduceFlashing, GameSettings.Keys.ReduceFlashing);
            SetToggle(reduceShake, GameSettings.Keys.ReduceShake);
            SetToggle(highContrastUI, GameSettings.Keys.HighContrastUI);
            SetToggle(dyslexiaFont, GameSettings.Keys.DyslexiaFont);
            SetToggle(battleAnimations, GameSettings.Keys.BattleAnims);
            SetToggle(typeEffectivenessHints, GameSettings.Keys.TypeEffectivenessHints);
            SetToggle(autosave, GameSettings.Keys.AutoSave);

            SetIntSlider(textSize, GameSettings.Keys.TextSize, textSizeValueText);
            SetIntSlider(textSpeed, GameSettings.Keys.TextSpeed, textSpeedValueText);
        }

        private void HookUIEvents()
        {
            HookSlider(master, GameSettings.Keys.Master01, masterValueText);
            HookSlider(worldMusic, GameSettings.Keys.WorldMusic01, worldMusicValueText);
            HookSlider(battleMusic, GameSettings.Keys.BattleMusic01, battleMusicValueText);
            HookSlider(battleFx, GameSettings.Keys.BattleFx01, battleFxValueText);
            HookSlider(speaking, GameSettings.Keys.Voice01, speakingValueText);
            HookSlider(textFx, GameSettings.Keys.TextFx01, textFxValueText);

            HookToggle(subtitles, GameSettings.Keys.SubtitlesEnabled);
            HookToggle(reduceFlashing, GameSettings.Keys.ReduceFlashing);
            HookToggle(reduceShake, GameSettings.Keys.ReduceShake);
            HookToggle(highContrastUI, GameSettings.Keys.HighContrastUI);
            HookToggle(dyslexiaFont, GameSettings.Keys.DyslexiaFont);
            HookToggle(battleAnimations, GameSettings.Keys.BattleAnims);
            HookToggle(typeEffectivenessHints, GameSettings.Keys.TypeEffectivenessHints);
            HookToggle(autosave, GameSettings.Keys.AutoSave);

            HookIntSlider(textSize, GameSettings.Keys.TextSize, textSizeValueText);
            HookIntSlider(textSpeed, GameSettings.Keys.TextSpeed, textSpeedValueText);
        }

        private void UnhookUIEvents()
        {
            UnhookSlider(master);
            UnhookSlider(worldMusic);
            UnhookSlider(battleMusic);
            UnhookSlider(battleFx);
            UnhookSlider(speaking);
            UnhookSlider(textFx);

            UnhookToggle(subtitles);
            UnhookToggle(reduceFlashing);
            UnhookToggle(reduceShake);
            UnhookToggle(highContrastUI);
            UnhookToggle(dyslexiaFont);
            UnhookToggle(battleAnimations);
            UnhookToggle(typeEffectivenessHints);
            UnhookToggle(autosave);

            UnhookSlider(textSize);
            UnhookSlider(textSpeed);
        }

        private void ApplyAll()
        {
            GameSettings.BindAudioMixer(audioMixer, masterParam, worldMusicParam, battleMusicParam, battleFxParam, voiceParam, textFxParam);
            GameSettings.BindFonts(defaultFont, dyslexiaFriendlyFont);
            GameSettings.ApplyAll();
        }

        // ---------- helpers ----------
        private static void SetSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            float v = Mathf.Clamp01(GameSettings.GetFloat(key));
            s.SetValueWithoutNotify(v);
            UpdatePercentLabel(valueText, v);
        }

        private static void SetIntSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            s.wholeNumbers = true;
            int v = GameSettings.GetInt(key);
            s.SetValueWithoutNotify(v);
            if (valueText != null) valueText.text = v.ToString();
        }

        private static void SetToggle(Toggle t, string key)
        {
            if (t == null) return;
            t.SetIsOnWithoutNotify(GameSettings.GetBool(key));
        }

        private static void HookSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            s.onValueChanged.AddListener(v =>
            {
                v = Mathf.Clamp01(v);
                UpdatePercentLabel(valueText, v);
                GameSettings.SetFloat(key, v);
            });
        }

        private static void HookIntSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            s.onValueChanged.AddListener(v =>
            {
                int iv = Mathf.RoundToInt(v);
                if (valueText != null) valueText.text = iv.ToString();
                GameSettings.SetInt(key, iv);
            });
        }

        private static void HookToggle(Toggle t, string key)
        {
            if (t == null) return;
            t.onValueChanged.AddListener(on =>
            {
                GameSettings.SetBool(key, on);
            });
        }

        private static void UnhookSlider(Slider s)
        {
            if (s == null) return;
            s.onValueChanged.RemoveAllListeners();
        }

        private static void UnhookToggle(Toggle t)
        {
            if (t == null) return;
            t.onValueChanged.RemoveAllListeners();
        }

        private static void UpdatePercentLabel(TMP_Text label, float v01)
        {
            if (label == null) return;
            int pct = Mathf.RoundToInt(v01 * 100f);
            label.text = $"{pct}%";
        }
    }
}
