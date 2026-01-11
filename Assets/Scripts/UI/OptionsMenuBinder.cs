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
    /// - Values persist via PlayerPrefs
    /// - Audio sliders map 0..1 to dB on an AudioMixer
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

        // PlayerPrefs keys
        private const string PP_MASTER = "opt_master";
        private const string PP_BATTLEFX = "opt_battlefx";
        private const string PP_VOICE = "opt_voice";
        private const string PP_TEXTFX = "opt_textfx";
        private const string PP_WORLDMUSIC = "opt_worldmusic";
        private const string PP_BATTLEMUSIC = "opt_battlemusic";

        private const string PP_SUBTITLES = "opt_subtitles";
        private const string PP_REDUCE_FLASH = "opt_reduce_flash";
        private const string PP_REDUCE_SHAKE = "opt_reduce_shake";
        private const string PP_HIGH_CONTRAST = "opt_high_contrast_ui";
        private const string PP_DYSLEXIA_FONT = "opt_dyslexia_font";
        private const string PP_BATTLE_ANIMS = "opt_battle_anims";
        private const string PP_TYPE_HINTS = "opt_type_hints";
        private const string PP_AUTOSAVE = "opt_autosave";

        private const string PP_TEXT_SIZE = "opt_text_size";
        private const string PP_TEXT_SPEED = "opt_text_speed";

        private void OnEnable()
        {
            EnsureDefaults();
            LoadUIFromPrefs();
            HookUIEvents();
            ApplyAll();
        }

        private void OnDisable()
        {
            UnhookUIEvents();
        }

        private void EnsureDefaults()
        {
            SetDefaultFloat(PP_MASTER, 0.85f);
            SetDefaultFloat(PP_WORLDMUSIC, 0.75f);
            SetDefaultFloat(PP_BATTLEMUSIC, 0.75f);
            SetDefaultFloat(PP_BATTLEFX, 0.85f);
            SetDefaultFloat(PP_VOICE, 0.85f);
            SetDefaultFloat(PP_TEXTFX, 0.85f);

            SetDefaultInt(PP_SUBTITLES, 1);
            SetDefaultInt(PP_REDUCE_FLASH, 1);
            SetDefaultInt(PP_REDUCE_SHAKE, 0);
            SetDefaultInt(PP_HIGH_CONTRAST, 0);
            SetDefaultInt(PP_DYSLEXIA_FONT, 0);
            SetDefaultInt(PP_BATTLE_ANIMS, 1);
            SetDefaultInt(PP_TYPE_HINTS, 1);
            SetDefaultInt(PP_AUTOSAVE, 1);

            SetDefaultInt(PP_TEXT_SIZE, 2);  // 1..4
            SetDefaultInt(PP_TEXT_SPEED, 3); // 1..5
        }

        private static void SetDefaultFloat(string key, float value)
        {
            if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetFloat(key, value);
        }

        private static void SetDefaultInt(string key, int value)
        {
            if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, value);
        }

        private void LoadUIFromPrefs()
        {
            SetSlider(master, PP_MASTER, masterValueText);
            SetSlider(worldMusic, PP_WORLDMUSIC, worldMusicValueText);
            SetSlider(battleMusic, PP_BATTLEMUSIC, battleMusicValueText);
            SetSlider(battleFx, PP_BATTLEFX, battleFxValueText);
            SetSlider(speaking, PP_VOICE, speakingValueText);
            SetSlider(textFx, PP_TEXTFX, textFxValueText);

            SetToggle(subtitles, PP_SUBTITLES);
            SetToggle(reduceFlashing, PP_REDUCE_FLASH);
            SetToggle(reduceShake, PP_REDUCE_SHAKE);
            SetToggle(highContrastUI, PP_HIGH_CONTRAST);
            SetToggle(dyslexiaFont, PP_DYSLEXIA_FONT);
            SetToggle(battleAnimations, PP_BATTLE_ANIMS);
            SetToggle(typeEffectivenessHints, PP_TYPE_HINTS);
            SetToggle(autosave, PP_AUTOSAVE);

            SetIntSlider(textSize, PP_TEXT_SIZE, textSizeValueText);
            SetIntSlider(textSpeed, PP_TEXT_SPEED, textSpeedValueText);
        }

        private void HookUIEvents()
        {
            HookSlider(master, PP_MASTER, masterParam, masterValueText);
            HookSlider(worldMusic, PP_WORLDMUSIC, worldMusicParam, worldMusicValueText);
            HookSlider(battleMusic, PP_BATTLEMUSIC, battleMusicParam, battleMusicValueText);
            HookSlider(battleFx, PP_BATTLEFX, battleFxParam, battleFxValueText);
            HookSlider(speaking, PP_VOICE, voiceParam, speakingValueText);
            HookSlider(textFx, PP_TEXTFX, textFxParam, textFxValueText);

            HookToggle(subtitles, PP_SUBTITLES);
            HookToggle(reduceFlashing, PP_REDUCE_FLASH);
            HookToggle(reduceShake, PP_REDUCE_SHAKE);
            HookToggle(highContrastUI, PP_HIGH_CONTRAST);
            HookToggle(dyslexiaFont, PP_DYSLEXIA_FONT);
            HookToggle(battleAnimations, PP_BATTLE_ANIMS);
            HookToggle(typeEffectivenessHints, PP_TYPE_HINTS);
            HookToggle(autosave, PP_AUTOSAVE);

            HookIntSlider(textSize, PP_TEXT_SIZE, textSizeValueText);
            HookIntSlider(textSpeed, PP_TEXT_SPEED, textSpeedValueText);
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
            // Apply audio
            ApplyMixerVolume(masterParam, PlayerPrefs.GetFloat(PP_MASTER, 0.85f));
            ApplyMixerVolume(worldMusicParam, PlayerPrefs.GetFloat(PP_WORLDMUSIC, 0.75f));
            ApplyMixerVolume(battleMusicParam, PlayerPrefs.GetFloat(PP_BATTLEMUSIC, 0.75f));
            ApplyMixerVolume(battleFxParam, PlayerPrefs.GetFloat(PP_BATTLEFX, 0.85f));
            ApplyMixerVolume(voiceParam, PlayerPrefs.GetFloat(PP_VOICE, 0.85f));
            ApplyMixerVolume(textFxParam, PlayerPrefs.GetFloat(PP_TEXTFX, 0.85f));

            // If no mixer is assigned, at least master affects AudioListener.
            if (audioMixer == null)
                AudioListener.volume = PlayerPrefs.GetFloat(PP_MASTER, 0.85f);

            // Apply dyslexia font globally (optional)
            if (defaultFont != null && dyslexiaFriendlyFont != null)
            {
                bool useDys = PlayerPrefs.GetInt(PP_DYSLEXIA_FONT, 0) == 1;
                TMP_Settings.defaultFontAsset = useDys ? dyslexiaFriendlyFont : defaultFont;
            }

            // The rest (reduce flashing, shake, etc.) should be read by your systems at runtime.
        }

        // ---------- helpers ----------
        private void SetSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            float v = Mathf.Clamp01(PlayerPrefs.GetFloat(key));
            s.SetValueWithoutNotify(v);
            UpdatePercentLabel(valueText, v);
        }

        private void SetIntSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            s.wholeNumbers = true;
            int v = PlayerPrefs.GetInt(key);
            s.SetValueWithoutNotify(v);
            if (valueText != null) valueText.text = v.ToString();
        }

        private void SetToggle(Toggle t, string key)
        {
            if (t == null) return;
            t.SetIsOnWithoutNotify(PlayerPrefs.GetInt(key, 0) == 1);
        }

        private void HookSlider(Slider s, string key, string mixerParam, TMP_Text valueText)
        {
            if (s == null) return;
            s.onValueChanged.AddListener(v =>
            {
                v = Mathf.Clamp01(v);
                PlayerPrefs.SetFloat(key, v);
                PlayerPrefs.Save();
                UpdatePercentLabel(valueText, v);

                if (audioMixer != null)
                    ApplyMixerVolume(mixerParam, v);
                else if (key == PP_MASTER)
                    AudioListener.volume = v;
            });
        }

        private void HookIntSlider(Slider s, string key, TMP_Text valueText)
        {
            if (s == null) return;
            s.onValueChanged.AddListener(v =>
            {
                int iv = Mathf.RoundToInt(v);
                PlayerPrefs.SetInt(key, iv);
                PlayerPrefs.Save();
                if (valueText != null) valueText.text = iv.ToString();
            });
        }

        private void HookToggle(Toggle t, string key)
        {
            if (t == null) return;
            t.onValueChanged.AddListener(on =>
            {
                PlayerPrefs.SetInt(key, on ? 1 : 0);
                PlayerPrefs.Save();
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

        private void ApplyMixerVolume(string param, float v01)
        {
            if (audioMixer == null) return;
            if (string.IsNullOrWhiteSpace(param)) return;

            // 0..1 -> dB with log curve (0 => -80 dB, 1 => 0 dB)
            float dB = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
            audioMixer.SetFloat(param, dB);
        }
    }
}
