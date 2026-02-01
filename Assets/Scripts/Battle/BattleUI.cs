// Assets/Scripts/Battle/BattleUI.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nebula
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Text")]
        public TMP_Text topText;
        public TMP_Text playerHpText;
        public TMP_Text enemyHpText;

        [Header("Resources (optional)")]
        public TMP_Text playerResourceText;

        [Header("Move Detail Panel (optional)")]
        public BattleMoveDetailPanel moveDetailPanel;

        [Header("Root Menu (Fight/Draw/Run ONLY)")]
        public GameObject rootMenu;
        public Button fightButton;
        public Button drawButton;
        public Button runButton;

        [Header("Moves Menu (Move1-4 + Back ONLY)")]
        public GameObject movesMenu;
        public Button move1Button;
        public Button move2Button;
        public Button move3Button;
        public Button move4Button;
        public Button backButton;

        private MonsterInstance _active;
        private readonly MoveDefinition[] _slotMoves = new MoveDefinition[4];
        private Action<MoveDefinition> _onPicked;

        // Track whether we should accept player input
        private bool _inputEnabled = true;

        private void Awake()
        {
            // Moves menu starts OFF unless used
            if (movesMenu) movesMenu.SetActive(false);
            if (moveDetailPanel) moveDetailPanel.Hide();
        }

        // ---------- HUD text ----------
        public void SetTop(string msg)
        {
            if (topText) topText.text = msg;
        }

        public void SetHP(MonsterInstance player, MonsterInstance enemy)
        {
            if (playerHpText && player?.def != null)
                playerHpText.text = $"{player.def.displayName}  HP {Mathf.Max(0, player.hp)}/{player.def.maxHP}";

            if (enemyHpText && enemy?.def != null)
                enemyHpText.text = $"{enemy.def.displayName}  HP {Mathf.Max(0, enemy.hp)}/{enemy.def.maxHP}";
        }

        public void SetResources(MonsterInstance player)
        {
            if (!playerResourceText || player == null) return;

            var p = player.pool;
            playerResourceText.text = $"Solar {p.solar}   Void {p.voids}   Bio {p.bio}   Time {p.time}";

            if (moveDetailPanel) moveDetailPanel.RefreshResources(player);
        }

        // ---------- NEW: Grey out / enable input ----------
        public void SetPlayerInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            // Root buttons
            if (fightButton) fightButton.interactable = enabled;
            if (drawButton) drawButton.interactable = enabled && drawButton.gameObject.activeSelf;
            if (runButton) runButton.interactable = enabled;

            // Move + back buttons: refresh based on affordability and input state
            RefreshMoveButtonInteractableStates();

            if (!enabled)
            {
                // Don�t show the move detail panel when player can't act
                if (moveDetailPanel) moveDetailPanel.Hide();
            }
        }

        private void RefreshMoveButtonInteractableStates()
        {
            ApplyMoveInteractable(move1Button, 0);
            ApplyMoveInteractable(move2Button, 1);
            ApplyMoveInteractable(move3Button, 2);
            ApplyMoveInteractable(move4Button, 3);

            if (backButton) backButton.interactable = _inputEnabled;
        }

        private void ApplyMoveInteractable(Button b, int slot)
        {
            if (!b) return;

            var m = _slotMoves[slot];
            if (m == null)
            {
                b.interactable = false;
                return;
            }

            bool canAfford = (_active != null) && _active.pool.CanAfford(m);
            b.interactable = _inputEnabled && canAfford;
        }

        // ---------- Menus ----------
        public void ShowRootMenu()
        {
            if (movesMenu) movesMenu.SetActive(false);
            if (rootMenu) rootMenu.SetActive(true);
            if (moveDetailPanel) moveDetailPanel.Hide();
            // keep current input state (don�t auto-enable)
            SetPlayerInputEnabled(_inputEnabled);
        }

        public void ShowMovesMenu()
        {
            if (rootMenu) rootMenu.SetActive(false);
            if (movesMenu) movesMenu.SetActive(true);
            // keep current input state (don�t auto-enable)
            SetPlayerInputEnabled(_inputEnabled);
        }

        public void HideMovesMenu()
        {
            if (movesMenu) movesMenu.SetActive(false);
            if (moveDetailPanel) moveDetailPanel.Hide();
        }

        // ---------- Root button wiring ----------
        public void WireRootButtons(Action onFight, Action onDraw, Action onRun)
        {
            if (fightButton)
            {
                fightButton.onClick.RemoveAllListeners();
                fightButton.onClick.AddListener(() =>
                {
                    if (!_inputEnabled) return;
                    onFight?.Invoke();
                });
            }

            if (drawButton)
            {
                drawButton.onClick.RemoveAllListeners();
                drawButton.onClick.AddListener(() =>
                {
                    if (!_inputEnabled) return;
                    onDraw?.Invoke();
                });
                drawButton.gameObject.SetActive(onDraw != null);
            }

            if (runButton)
            {
                runButton.onClick.RemoveAllListeners();
                runButton.onClick.AddListener(() =>
                {
                    if (!_inputEnabled) return;
                    onRun?.Invoke();
                });
            }

            SetPlayerInputEnabled(_inputEnabled);
        }

        // ---------- Moves menu ----------
        public void ShowMoves4(MonsterInstance active, Action<MoveDefinition> onPicked, Action onBack)
        {
            _active = active;
            _onPicked = onPicked;

            ShowMovesMenu();

            for (int i = 0; i < 4; i++) _slotMoves[i] = null;

            if (active?.def?.moves != null)
            {
                for (int i = 0; i < Mathf.Min(4, active.def.moves.Count); i++)
                    _slotMoves[i] = active.def.moves[i];
            }

            SetupMoveButton(move1Button, 0);
            SetupMoveButton(move2Button, 1);
            SetupMoveButton(move3Button, 2);
            SetupMoveButton(move4Button, 3);

            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    if (!_inputEnabled) return;
                    if (moveDetailPanel) moveDetailPanel.Hide();
                    onBack?.Invoke();
                });
            }

            // apply current enabled/disabled state
            SetPlayerInputEnabled(_inputEnabled);

            // auto select first interactable for gamepad highlight (only if enabled)
            if (_inputEnabled) SelectFirstInteractableMove();
        }

        private void SetupMoveButton(Button b, int slot)
        {
            if (!b) return;

            b.onClick.RemoveAllListeners();

            var m = _slotMoves[slot];
            var label = b.GetComponentInChildren<TMP_Text>();

            if (m == null)
            {
                if (label) label.text = "�";
                b.interactable = false;
                BindHighlight(b, null);
                return;
            }

            if (label) label.text = m.moveName;

            BindHighlight(b, m);

            b.onClick.AddListener(() =>
            {
                if (!_inputEnabled) return;

                bool canUse = (_active != null) && _active.pool.CanAfford(m);
                if (!canUse)
                {
                    SetTop("Not enough element energy!");
                    return;
                }

                // When chosen, leave menus visible but disable input during resolution
                if (moveDetailPanel) moveDetailPanel.Hide();
                _onPicked?.Invoke(m);
            });
        }

        private void BindHighlight(Button b, MoveDefinition move)
        {
            if (moveDetailPanel == null || b == null) return;

            var hi = b.GetComponent<BattleMoveButtonHighlight>();
            if (!hi) hi = b.gameObject.AddComponent<BattleMoveButtonHighlight>();

            hi.Bind(moveDetailPanel, _active, move);
        }

        private void SelectFirstInteractableMove()
        {
            if (EventSystem.current == null) return;

            GameObject pick = null;

            if (move1Button && move1Button.interactable) pick = move1Button.gameObject;
            else if (move2Button && move2Button.interactable) pick = move2Button.gameObject;
            else if (move3Button && move3Button.interactable) pick = move3Button.gameObject;
            else if (move4Button && move4Button.interactable) pick = move4Button.gameObject;
            else if (backButton && backButton.interactable) pick = backButton.gameObject;

            if (pick != null)
                EventSystem.current.SetSelectedGameObject(pick);
        }
    }
}
