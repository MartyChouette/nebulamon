using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nebula
{
    /// <summary>
    /// Central controller for the card display system.
    /// Manages showing/hiding/swapping of Character, Monster, and Ship cards.
    ///
    /// Design principles:
    /// - Single instance per card type (0-1 active at a time)
    /// - Content swapping, not prefab spawning
    /// - Optional background sprite layer support
    /// - Clean separation: manager decides what shows, cards decide how to render
    /// </summary>
    public class CardDisplayManager : MonoBehaviour
    {
        public static CardDisplayManager Instance { get; private set; }

        [Header("Card UI References")]
        [Tooltip("The single CharacterCardUI instance in the scene")]
        [SerializeField] private CharacterCardUI characterCard;

        [Tooltip("The single MonsterCardUI instance in the scene")]
        [SerializeField] private MonsterCardUI monsterCard;

        [Tooltip("The single ShipCardUI instance in the scene")]
        [SerializeField] private ShipCardUI shipCard;

        [Header("Settings")]
        [Tooltip("If true, automatically hides all cards when dialogue ends")]
        [SerializeField] private bool hideCardsOnDialogueEnd = true;

        // Currently displayed data (for reference/queries)
        private CharacterDefinition _activeCharacter;
        private MonsterDefinition _activeMonsterDef;
        private MonsterInstance _activeMonsterInstance;
        private ShipDefinition _activeShip;

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[CardDisplayManager] Duplicate found on '{name}', destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ValidateReferences();

            // Ensure all cards start hidden
            HideAll();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (characterCard == null)
            {
                var found = FindFirstObjectByType<CharacterCardUI>(FindObjectsInactive.Include);
                if (found != null) SetCharacterCard(found);
            }
            if (monsterCard == null)
            {
                var found = FindFirstObjectByType<MonsterCardUI>(FindObjectsInactive.Include);
                if (found != null) SetMonsterCard(found);
            }
            if (shipCard == null)
            {
                var found = FindFirstObjectByType<ShipCardUI>(FindObjectsInactive.Include);
                if (found != null) SetShipCard(found);
            }
        }

        private void ValidateReferences()
        {
            if (characterCard == null)
                Debug.LogWarning("[CardDisplayManager] characterCard is not assigned. Character cards won't display.", this);
            if (monsterCard == null)
                Debug.LogWarning("[CardDisplayManager] monsterCard is not assigned. Monster cards won't display.", this);
            if (shipCard == null)
                Debug.LogWarning("[CardDisplayManager] shipCard is not assigned. Ship cards won't display.", this);
        }

        #endregion

        #region Character Card API

        /// <summary>
        /// Shows the character card with the given data.
        /// Swaps content if already visible, or reveals if hidden.
        /// </summary>
        public void ShowCharacter(CharacterDefinition data)
        {
            if (characterCard == null)
            {
                Debug.LogWarning("[CardDisplayManager] Cannot show character: card UI not assigned.");
                return;
            }

            _activeCharacter = data;
            characterCard.Show(data);
        }

        /// <summary>
        /// Hides the character card.
        /// </summary>
        public void HideCharacter()
        {
            _activeCharacter = null;
            if (characterCard != null)
                characterCard.Hide();
        }

        /// <summary>
        /// Returns true if a character card is currently visible.
        /// </summary>
        public bool IsCharacterVisible => characterCard != null && characterCard.IsVisible;

        /// <summary>
        /// Returns the currently displayed character, or null.
        /// </summary>
        public CharacterDefinition ActiveCharacter => _activeCharacter;

        #endregion

        #region Monster Card API

        /// <summary>
        /// Shows the monster card with definition data (template/base stats).
        /// </summary>
        public void ShowMonster(MonsterDefinition data)
        {
            if (monsterCard == null)
            {
                Debug.LogWarning("[CardDisplayManager] Cannot show monster: card UI not assigned.");
                return;
            }

            _activeMonsterDef = data;
            _activeMonsterInstance = null;
            monsterCard.Show(data);
        }

        /// <summary>
        /// Shows the monster card with instance data (runtime HP, status, etc.).
        /// </summary>
        public void ShowMonster(MonsterInstance instance)
        {
            if (monsterCard == null)
            {
                Debug.LogWarning("[CardDisplayManager] Cannot show monster: card UI not assigned.");
                return;
            }

            _activeMonsterDef = instance?.def;
            _activeMonsterInstance = instance;
            monsterCard.Show(instance);
        }

        /// <summary>
        /// Hides the monster card.
        /// </summary>
        public void HideMonster()
        {
            _activeMonsterDef = null;
            _activeMonsterInstance = null;
            if (monsterCard != null)
                monsterCard.Hide();
        }

        /// <summary>
        /// Refreshes the monster card display (e.g., after HP change).
        /// </summary>
        public void RefreshMonster()
        {
            if (monsterCard != null)
                monsterCard.Refresh();
        }

        /// <summary>
        /// Returns true if a monster card is currently visible.
        /// </summary>
        public bool IsMonsterVisible => monsterCard != null && monsterCard.IsVisible;

        /// <summary>
        /// Returns the currently displayed monster definition, or null.
        /// </summary>
        public MonsterDefinition ActiveMonsterDefinition => _activeMonsterDef;

        /// <summary>
        /// Returns the currently displayed monster instance, or null.
        /// </summary>
        public MonsterInstance ActiveMonsterInstance => _activeMonsterInstance;

        #endregion

        #region Ship Card API

        /// <summary>
        /// Shows the ship card with the given data.
        /// </summary>
        public void ShowShip(ShipDefinition data)
        {
            if (shipCard == null)
            {
                Debug.LogWarning("[CardDisplayManager] Cannot show ship: card UI not assigned.");
                return;
            }

            _activeShip = data;
            shipCard.Show(data);
        }

        /// <summary>
        /// Hides the ship card.
        /// </summary>
        public void HideShip()
        {
            _activeShip = null;
            if (shipCard != null)
                shipCard.Hide();
        }

        /// <summary>
        /// Returns true if a ship card is currently visible.
        /// </summary>
        public bool IsShipVisible => shipCard != null && shipCard.IsVisible;

        /// <summary>
        /// Returns the currently displayed ship, or null.
        /// </summary>
        public ShipDefinition ActiveShip => _activeShip;

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Hides all card types.
        /// </summary>
        public void HideAll()
        {
            HideCharacter();
            HideMonster();
            HideShip();
        }

        /// <summary>
        /// Returns true if any card is currently visible.
        /// </summary>
        public bool AnyCardVisible => IsCharacterVisible || IsMonsterVisible || IsShipVisible;

        #endregion

        #region Dialogue Integration

        /// <summary>
        /// Called by DialogueManager when dialogue ends.
        /// Optionally hides all cards based on settings.
        /// </summary>
        public void OnDialogueEnded()
        {
            if (hideCardsOnDialogueEnd)
            {
                HideAll();
            }
        }

        /// <summary>
        /// Convenience method to show a character during dialogue.
        /// Combines showing the character card with any dialogue-specific behavior.
        /// </summary>
        public void ShowCharacterForDialogue(CharacterDefinition character)
        {
            ShowCharacter(character);
        }

        #endregion

        #region Runtime Card Assignment

        /// <summary>
        /// Allows runtime assignment of card UI references.
        /// Useful when cards are instantiated dynamically or found in scene.
        /// </summary>
        public void SetCharacterCard(CharacterCardUI card)
        {
            characterCard = card;
        }

        /// <summary>
        /// Allows runtime assignment of monster card UI.
        /// </summary>
        public void SetMonsterCard(MonsterCardUI card)
        {
            monsterCard = card;
        }

        /// <summary>
        /// Allows runtime assignment of ship card UI.
        /// </summary>
        public void SetShipCard(ShipCardUI card)
        {
            shipCard = card;
        }

        #endregion
    }
}
