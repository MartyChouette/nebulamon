using System.Collections;
using UnityEngine;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Demo scene: plays a scripted story sequence using BattleStoryDirector,
    /// card displays, and dialogue system to showcase all narrative systems.
    /// </summary>
    public class DemoStoryPlayer : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition playerChar;
        [SerializeField] private CharacterDefinition npcChar;
        [SerializeField] private MonsterDefinition[] monstersToShow;
        [SerializeField] private ShipDefinition playerShip;

        TMP_Text _statusText;

        void Start()
        {
            DemoUIBuilder.CreateCamera();
            DemoUIBuilder.CreateEventSystem();
            var canvas = DemoUIBuilder.CreateCanvas();
            var ct = canvas.transform;

            // Card panels
            var charCard = DemoUIBuilder.CreateCardUI<CharacterCardUI>(ct, "CharacterCard", new Vector2(-350, 50));
            var monCard = DemoUIBuilder.CreateCardUI<MonsterCardUI>(ct, "MonsterCard", new Vector2(0, 50));
            var shipCard = DemoUIBuilder.CreateCardUI<ShipCardUI>(ct, "ShipCard", new Vector2(350, 50));

            // Managers
            DemoUIBuilder.CreateCardDisplayManager(charCard, monCard, shipCard);
            var story = DemoUIBuilder.CreateStoryDirector(ct);

            // Dialogue UI (for ShowSingleLine demo)
            var dialogueUI = DemoUIBuilder.CreateDialogueUI(ct);

            // DialogueManager singleton
            if (DialogueManager.Instance != null)
                Destroy(DialogueManager.Instance.gameObject);

            var dmGo = new GameObject("DialogueManager");
            dmGo.AddComponent<DialogueManager>();
            // OnSceneLoaded will auto-find dialogueUI

            // Status text
            _statusText = DemoUIBuilder.CreateText(ct, "Starting demo...",
                new Vector2(0, 330), new Vector2(800, 30),
                new Color(0.6f, 0.7f, 0.6f));
            _statusText.fontSize = 16;

            StartCoroutine(PlayDemoSequence(story));
        }

        IEnumerator PlayDemoSequence(BattleStoryDirector story)
        {
            yield return new WaitForSeconds(0.5f);
            SetStatus("Act 1: Ship Card + Narration");

            // Act 1: Ship card + narration
            if (playerShip != null)
            {
                yield return story.ShowShip(playerShip, waitForCard: true);
                yield return story.Say("", "The Starhopper drifts through an uncharted nebula...");
                yield return story.HideShip();
            }

            yield return new WaitForSeconds(0.3f);
            SetStatus("Act 2: Character Dialogue");

            // Act 2: Character cards + dialogue
            if (playerChar != null)
            {
                yield return story.ShowCharacter(playerChar, waitForCard: true);
                yield return story.Say(playerChar.displayName, "Sensors are picking up something ahead.");
                yield return story.HideCharacter();
            }

            if (npcChar != null)
            {
                yield return story.ShowCharacter(npcChar, waitForCard: true);
                yield return story.Say(npcChar.displayName, "Captain, I'm detecting wild monsters in this sector!");
                yield return story.HideCharacter();
            }

            yield return new WaitForSeconds(0.3f);
            SetStatus("Act 3: Monster Showcase");

            // Act 3: Monster showcase
            if (monstersToShow != null)
            {
                foreach (var monster in monstersToShow)
                {
                    if (monster == null) continue;
                    yield return story.ShowMonster(monster, waitForCard: true);
                    yield return story.Say("", $"A wild {monster.displayName} appears!");
                    yield return story.Wait(0.3f);
                    yield return story.HideMonster();
                }
            }

            // Act 4: Dialogue system demo
            yield return story.ClearText();
            yield return story.HideAllCards();

            SetStatus("Act 4: Dialogue System");

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowSingleLine("Demo complete! All systems operational.", "System");
            }

            yield return new WaitForSeconds(2f);
            SetStatus("Demo complete - Press ESC to exit");

            // Show exit instruction
            DemoUIBuilder.CreateText(
                _statusText.transform.parent,
                "Press ESC to exit",
                new Vector2(0, -330), new Vector2(400, 30),
                new Color(0.8f, 0.8f, 0.3f));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }

        void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }
    }
}
