using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Applies retro text effects to TMP text (like old Gameboy/SNES RPGs).
    ///
    /// Supported tags in dialogue text:
    ///   <shake>shaky text</shake>           - Jittery/vibrating text
    ///   <wave>wavy text</wave>              - Sine wave motion
    ///   <rainbow>colorful</rainbow>         - Cycling rainbow colors
    ///   <pulse>pulsing</pulse>              - Scale pulsing
    ///   <slow>slower text</slow>            - Typewriter slows down
    ///   <fast>faster text</fast>            - Typewriter speeds up
    ///   <pause=0.5/>                        - Pause typewriter for 0.5 seconds
    ///   <instant>no typewriter</instant>    - Show instantly (no typewriter)
    ///   <color=#FF0000>red</color>          - Standard TMP color (preserved)
    ///   <b>bold</b>                         - Standard TMP bold (preserved)
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class NarrativeTextEffects : MonoBehaviour
    {
        [Header("Shake Effect")]
        [SerializeField] private float shakeIntensity = 2f;
        [SerializeField] private float shakeSpeed = 50f;

        [Header("Wave Effect")]
        [SerializeField] private float waveAmplitude = 5f;
        [SerializeField] private float waveFrequency = 3f;
        [SerializeField] private float waveSpeed = 5f;

        [Header("Rainbow Effect")]
        [SerializeField] private float rainbowSpeed = 2f;
        [SerializeField] private float rainbowSaturation = 0.8f;

        [Header("Pulse Effect")]
        [SerializeField] private float pulseMin = 0.9f;
        [SerializeField] private float pulseMax = 1.1f;
        [SerializeField] private float pulseSpeed = 4f;

        private TMP_Text _text;
        private string _originalText;
        private List<CharacterEffect> _characterEffects = new();
        private bool _effectsActive;

        // Effect types per character
        private enum EffectType { None, Shake, Wave, Rainbow, Pulse }

        private struct CharacterEffect
        {
            public int index;
            public EffectType effect;
            public float startTime;
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (!_effectsActive || _text == null) return;

            _text.ForceMeshUpdate();
            ApplyEffects();
        }

        /// <summary>
        /// Sets the text and parses effect tags.
        /// Returns the clean text (with effect tags removed but TMP tags preserved).
        /// </summary>
        public string SetTextWithEffects(string richText)
        {
            _originalText = richText;
            _characterEffects.Clear();

            // Parse our custom tags and build the effect list
            string cleanText = ParseEffectTags(richText);

            _text.text = cleanText;
            _text.ForceMeshUpdate();

            _effectsActive = _characterEffects.Count > 0;

            return cleanText;
        }

        /// <summary>
        /// Clears all effects.
        /// </summary>
        public void ClearEffects()
        {
            _effectsActive = false;
            _characterEffects.Clear();
        }

        private string ParseEffectTags(string input)
        {
            var result = new StringBuilder();
            int cleanIndex = 0; // Index in the clean (output) text

            // Our custom effect tags
            var effectTags = new[] { "shake", "wave", "rainbow", "pulse", "slow", "fast", "instant" };

            int i = 0;
            while (i < input.Length)
            {
                // Check for tag start
                if (input[i] == '<')
                {
                    // Find tag end
                    int tagEnd = input.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        result.Append(input[i]);
                        cleanIndex++;
                        i++;
                        continue;
                    }

                    string tagContent = input.Substring(i + 1, tagEnd - i - 1).ToLowerInvariant();

                    // Check if it's one of our effect tags
                    bool isOurTag = false;
                    bool isClosingTag = tagContent.StartsWith("/");
                    string tagName = isClosingTag ? tagContent.Substring(1) : tagContent.Split('=')[0];

                    foreach (var effectTag in effectTags)
                    {
                        if (tagName == effectTag)
                        {
                            isOurTag = true;
                            break;
                        }
                    }

                    // Check for pause tag
                    if (tagName.StartsWith("pause"))
                    {
                        // Skip pause tags - handled by typewriter
                        i = tagEnd + 1;
                        continue;
                    }

                    if (isOurTag)
                    {
                        if (!isClosingTag)
                        {
                            // Opening tag - mark where effect starts
                            var effect = TagToEffect(tagName);
                            if (effect != EffectType.None)
                            {
                                // Find the closing tag
                                string closeTag = $"</{tagName}>";
                                int closeIndex = input.IndexOf(closeTag, tagEnd, StringComparison.OrdinalIgnoreCase);

                                if (closeIndex != -1)
                                {
                                    // Get the content between tags
                                    string innerContent = input.Substring(tagEnd + 1, closeIndex - tagEnd - 1);

                                    // Add characters with effects
                                    int startCleanIndex = cleanIndex;
                                    foreach (char c in innerContent)
                                    {
                                        if (c != '<') // Skip nested tags for now
                                        {
                                            _characterEffects.Add(new CharacterEffect
                                            {
                                                index = cleanIndex,
                                                effect = effect,
                                                startTime = Time.time
                                            });
                                        }
                                        result.Append(c);
                                        cleanIndex++;
                                    }

                                    i = closeIndex + closeTag.Length;
                                    continue;
                                }
                            }
                        }

                        // Skip our custom tags
                        i = tagEnd + 1;
                        continue;
                    }
                    else
                    {
                        // Not our tag - preserve it (TMP tags like <color>, <b>, etc.)
                        string fullTag = input.Substring(i, tagEnd - i + 1);
                        result.Append(fullTag);
                        i = tagEnd + 1;
                        continue;
                    }
                }

                // Regular character
                result.Append(input[i]);
                cleanIndex++;
                i++;
            }

            return result.ToString();
        }

        private EffectType TagToEffect(string tagName)
        {
            switch (tagName)
            {
                case "shake": return EffectType.Shake;
                case "wave": return EffectType.Wave;
                case "rainbow": return EffectType.Rainbow;
                case "pulse": return EffectType.Pulse;
                default: return EffectType.None;
            }
        }

        private void ApplyEffects()
        {
            var textInfo = _text.textInfo;
            if (textInfo.characterCount == 0) return;

            // Create a lookup of effects by character index
            var effectsByIndex = new Dictionary<int, EffectType>();
            foreach (var ce in _characterEffects)
            {
                effectsByIndex[ce.index] = ce.effect;
            }

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                if (!effectsByIndex.TryGetValue(i, out EffectType effect))
                    continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                var vertices = textInfo.meshInfo[materialIndex].vertices;
                var colors = textInfo.meshInfo[materialIndex].colors32;

                // Get character center
                Vector3 center = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f;

                switch (effect)
                {
                    case EffectType.Shake:
                        ApplyShake(vertices, vertexIndex);
                        break;

                    case EffectType.Wave:
                        ApplyWave(vertices, vertexIndex, i, center);
                        break;

                    case EffectType.Rainbow:
                        ApplyRainbow(colors, vertexIndex, i);
                        break;

                    case EffectType.Pulse:
                        ApplyPulse(vertices, vertexIndex, center);
                        break;
                }
            }

            // Update the mesh
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                _text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private void ApplyShake(Vector3[] vertices, int vertexIndex)
        {
            float time = Time.time * shakeSpeed;
            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(time, 0) * 2 - 1,
                Mathf.PerlinNoise(0, time) * 2 - 1,
                0
            ) * shakeIntensity;

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] += offset;
            }
        }

        private void ApplyWave(Vector3[] vertices, int vertexIndex, int charIndex, Vector3 center)
        {
            float time = Time.time * waveSpeed;
            float offset = Mathf.Sin(time + charIndex * waveFrequency) * waveAmplitude;

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j].y += offset;
            }
        }

        private void ApplyRainbow(Color32[] colors, int vertexIndex, int charIndex)
        {
            float hue = (Time.time * rainbowSpeed + charIndex * 0.1f) % 1f;
            Color color = Color.HSVToRGB(hue, rainbowSaturation, 1f);
            Color32 color32 = color;

            for (int j = 0; j < 4; j++)
            {
                colors[vertexIndex + j] = color32;
            }
        }

        private void ApplyPulse(Vector3[] vertices, int vertexIndex, Vector3 center)
        {
            float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

            for (int j = 0; j < 4; j++)
            {
                Vector3 dir = vertices[vertexIndex + j] - center;
                vertices[vertexIndex + j] = center + dir * scale;
            }
        }
    }

    /// <summary>
    /// Helper for parsing typewriter speed modifiers from text.
    /// </summary>
    public static class NarrativeTextHelper
    {
        /// <summary>
        /// Extracts speed modifiers and pauses from text.
        /// Returns a list of (charIndex, speedMultiplier, pauseDuration).
        /// </summary>
        public static List<(int index, float speed, float pause)> ParseSpeedModifiers(string text)
        {
            var modifiers = new List<(int, float, float)>();

            // Find <slow>, <fast>, <pause=X> tags
            var slowRegex = new Regex(@"<slow>(.*?)</slow>", RegexOptions.IgnoreCase);
            var fastRegex = new Regex(@"<fast>(.*?)</fast>", RegexOptions.IgnoreCase);
            var pauseRegex = new Regex(@"<pause=([0-9.]+)/?>", RegexOptions.IgnoreCase);

            // This is simplified - a full implementation would track positions properly
            // For now, return empty and let the typewriter handle tags inline

            return modifiers;
        }

        /// <summary>
        /// Strips all custom narrative tags from text, leaving only standard TMP tags.
        /// </summary>
        public static string StripNarrativeTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Remove our custom tags but preserve content
            text = Regex.Replace(text, @"<shake>(.*?)</shake>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<wave>(.*?)</wave>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<rainbow>(.*?)</rainbow>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<pulse>(.*?)</pulse>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<slow>(.*?)</slow>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<fast>(.*?)</fast>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<instant>(.*?)</instant>", "$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<pause=[0-9.]+/?>", "", RegexOptions.IgnoreCase);

            return text;
        }
    }
}
