# Nebulamon Project - Architecture & Script Responsibilities

## Overview

Nebulamon is a space-themed monster-catching RPG built in Unity with turn-based battles, exploration, dialogue, and a unified card display system.

---

## Card Display System Architecture

### Design Principles

1. **Single-instance per card type** - Only 0-1 of each card type (Character, Monster, Ship) visible at a time
2. **Content swapping, not spawning** - Cards are persistent UI panels that swap data, not prefab instantiation
3. **Data-driven** - Cards are fed by ScriptableObjects implementing `ICardData`
4. **Optional background layer** - Each card supports an optional background sprite behind the portrait
5. **Clean separation of concerns**:
   - `CardDisplayManager` decides **what** to show
   - Card UI components decide **how** to render
   - ScriptableObjects define **what values** to display

### Data Flow

```
ScriptableObject (CharacterDefinition, MonsterDefinition, ShipDefinition)
         ↓ implements ICardData
CardDisplayManager.Show*()
         ↓ calls
*CardUI.Show(data)
         ↓ sets
UI elements (portrait, name, stats, background)
```

---

## Script Responsibilities Table

### Cards System (`Assets/Scripts/Cards/`)

| Script | Attaches To | Responsibility | Public API |
|--------|-------------|----------------|------------|
| `ICardData.cs` | N/A (interface) | Unified data contract for card display | `DisplayName`, `CardSprite`, `BackgroundSprite` |
| `BaseCardUI.cs` | Card panel root | Base class with common show/hide/background logic | `Hide()`, `IsVisible`, protected `ShowCommon()` |
| `CharacterCardUI.cs` | Character card panel | Displays character portrait, name, title, description | `Show(CharacterDefinition)`, `Hide()`, `Refresh()` |
| `MonsterCardUI.cs` | Monster card panel | Displays monster portrait, element, stats, HP | `Show(MonsterDefinition)`, `Show(MonsterInstance)`, `Hide()`, `Refresh()` |
| `ShipCardUI.cs` | Ship card panel | Displays ship sprite, class, hull/shields/speed/cargo | `Show(ShipDefinition)`, `Hide()`, `Refresh()` |
| `CardDisplayManager.cs` | Persistent manager object | Singleton controller for all card operations | `ShowCharacter()`, `ShowMonster()`, `ShowShip()`, `Hide*()`, `HideAll()` |

### Data Definitions (`Assets/ScriptableObjects/ScriptableObjectScripts/`)

| Script | Attaches To | Responsibility | Implements |
|--------|-------------|----------------|------------|
| `CharacterDefinition.cs` | ScriptableObject asset | NPC/pilot character data | `ICardData` |
| `MonsterDefinition.cs` | ScriptableObject asset | Monster template with stats and moves | `ICardData` |
| `ShipDefinition.cs` | ScriptableObject asset | Ship template with stats and upgrades | `ICardData` |
| `EnemyDefinition.cs` | ScriptableObject asset | Enemy encounter data (pilot + party) | - |
| `MoveDefinition.cs` | ScriptableObject asset | Battle move with costs and effects | - |

### Dialogue System (`Assets/Scripts/Dialogue/`)

| Script | Attaches To | Responsibility | Public API |
|--------|-------------|----------------|------------|
| `DialogueManager.cs` | Persistent manager object | Singleton driving dialogue flow, typewriter, choices, card integration | `StartConversation(speaker, id)` |
| `DialogueUI.cs` | Dialogue canvas | Holds references to UI elements | `SetVisible()`, `ClearChoices()` |
| `DialogueSpeaker.cs` | NPC GameObject | Triggers dialogue when interacted, holds CharacterDefinition | `Interact()`, `StartConversation()` |
| `DialogueLine.cs` | N/A (data struct) | Single line of dialogue text | `speaker`, `text`, `chirpProfile` |
| `DialogueDatabaseCSV.cs` | ScriptableObject asset | Parses CSV dialogue files | `TryGetConversation()` |

### Battle System (`Assets/Scripts/Battle/`)

| Script | Attaches To | Responsibility | Public API |
|--------|-------------|----------------|------------|
| `BattleController.cs` | Battle scene manager | Orchestrates turn-based combat | Coroutine-based battle loop |
| `BattleUI.cs` | Battle canvas | Battle UI bindings (moves, HP, resources) | `ShowMoves4()`, `SetPlayerInputEnabled()` |
| `BattleMoveDetailPanel.cs` | Move detail panel | Shows move info on hover/focus | `Show(MonsterInstance, MoveDefinition)`, `Hide()` |
| `MonsterInstance.cs` | N/A (runtime class) | Runtime monster state (HP, status, pool) | `TryApplyStatus()`, `CanActThisTurn()` |
| `BattleMath.cs` | N/A (static) | Damage calculations, type advantage | Static math functions |
| `ElementPool.cs` | N/A (data struct) | Element resource tracking | `CanAfford()`, `Add()`, `Subtract()` |

### UI System (`Assets/Scripts/UI/`)

