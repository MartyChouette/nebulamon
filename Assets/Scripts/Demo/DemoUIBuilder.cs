using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Static helper for building UI at runtime in demo scenes.
    /// Avoids bloated scene YAML by constructing Canvas, panels, and card UIs from code.
    /// </summary>
    public static class DemoUIBuilder
    {
        static readonly BindingFlags NonPublicInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        public static GameObject CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = -1;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.AddComponent<AudioListener>();
            return go;
        }

        public static GameObject CreateEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return existing.gameObject;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return go;
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static TMP_Text CreateText(Transform parent, string text,
            Vector2 pos, Vector2 size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 18;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static Image CreateImage(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        /// <summary>
        /// Creates a card UI panel with the correct component type and wires serialized fields via reflection.
        /// </summary>
        public static T CreateCardUI<T>(Transform parent, string name, Vector2 pos) where T : BaseCardUI
        {
            // Root panel
            var panelGo = CreatePanel(parent, name, pos, new Vector2(280, 360),
                new Color(0.1f, 0.1f, 0.2f, 0.9f));

            var cardUI = panelGo.AddComponent<T>();
            var panelRT = panelGo.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            // Portrait
            var portrait = CreateImage(panelGo.transform, new Vector2(0, 60), new Vector2(160, 160),
                new Color(0.2f, 0.2f, 0.3f, 1f));
            portrait.gameObject.name = "Portrait";

            // Name
            var nameText = CreateText(panelGo.transform, "", new Vector2(0, -40), new Vector2(260, 30),
                Color.white, TextAlignmentOptions.Center);
            nameText.gameObject.name = "NameText";
            nameText.fontSize = 22;
            nameText.fontStyle = FontStyles.Bold;

            // Wire base fields
            SetField(typeof(BaseCardUI), cardUI, "root", panelGo);
            SetField(typeof(BaseCardUI), cardUI, "portraitImage", portrait);
            SetField(typeof(BaseCardUI), cardUI, "nameText", nameText);

            // Type-specific fields
            if (typeof(T) == typeof(CharacterCardUI))
            {
                var titleText = CreateText(panelGo.transform, "", new Vector2(0, -70), new Vector2(260, 24),
                    new Color(0.7f, 0.8f, 1f), TextAlignmentOptions.Center);
                titleText.gameObject.name = "TitleText";
                titleText.fontSize = 16;
                titleText.fontStyle = FontStyles.Italic;

                var descText = CreateText(panelGo.transform, "", new Vector2(0, -110), new Vector2(260, 60),
                    new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
                descText.gameObject.name = "DescriptionText";
                descText.fontSize = 14;

                SetField(typeof(CharacterCardUI), cardUI, "titleText", titleText);
                SetField(typeof(CharacterCardUI), cardUI, "descriptionText", descText);
            }
            else if (typeof(T) == typeof(MonsterCardUI))
            {
                var elemText = CreateText(panelGo.transform, "", new Vector2(0, -70), new Vector2(260, 24),
                    new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center);
                elemText.gameObject.name = "ElementText";
                elemText.fontSize = 16;

                var hpText = CreateText(panelGo.transform, "", new Vector2(-60, -100), new Vector2(120, 24),
                    Color.green, TextAlignmentOptions.Left);
                hpText.gameObject.name = "HPText";
                hpText.fontSize = 14;

                var spdText = CreateText(panelGo.transform, "", new Vector2(60, -100), new Vector2(120, 24),
                    new Color(0.5f, 0.8f, 1f), TextAlignmentOptions.Left);
                spdText.gameObject.name = "SpeedText";
                spdText.fontSize = 14;

                var atkText = CreateText(panelGo.transform, "", new Vector2(-60, -126), new Vector2(120, 24),
                    new Color(1f, 0.5f, 0.5f), TextAlignmentOptions.Left);
                atkText.gameObject.name = "AttackText";
                atkText.fontSize = 14;

                var defText = CreateText(panelGo.transform, "", new Vector2(60, -126), new Vector2(120, 24),
                    new Color(0.6f, 0.6f, 1f), TextAlignmentOptions.Left);
                defText.gameObject.name = "DefenseText";
                defText.fontSize = 14;

                SetField(typeof(MonsterCardUI), cardUI, "elementText", elemText);
                SetField(typeof(MonsterCardUI), cardUI, "hpText", hpText);
                SetField(typeof(MonsterCardUI), cardUI, "speedText", spdText);
                SetField(typeof(MonsterCardUI), cardUI, "attackText", atkText);
                SetField(typeof(MonsterCardUI), cardUI, "defenseText", defText);
            }
            else if (typeof(T) == typeof(ShipCardUI))
            {
                var classText = CreateText(panelGo.transform, "", new Vector2(0, -70), new Vector2(260, 24),
                    new Color(0.7f, 0.8f, 1f), TextAlignmentOptions.Center);
                classText.gameObject.name = "ClassText";
                classText.fontSize = 16;
                classText.fontStyle = FontStyles.Italic;

                var hullText = CreateText(panelGo.transform, "", new Vector2(-60, -100), new Vector2(120, 24),
                    new Color(0.8f, 0.6f, 0.3f), TextAlignmentOptions.Left);
                hullText.gameObject.name = "HullText";
                hullText.fontSize = 14;

                var shieldText = CreateText(panelGo.transform, "", new Vector2(60, -100), new Vector2(120, 24),
                    new Color(0.3f, 0.7f, 1f), TextAlignmentOptions.Left);
                shieldText.gameObject.name = "ShieldsText";
                shieldText.fontSize = 14;

                var speedText = CreateText(panelGo.transform, "", new Vector2(-60, -126), new Vector2(120, 24),
                    Color.white, TextAlignmentOptions.Left);
                speedText.gameObject.name = "SpeedText";
                speedText.fontSize = 14;

                var cargoText = CreateText(panelGo.transform, "", new Vector2(60, -126), new Vector2(120, 24),
                    new Color(0.9f, 0.9f, 0.5f), TextAlignmentOptions.Left);
                cargoText.gameObject.name = "CargoText";
                cargoText.fontSize = 14;

                var descText = CreateText(panelGo.transform, "", new Vector2(0, -158), new Vector2(260, 40),
                    new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
                descText.gameObject.name = "DescriptionText";
                descText.fontSize = 13;

                SetField(typeof(ShipCardUI), cardUI, "classText", classText);
                SetField(typeof(ShipCardUI), cardUI, "hullText", hullText);
                SetField(typeof(ShipCardUI), cardUI, "shieldsText", shieldText);
                SetField(typeof(ShipCardUI), cardUI, "speedText", speedText);
                SetField(typeof(ShipCardUI), cardUI, "cargoText", cargoText);
                SetField(typeof(ShipCardUI), cardUI, "descriptionText", descText);
            }

            return cardUI;
        }

        /// <summary>
        /// Creates a CardDisplayManager and wires the three card UIs to it.
        /// Destroys any existing instance first to avoid DontDestroyOnLoad conflicts.
        /// </summary>
        public static CardDisplayManager CreateCardDisplayManager(
            CharacterCardUI charCard, MonsterCardUI monCard, ShipCardUI shipCard)
        {
            // Destroy existing singleton to avoid duplicate
            if (CardDisplayManager.Instance != null)
                Object.Destroy(CardDisplayManager.Instance.gameObject);

            var go = new GameObject("CardDisplayManager");
            var mgr = go.AddComponent<CardDisplayManager>();
            // Awake runs immediately; now set card references
            mgr.SetCharacterCard(charCard);
            mgr.SetMonsterCard(monCard);
            mgr.SetShipCard(shipCard);
            return mgr;
        }

        /// <summary>
        /// Creates a BattleStoryDirector with a text panel for story sequences.
        /// </summary>
        public static BattleStoryDirector CreateStoryDirector(Transform canvasTransform)
        {
            // Destroy existing singleton
            if (BattleStoryDirector.Instance != null)
                Object.Destroy(BattleStoryDirector.Instance.gameObject);

            // Story text panel
            var panel = CreatePanel(canvasTransform, "StoryTextPanel",
                new Vector2(0, -280), new Vector2(900, 100),
                new Color(0.05f, 0.05f, 0.1f, 0.92f));

            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);

            var storyText = CreateText(panel.transform, "", Vector2.zero, new Vector2(860, 80),
                Color.white, TextAlignmentOptions.TopLeft);
            storyText.gameObject.name = "StoryText";
            storyText.fontSize = 18;

            var go = new GameObject("BattleStoryDirector");
            var director = go.AddComponent<BattleStoryDirector>();

            SetField(typeof(BattleStoryDirector), director, "storyText", storyText);
            SetField(typeof(BattleStoryDirector), director, "storyTextPanel", panel);

            return director;
        }

        /// <summary>
        /// Creates a DialogueUI with a text panel for dialogue display.
        /// </summary>
        public static DialogueUI CreateDialogueUI(Transform canvasTransform)
        {
            var panel = CreatePanel(canvasTransform, "DialoguePanel",
                new Vector2(0, -260), new Vector2(800, 120),
                new Color(0.08f, 0.08f, 0.15f, 0.9f));

            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);

            var speakerLabel = CreateText(panel.transform, "", new Vector2(-300, 35), new Vector2(200, 28),
                new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Left);
            speakerLabel.gameObject.name = "SpeakerLabel";
            speakerLabel.fontSize = 16;
            speakerLabel.fontStyle = FontStyles.Bold;

            var bodyText = CreateText(panel.transform, "", new Vector2(0, -5), new Vector2(760, 70),
                Color.white, TextAlignmentOptions.TopLeft);
            bodyText.gameObject.name = "BodyText";
            bodyText.fontSize = 16;

            var choicesRoot = new GameObject("ChoicesRoot");
            choicesRoot.transform.SetParent(panel.transform, false);
            var choicesRT = choicesRoot.AddComponent<RectTransform>();
            choicesRT.anchoredPosition = new Vector2(0, -50);
            choicesRT.sizeDelta = new Vector2(760, 40);

            var dialogueUI = panel.AddComponent<DialogueUI>();
            dialogueUI.root = panel;
            dialogueUI.speakerLabel = speakerLabel;
            dialogueUI.bodyText = bodyText;
            dialogueUI.choicesRoot = choicesRoot.transform;

            panel.SetActive(false);
            return dialogueUI;
        }

        static void SetField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, NonPublicInstance);
            if (field != null)
                field.SetValue(target, value);
        }
    }
}
