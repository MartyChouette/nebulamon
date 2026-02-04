using UnityEngine;
using TMPro;

namespace Nebula
{
    /// <summary>
    /// Demo scene: cycle through Character, Monster, and Ship cards on keypress.
    /// Arrow keys navigate items, Space switches card type, Escape quits.
    /// </summary>
    public class DemoCardShowcase : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition[] characters;
        [SerializeField] private MonsterDefinition[] monsters;
        [SerializeField] private ShipDefinition[] ships;

        enum CardMode { Monster, Character, Ship }

        CardMode _mode = CardMode.Monster;
        int _index;
        TMP_Text _titleText;
        TMP_Text _modeText;

        void Start()
        {
            DemoUIBuilder.CreateCamera();
            DemoUIBuilder.CreateEventSystem();
            var canvas = DemoUIBuilder.CreateCanvas();
            var ct = canvas.transform;

            // Create card panels
            var charCard = DemoUIBuilder.CreateCardUI<CharacterCardUI>(ct, "CharacterCard", Vector2.zero);
            var monCard = DemoUIBuilder.CreateCardUI<MonsterCardUI>(ct, "MonsterCard", Vector2.zero);
            var shipCard = DemoUIBuilder.CreateCardUI<ShipCardUI>(ct, "ShipCard", Vector2.zero);

            // Create manager
            DemoUIBuilder.CreateCardDisplayManager(charCard, monCard, shipCard);

            // Instruction text
            DemoUIBuilder.CreateText(ct,
                "Arrow Keys: Navigate  |  Space: Cycle Type  |  ESC: Quit",
                new Vector2(0, -330), new Vector2(800, 30),
                new Color(0.6f, 0.6f, 0.7f));

            // Title text
            _titleText = DemoUIBuilder.CreateText(ct, "", new Vector2(0, 300), new Vector2(600, 40),
                Color.white);
            _titleText.fontSize = 28;
            _titleText.fontStyle = FontStyles.Bold;

            // Mode indicator
            _modeText = DemoUIBuilder.CreateText(ct, "", new Vector2(0, 260), new Vector2(400, 30),
                new Color(0.7f, 0.8f, 1f));
            _modeText.fontSize = 18;

            // Show first card
            _index = 0;
            ShowCurrent();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                _index++;
                ClampIndex();
                ShowCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                _index--;
                ClampIndex();
                ShowCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                _mode = (CardMode)(((int)_mode + 1) % 3);
                _index = 0;
                ShowCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }

        void ShowCurrent()
        {
            var mgr = CardDisplayManager.Instance;
            if (mgr == null) return;

            mgr.HideAll();

            switch (_mode)
            {
                case CardMode.Monster:
                    if (monsters != null && monsters.Length > 0 && monsters[_index] != null)
                    {
                        mgr.ShowMonster(monsters[_index]);
                        _titleText.text = monsters[_index].displayName;
                    }
                    _modeText.text = $"Monster ({_index + 1}/{(monsters != null ? monsters.Length : 0)})";
                    break;

                case CardMode.Character:
                    if (characters != null && characters.Length > 0 && characters[_index] != null)
                    {
                        mgr.ShowCharacter(characters[_index]);
                        _titleText.text = characters[_index].displayName;
                    }
                    _modeText.text = $"Character ({_index + 1}/{(characters != null ? characters.Length : 0)})";
                    break;

                case CardMode.Ship:
                    if (ships != null && ships.Length > 0 && ships[_index] != null)
                    {
                        mgr.ShowShip(ships[_index]);
                        _titleText.text = ships[_index].displayName;
                    }
                    _modeText.text = $"Ship ({_index + 1}/{(ships != null ? ships.Length : 0)})";
                    break;
            }
        }

        void ClampIndex()
        {
            int count = _mode switch
            {
                CardMode.Monster => monsters != null ? monsters.Length : 0,
                CardMode.Character => characters != null ? characters.Length : 0,
                CardMode.Ship => ships != null ? ships.Length : 0,
                _ => 0
            };

            if (count == 0)
            {
                _index = 0;
                return;
            }

            if (_index < 0) _index = count - 1;
            else if (_index >= count) _index = 0;
        }
    }
}