| Script | Attaches To | Responsibility | Public API |
|--------|-------------|----------------|------------|
| `MainMenuUI.cs` | Main menu canvas | Menu panel navigation | Button callbacks |
| `OptionsMenuBinder.cs` | Options panel | Settings UI ↔ GameSettings binding | Slider/toggle bindings |
| `InteractPromptUI.cs` | Interact prompt prefab | Shows "Press X to Talk" prompts | `Show(IInteractable)`, `Hide()` |
| `SceneFadeIn.cs` | Fade overlay | Scene transition fade effect | Auto-plays on Start |

### Player & World (`Assets/Scripts/Player/`, `Assets/Scripts/World/`)

| Script | Attaches To | Responsibility |
|--------|-------------|----------------|
| `PlayerShipController2D.cs` | Player ship | Overworld ship movement and input |
| `PlayerCharacterController2D.cs` | Player character | Town/walking movement |
| `PlayerInteractor2D.cs` | Player | Detects and triggers IInteractable |
| `GameSettings.cs` | N/A (static) | Player preferences (audio, accessibility) |
| `EncounterDirector2D.cs` | Overworld manager | Random encounter triggering |
| `EncounterRegion2D.cs` | Trigger volume | Defines encounter tables per area |

### Progression (`Assets/Scripts/Progression/`)

| Script | Attaches To | Responsibility | Public API |
|--------|-------------|----------------|------------|
| `Progression.cs` | Persistent manager | Save/load, money, upgrades, romance | `Save()`, `Load()`, `HasUpgrade()`, etc. |
| `ProgressionData.cs` | N/A (data class) | Serializable save data structure | Enums + data fields |

---

## UI Ownership Model

```
UIRoot (Canvas)
├── DialoguePanel (DialogueUI)
│   └── managed by DialogueManager
├── CardPanels
│   ├── CharacterCardPanel (CharacterCardUI)
│   ├── MonsterCardPanel (MonsterCardUI)
│   └── ShipCardPanel (ShipCardUI)
│   └── all managed by CardDisplayManager
├── BattleUI (BattleUI)
│   └── MoveDetailPanel (BattleMoveDetailPanel)
│   └── managed by BattleController
└── InteractPrompt (InteractPromptUI)
    └── managed by PlayerInteractor2D
```

---

## How To: Add a New Character

1. **Create CharacterDefinition asset**:
   - Right-click in Project → Create → Nebula → Cards → Character Definition
   - Fill in displayName, title, portrait sprite, optional background

2. **Assign to DialogueSpeaker**:
   - On the NPC GameObject, find the `DialogueSpeaker` component
   - Drag the CharacterDefinition into the `characterDefinition` field

3. **Result**: When the player talks to this NPC, their character card automatically shows

---

## How To: Show Cards Programmatically

```csharp
// Show a character card
CardDisplayManager.Instance.ShowCharacter(myCharacterDefinition);

// Show a monster (definition or instance)
CardDisplayManager.Instance.ShowMonster(myMonsterDefinition);
CardDisplayManager.Instance.ShowMonster(myMonsterInstance);

// Show a ship
CardDisplayManager.Instance.ShowShip(myShipDefinition);

// Hide specific cards
CardDisplayManager.Instance.HideCharacter();
CardDisplayManager.Instance.HideMonster();
CardDisplayManager.Instance.HideShip();

// Hide all cards
CardDisplayManager.Instance.HideAll();
```

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Persistent UI panels | Avoids prefab spawning, reduces GC, prevents duplicate UI bugs |
| ICardData interface | Unified contract allows future card types without changing manager |
| Optional background sprite | Supports scene plates/vignettes without requiring them |
| DontDestroyOnLoad singletons | DialogueManager and CardDisplayManager persist across scenes |
| Cards auto-hide on dialogue end | Clean state management; can be disabled in inspector |

---

## Scene Setup Checklist

For the card system to work in a scene:

1. [ ] Add `CardDisplayManager` GameObject (or ensure it persists from another scene)
2. [ ] Create card UI panels in canvas:
   - CharacterCardPanel with `CharacterCardUI` component
   - MonsterCardPanel with `MonsterCardUI` component
   - ShipCardPanel with `ShipCardUI` component
3. [ ] Wire panel references in CardDisplayManager inspector
4. [ ] Each panel needs:
   - Root GameObject reference
   - Portrait Image
   - Name TMP_Text
   - (Optional) Background Image + Background Root
   - Type-specific fields (title, stats, etc.)

---

## File Locations

```
Assets/
├── Scripts/
│   ├── Cards/
│   │   ├── ICardData.cs
│   │   ├── BaseCardUI.cs
│   │   ├── CharacterCardUI.cs
│   │   ├── MonsterCardUI.cs
│   │   ├── ShipCardUI.cs
│   │   └── CardDisplayManager.cs
│   ├── Dialogue/
│   │   ├── DialogueManager.cs (integrated with CardDisplayManager)
│   │   ├── DialogueSpeaker.cs (has CharacterDefinition field)
│   │   └── ...
│   └── ...
├── ScriptableObjects/
│   ├── ScriptableObjectScripts/
│   │   ├── CharacterDefinition.cs
│   │   ├── ShipDefinition.cs
│   │   ├── MonsterDefinition.cs (implements ICardData)
│   │   └── ...
│   └── (asset instances go here)
└── ...
```
