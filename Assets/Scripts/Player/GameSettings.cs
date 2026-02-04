using System;
using UnityEngine;
using UnityEngine.Audio;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Single source of truth for player options.
    /// - Stores to PlayerPrefs
    /// - Optional "Apply" hooks (audio mixer, TMP font defaults, etc.)
    ///
    /// Use anywhere:
    ///   GameSettings.GetBool(GameSettings.Keys.SubtitlesEnabled)
    ///   GameSettings.Master01
    ///   GameSettings.Colorblind
    /// </summary>
    public static class GameSettings
    {
        // -------------------------
        // Keys
        // -------------------------
        public static class Keys
        {
            // Audio sliders (0..1)
            public const string Master01 = "opt_master";
            public const string WorldMusic01 = "opt_worldmusic";
            public const string BattleMusic01 = "opt_battlemusic";
            public const string BattleFx01 = "opt_battlefx";
            public const string Voice01 = "opt_voice";
            public const string TextFx01 = "opt_textfx";

            // Accessibility toggles
            public const string ReduceFlashing = "opt_reduce_flash";
            public const string ReduceShake = "opt_reduce_shake";
            public const string BattleAnims = "opt_battle_anims";

            public const string SubtitlesEnabled = "opt_subtitles";
            public const string SubtitleSpeakerLabels = "opt_subtitle_speaker";
            public const string SubtitleBackground = "opt_subtitle_bg";

            public const string DyslexiaFont = "opt_dyslexia_font";
            public const string HighContrastUI = "opt_high_contrast_ui";
            public const string ColorblindMode = "opt_colorblind_mode"; // int enum

            public const string AutoAdvanceDialogue = "opt_auto_advance_dialogue";
            public const string HoldToConfirm = "opt_hold_to_confirm";

            public const string UiSoundsEnabled = "opt_ui_sounds";

            public const string MonoAudio = "opt_mono_audio";
            public const string ReduceSpatialAudio = "opt_reduce_spatial";

            // Text speed/size (ints)
            public const string TextSpeed = "opt_text_speed"; // 1..5
            public const string TextSize = "opt_text_size";  // 1..4

            // Pokemon-like QoL / modern era
            public const string TypeEffectivenessHints = "opt_type_hints"; // show "Super effective!" etc even if player "should know"
            public const string BattleSpeed = "opt_battle_speed"; // 1..3
            public const string AutoSave = "opt_autosave";
            public const string SkipBattleIntro = "opt_skip_battle_intro";

            // Visual effects
            public const string GBCPaletteEnabled = "opt_gbc_palette";
            public const string GBCPaletteIndex = "opt_gbc_palette_idx"; // int: which palette preset
            public const string ScanlineEnabled = "opt_scanlines";
            public const string DayNightEnabled = "opt_daynight";
            public const string CRTPhosphorDitherEnabled = "opt_crt_phosphor";
        }

        // -------------------------
        // Enums
        // -------------------------
        public enum ColorblindModeEnum
        {
            Off = 0,
            Protanopia = 1,
            Deuteranopia = 2,
            Tritanopia = 3
        }

        public enum BattleSpeedEnum
        {
            Normal = 1,
            Fast = 2,
            Ultra = 3
        }

        // -------------------------
        // Optional integrations
        // -------------------------
        private static AudioMixer _mixer;
        private static string _masterParam, _worldMusicParam, _battleMusicParam, _battleFxParam, _voiceParam, _textFxParam;

        private static TMP_FontAsset _defaultFont;
        private static TMP_FontAsset _dyslexiaFont;

        public static event Action<string> OnSettingChanged;

        private static bool _inited;

        public static void InitIfNeeded()
        {
            if (_inited) return;
            _inited = true;
            EnsureDefaults();
        }

        public static void BindAudioMixer(
            AudioMixer mixer,
            string masterParam,
            string worldMusicParam,
            string battleMusicParam,
            string battleFxParam,
            string voiceParam,
            string textFxParam)
        {
            _mixer = mixer;
            _masterParam = masterParam;
            _worldMusicParam = worldMusicParam;
            _battleMusicParam = battleMusicParam;
            _battleFxParam = battleFxParam;
            _voiceParam = voiceParam;
            _textFxParam = textFxParam;
        }

        public static void BindFonts(TMP_FontAsset defaultFont, TMP_FontAsset dyslexiaFriendlyFont)
        {
            _defaultFont = defaultFont;
            _dyslexiaFont = dyslexiaFriendlyFont;
        }

        // -------------------------
        // Defaults
        // -------------------------
        public static void EnsureDefaults()
        {
            // Audio defaults
            EnsureFloat(Keys.Master01, 0.85f);
            EnsureFloat(Keys.WorldMusic01, 0.75f);
            EnsureFloat(Keys.BattleMusic01, 0.75f);
            EnsureFloat(Keys.BattleFx01, 0.85f);
            EnsureFloat(Keys.Voice01, 0.85f);
            EnsureFloat(Keys.TextFx01, 0.85f);

            // Accessibility defaults
            EnsureBool(Keys.ReduceFlashing, true);
            EnsureBool(Keys.ReduceShake, false);
            EnsureBool(Keys.BattleAnims, true);

            EnsureBool(Keys.SubtitlesEnabled, true);
            EnsureBool(Keys.SubtitleSpeakerLabels, true);
            EnsureBool(Keys.SubtitleBackground, true);

            EnsureBool(Keys.DyslexiaFont, false);
            EnsureBool(Keys.HighContrastUI, false);
            EnsureInt(Keys.ColorblindMode, (int)ColorblindModeEnum.Off);

            EnsureBool(Keys.AutoAdvanceDialogue, false);
            EnsureBool(Keys.HoldToConfirm, false);

            EnsureBool(Keys.UiSoundsEnabled, true);

            EnsureBool(Keys.MonoAudio, false);
            EnsureBool(Keys.ReduceSpatialAudio, false);

            EnsureInt(Keys.TextSpeed, 3);
            EnsureInt(Keys.TextSize, 2);

            // Pokemon-like QoL defaults
            EnsureBool(Keys.TypeEffectivenessHints, true);
            EnsureInt(Keys.BattleSpeed, (int)BattleSpeedEnum.Fast);
            EnsureBool(Keys.AutoSave, true);
            EnsureBool(Keys.SkipBattleIntro, false);

            // Visual effects defaults
            EnsureBool(Keys.GBCPaletteEnabled, false);
            EnsureInt(Keys.GBCPaletteIndex, 0);
            EnsureBool(Keys.ScanlineEnabled, false);
            EnsureBool(Keys.DayNightEnabled, false);
            EnsureBool(Keys.CRTPhosphorDitherEnabled, false);
        }

        // -------------------------
        // Convenience properties
        // -------------------------
        public static float Master01 { get => GetFloat(Keys.Master01); set => SetFloat(Keys.Master01, value); }
        public static float WorldMusic01 { get => GetFloat(Keys.WorldMusic01); set => SetFloat(Keys.WorldMusic01, value); }
        public static float BattleMusic01 { get => GetFloat(Keys.BattleMusic01); set => SetFloat(Keys.BattleMusic01, value); }
        public static float BattleFx01 { get => GetFloat(Keys.BattleFx01); set => SetFloat(Keys.BattleFx01, value); }
        public static float Voice01 { get => GetFloat(Keys.Voice01); set => SetFloat(Keys.Voice01, value); }
        public static float TextFx01 { get => GetFloat(Keys.TextFx01); set => SetFloat(Keys.TextFx01, value); }

        public static ColorblindModeEnum Colorblind
        {
            get => (ColorblindModeEnum)GetInt(Keys.ColorblindMode);
            set => SetInt(Keys.ColorblindMode, (int)value);
        }

        public static BattleSpeedEnum BattleSpeed
        {
            get => (BattleSpeedEnum)GetInt(Keys.BattleSpeed);
            set => SetInt(Keys.BattleSpeed, (int)value);
        }

        // -------------------------
        // Get/Set primitives
        // -------------------------
        public static bool GetBool(string key, bool defaultValue = false)
        {
            InitIfNeeded();
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public static void SetBool(string key, bool value)
        {
            InitIfNeeded();
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAfterChange(key);
            OnSettingChanged?.Invoke(key);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            InitIfNeeded();
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            InitIfNeeded();
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            ApplyAfterChange(key);
            OnSettingChanged?.Invoke(key);
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            InitIfNeeded();
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public static void SetFloat(string key, float value)
        {
            InitIfNeeded();
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyAfterChange(key);
            OnSettingChanged?.Invoke(key);
        }

        private static void EnsureBool(string key, bool defaultValue)
        {
            if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, defaultValue ? 1 : 0);
        }

        private static void EnsureInt(string key, int defaultValue)
        {
            if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, defaultValue);
        }

        private static void EnsureFloat(string key, float defaultValue)
        {
            if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetFloat(key, defaultValue);
        }

        // -------------------------
        // Apply hooks
        // -------------------------
        public static void ApplyAll()
        {
            InitIfNeeded();

            ApplyMixerVolume(_masterParam, Master01);
            ApplyMixerVolume(_worldMusicParam, WorldMusic01);
            ApplyMixerVolume(_battleMusicParam, BattleMusic01);
            ApplyMixerVolume(_battleFxParam, BattleFx01);
            ApplyMixerVolume(_voiceParam, Voice01);
            ApplyMixerVolume(_textFxParam, TextFx01);

            ApplyMasterFallbackIfNoMixer();

            ApplyFontDefaultsIfBound();

            // Note: Mono/spatial are game-level concerns; we store them here
            // but applying them globally depends on your audio pipeline.
        }

        private static void ApplyAfterChange(string key)
        {
            // Audio
            if (key == Keys.Master01) { ApplyMixerVolume(_masterParam, Master01); ApplyMasterFallbackIfNoMixer(); return; }
            if (key == Keys.WorldMusic01) { ApplyMixerVolume(_worldMusicParam, WorldMusic01); return; }
            if (key == Keys.BattleMusic01) { ApplyMixerVolume(_battleMusicParam, BattleMusic01); return; }
            if (key == Keys.BattleFx01) { ApplyMixerVolume(_battleFxParam, BattleFx01); return; }
            if (key == Keys.Voice01) { ApplyMixerVolume(_voiceParam, Voice01); return; }
            if (key == Keys.TextFx01) { ApplyMixerVolume(_textFxParam, TextFx01); return; }

            // Fonts
            if (key == Keys.DyslexiaFont)
            {
                ApplyFontDefaultsIfBound();
                return;
            }

            // Everything else is read by gameplay systems when needed (dialogue, battle, UI, etc.)
        }

        private static void ApplyMixerVolume(string param, float v01)
        {
            if (_mixer == null) return;
            if (string.IsNullOrWhiteSpace(param)) return;

            float dB = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
            _mixer.SetFloat(param, dB);
        }

        private static void ApplyMasterFallbackIfNoMixer()
        {
            if (_mixer != null) return;
            // Fallback: only master via AudioListener if no mixer bound.
            AudioListener.volume = Master01;
        }

        private static void ApplyFontDefaultsIfBound()
        {
            if (_defaultFont == null) return;
            if (_dyslexiaFont == null) return;

            bool useDyslexia = GetBool(Keys.DyslexiaFont, false);
            TMP_Settings.defaultFontAsset = useDyslexia ? _dyslexiaFont : _defaultFont;
        }
    }
}
