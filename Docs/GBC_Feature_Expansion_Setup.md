# GBC-Era Feature Expansion -- Complete Setup Guide

This document covers every system added in the 14-phase expansion: what each file does, how the data flows, how to configure ScriptableObjects in the Unity Inspector, and how to wire up every UI panel in your scenes.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [ScriptableObject Setup](#2-scriptableobject-setup)
3. [Monster System (Levels, Stats, XP)](#3-monster-system)
4. [Battle System Wiring](#4-battle-system-wiring)
5. [Roster, Party & Inventory](#5-roster-party--inventory)
6. [Multi-Monster Battle (Switch)](#6-multi-monster-battle-switch)
7. [Items & Catching](#7-items--catching)
8. [Town Services](#8-town-services)
9. [Evolution](#9-evolution)
10. [Bestiary](#10-bestiary)
11. [Trainer NPCs](#11-trainer-npcs)
12. [Visual Effects (GBC Palette, Scanlines)](#12-visual-effects)
13. [Screen Transition Wipes](#13-screen-transition-wipes)
14. [Day/Night Cycle](#14-daynight-cycle)
15. [Save Slots](#15-save-slots)
16. [Speed Boost & Overworld HUD](#16-speed-boost--overworld-hud)
17. [Editor Tooling](#17-editor-tooling)
18. [Formula Reference](#18-formula-reference)
19. [Full File Index](#19-full-file-index)

---

## 1. Architecture Overview

### Data Flow

```
ScriptableObjects (MonsterDefinition, ItemDefinition, EnemyDefinition, BattleConfig, etc.)
    |
    v
Catalogs (MonsterCatalog, ItemCatalog -- singleton lookup tables)
    |
    v
Runtime Classes (MonsterInstance, BattleSide, ElementPool)
    |
    v
Controllers (BattleController, BattleScreenBootstrapper)
    |
    v
UI Components (BattleUI, BattlePartyPickerUI, BattleItemPickerUI, etc.)
    |
    v
Persistence (Progression / ProgressionData -- JSON save file)
```

### Singleton Pattern

Several systems use a static `Instance` property:

| Class | Lifecycle | Set By |
|-------|-----------|--------|
| `BattleConfig.Instance` | Scene | Inspector (self-assigns in field init or bootstrapper) |
| `MonsterCatalog.Instance` | Scene | `BattleScreenBootstrapper.Start()` |
| `ItemCatalog.Instance` | Scene | Must be assigned manually (see below) |
| `GameFlowManager.Instance` | DontDestroyOnLoad | `Awake()` |
| `DialogueManager.Instance` | DontDestroyOnLoad | `Awake()` |
| `ScreenEffects.Instance` | Scene | `Awake()` |
| `TransitionWipeController.Instance` | Scene/Camera | `Awake()` |
| `EvolutionCutsceneUI.Instance` | Scene | `Awake()` |

---

## 2. ScriptableObject Setup

### 2.1 BattleConfig

**Create:** Right-click in Project > Create > Nebula > Battle > Battle Config

This is the central balance knob file. One instance per project.

| Section | Key Fields | Purpose |
|---------|------------|---------|
| Type Advantage | `strongMultiplier` (2.0), `weakMultiplier` (0.5) | Element damage scaling |
| STAB & Crits | `stabBonus` (1.25), `critMultiplier` (1.5) | Same-type attack bonus |
| Accuracy | `accBase` (0.75), `accScaling` (0.25), `luckNudgePerPoint` (0.0025) | Hit chance formula |
| Element Pool | `baseElementStock` (2), `nativeElementBonus` (2) | Starting element resources |
| XP & Leveling | `baseXpThreshold` (20), `xpPerLevel` (8), `maxLevel` (50) | Leveling curve |
| Catching | `baseCatchRate` (0.3), `hpCatchFactor` (0.5), `statusCatchBonus` (0.15), `maxRosterSize` (30) | Catch formula |

**How to wire:**
1. Create the asset
2. On `BattleController` in BattleScreen scene, drag it into the `Battle Config` slot
3. `BattleConfig.Instance` is set automatically by the controller

### 2.2 MonsterDefinition

**Create:** Right-click > Create > Nebula > Battle > Monster Definition

Each monster species gets one asset. Key fields added by the expansion:

| Field | Type | Purpose |
|-------|------|---------|
| `monsterId` | `MonsterId` enum | Unique ID for save serialization |
| `hpGrowth` .. `luckGrowth` | float | Stat gain per level (formula: `base + growth * (level-1)`) |
| `evolvedForm` | `MonsterDefinition` | Reference to the evolved species (null = no evolution) |
| `baseXpYield` | int (default 30) | XP awarded when this species is defeated |
| `bestiaryDescription` | string (TextArea) | Shown in the Bestiary when caught |

**Important:** Every MonsterDefinition must have a unique `monsterId` set. The `MonsterId` enum is in `ProgressionData.cs`. Add new entries as you add monsters.

### 2.3 MonsterCatalog

**Create:** Right-click > Create > Nebula > Data > Monster Catalog

A single master list of all monsters.

1. Create one asset (e.g., `Assets/ScriptableObjects/MonsterCatalog.asset`)
2. Drag every MonsterDefinition into the `allMonsters` list
3. Drag this asset into `BattleScreenBootstrapper.monsterCatalog` in BattleScreen scene

`MonsterCatalog.Instance` is set by `BattleScreenBootstrapper.Start()`. Other scenes that need it (town, overworld HUD) must also set it -- add a bootstrap MonoBehaviour that assigns it on Awake:

```csharp
public class CatalogBootstrap : MonoBehaviour
{
    public MonsterCatalog monsterCatalog;
    public ItemCatalog itemCatalog;

    void Awake()
    {
        if (monsterCatalog) MonsterCatalog.Instance = monsterCatalog;
        if (itemCatalog) ItemCatalog.Instance = itemCatalog;
    }
}
```

### 2.4 ItemDefinition

**Create:** Right-click > Create > Nebula > Items > Item Definition

Each item type gets one asset. The `category` field controls which sub-fields are relevant:

| Category | Relevant Fields |
|----------|----------------|
| Heal | `healAmount`, `healStatus`, `healAllParty` |
| CatchDevice | `catchRateBonus` (0.0 = base rate only, 0.5 = significant bonus) |
| Evolution | `targetMonster` (which monster it works on), `evolvedInto` (result) |
| MoveTutor | `taughtMove` (MoveDefinition), `compatibleMonsters` (MonsterId list) |
| KeyItem | No special fields (quest items) |
| Misc | No special fields (sell-only items) |

**Every item needs a unique `itemId` string.** This is what gets saved to the inventory.

The custom editor (`ItemDefinitionEditor`) hides irrelevant fields based on category.

### 2.5 ItemCatalog

**Create:** Right-click > Create > Nebula > Data > Item Catalog

Master list of all items. Works like MonsterCatalog.

1. Create one asset
2. Drag all ItemDefinitions into `allItems`
3. Set `ItemCatalog.Instance` via bootstrap (see CatalogBootstrap above)

### 2.6 ShopInventory

**Create:** Right-click > Create > Nebula > Data > Shop Inventory

Per-shop item list. Each town shop gets one.

1. Create an asset per shop (e.g., `SpacePort_Shop.asset`)
2. Drag the ItemDefinitions that shop sells into `itemsForSale`
3. Assign to the `ItemShopUI.shopInventory` field in the scene

### 2.7 EnemyDefinition

**Existing asset**, now has additional fields:

| Field | Type | Purpose |
|-------|------|---------|
| `rewardMoney` | int (default 50) | Money awarded on victory |
| `trainerId` | string | Unique ID for defeat tracking (leave empty for wild encounters) |
| `isTrainer` | bool | If true, catching is blocked and defeat is tracked |

---

## 3. Monster System

### MonsterInstance (Runtime)

`MonsterInstance` is the runtime state of one monster in battle. Created from a `MonsterDefinition` + level:

```csharp
var monster = new MonsterInstance(someDef, level: 10);
// monster.hp == EffectiveMaxHP() (full health)
// monster.knownMoves == copy of someDef.moves
// monster.pool == starting element resources
```

**Effective stat formula (all stats):**
```
EffectiveStat = Max(1, Round(baseStat + growthRate * (level - 1)))
```

**XP & Leveling:**
```
XP needed per level = baseXpThreshold + xpPerLevel * currentLevel
```
- On level up, HP is fully restored
- `TryGainXP(amount, out levelsGained)` handles all leveling logic
- Max level is `BattleConfig.maxLevel` (default 50)

**Evolution:**
```csharp
monster.Evolve(newMonsterDef);
// - Preserves HP ratio (if was at 50% HP, stays at 50% of new max)
// - Merges new moves from evolved form's move list
// - Swaps the def reference
```

---

## 4. Battle System Wiring

### BattleScreen Scene Hierarchy

```
BattleScreen (Scene)
├── BattleManager (GameObject)
│   ├── BattleController (component)
│   ├── BattleScreenBootstrapper (component)
│   └── BattleConfig (assign ScriptableObject)
│
├── BattleCanvas (Canvas)
│   ├── BattleUI (component on root)
│   │   ├── TopText (TMP_Text)
│   │   ├── PlayerHPText (TMP_Text)
│   │   ├── EnemyHPText (TMP_Text)
│   │   ├── PlayerResourceText (TMP_Text) [optional]
│   │   ├── MoveDetailPanel (BattleMoveDetailPanel) [optional]
│   │   │
│   │   ├── RootMenu (GameObject)
│   │   │   ├── FightButton (Button)
│   │   │   ├── DrawButton (Button)
│   │   │   ├── RunButton (Button)
│   │   │   ├── SwitchButton (Button) ← NEW
│   │   │   └── ItemButton (Button) ← NEW
│   │   │
│   │   └── MovesMenu (GameObject)
│   │       ├── Move1Button .. Move4Button (Buttons)
│   │       └── BackButton (Button)
│   │
│   ├── PartyPickerPanel (BattlePartyPickerUI) ← NEW
│   │   ├── panel (GameObject, starts inactive)
│   │   ├── slots[0..2] (PartySlot: root, nameText, levelText, hpBar, selectButton)
│   │   └── backButton (Button)
│   │
│   ├── ItemPickerPanel (BattleItemPickerUI) ← NEW
│   │   ├── panel (GameObject, starts inactive)
│   │   ├── slots[0..5] (ItemSlot: root, icon, nameText, quantityText, selectButton)
│   │   └── backButton (Button)
│   │
│   └── TurnHUD (BattleTurnHudUI) [optional]
│
├── ScreenEffects (ScreenEffects component)
├── TransitionWipeController (on Camera or separate GO)
└── Camera
```

### BattleController Inspector Setup

| Field | What to assign |
|-------|----------------|
| `ui` | The BattleUI component on BattleCanvas |
| `vfx` | BattleVfxController (optional, null-safe) |
| `turnHud` | BattleTurnHudUI (optional) |
| `battleConfig` | Your BattleConfig ScriptableObject asset |
| `partyPicker` | The BattlePartyPickerUI component (optional -- no switch without it) |
| `itemPicker` | The BattleItemPickerUI component (optional -- no items without it) |

### BattleScreenBootstrapper Inspector Setup

| Field | What to assign |
|-------|----------------|
| `battle` | The BattleController component |
| `fallbackPlayerMonster` | A MonsterDefinition to use when Progression has no roster |
| `playerPilotSprite` | Player's pilot portrait (for battle intro) |
| `playerShipSprite` | Player's ship sprite (for battle intro) |
| `monsterCatalog` | Your MonsterCatalog ScriptableObject asset |

### BattleUI Inspector Setup

| Field | What to assign |
|-------|----------------|
| `topText` | TMP_Text for battle messages |
| `playerHpText` | TMP_Text showing player monster HP |
| `enemyHpText` | TMP_Text showing enemy monster HP |
| `playerResourceText` | TMP_Text showing element pool (optional) |
| `moveDetailPanel` | BattleMoveDetailPanel component (optional) |
| `rootMenu` | Root menu GameObject |
| `fightButton` | Fight button |
| `drawButton` | Draw button |
| `runButton` | Run button |
| `switchButton` | **NEW** -- Switch button (hidden when party size = 1) |
| `itemButton` | **NEW** -- Item button (hidden when no usable items) |
| `movesMenu` | Moves menu GameObject |
| `move1Button` .. `move4Button` | Move slot buttons |
| `backButton` | Back button in moves menu |

### BattlePartyPickerUI Inspector Setup

| Field | What to assign |
|-------|----------------|
| `panel` | Root panel GameObject (starts inactive) |
| `slots` | Array of `PartySlot` (up to 2 non-active party members) |
| `backButton` | Button to cancel and return to root menu |

Each `PartySlot` has:
- `root` -- slot container GameObject
- `nameText` -- TMP_Text for monster name
- `levelText` -- TMP_Text for level
- `hpBar` -- Slider for HP
- `selectButton` -- Button to select this monster

### BattleItemPickerUI Inspector Setup

| Field | What to assign |
|-------|----------------|
| `panel` | Root panel GameObject (starts inactive) |
| `slots` | Array of `ItemSlot` (however many item slots you want visible) |
| `backButton` | Button to cancel |

Each `ItemSlot` has:
- `root` -- slot container GameObject
- `icon` -- Image for item sprite
- `nameText` -- TMP_Text for item name
- `quantityText` -- TMP_Text for "x3" quantity
- `selectButton` -- Button to use this item

---

## 5. Roster, Party & Inventory

### How Roster Works

The player's monster collection is stored in `ProgressionData`:

```
ProgressionData
├── roster: List<OwnedMonster>    // All owned monsters (up to maxRosterSize)
├── partyIndices: List<int>       // Which roster indices are in the active party (max 3)
└── inventory: List<InventorySlot> // itemId + quantity pairs
```

**OwnedMonster** stores:
- `monsterId` -- enum, maps to MonsterDefinition via MonsterCatalog
- `level`, `xp`, `currentHp` -- persistent state
- `knownMoveNames` -- List of move name strings
- `nickname` -- optional display name override

### Progression API

```csharp
// Roster
Progression.AddToRoster(ownedMonster);
List<OwnedMonster> party = Progression.GetParty();
Progression.SetPartyOrder(new List<int> { 0, 2, 1 });
OwnedMonster entry = Progression.GetRosterEntry(index);
int count = Progression.RosterCount;

// Inventory
Progression.AddItem("potion_basic", 3);
bool ok = Progression.RemoveItem("potion_basic", 1);
int qty = Progression.GetItemCount("potion_basic");
List<InventorySlot> inv = Progression.GetInventory();

// Bestiary
Progression.MarkSeen(MonsterId.Monster1);
Progression.MarkCaught(MonsterId.Monster1);
bool seen = Progression.HasSeen(MonsterId.Monster1);
int seenTotal = Progression.SeenCount();
int caughtTotal = Progression.CaughtCount();

// Trainers
Progression.MarkTrainerDefeated("pirate_captain_01");
bool beaten = Progression.IsTrainerDefeated("pirate_captain_01");

// Save Slots
Progression.LoadSlot(1);            // Load slot 1
Progression.DeleteSlot(2);          // Delete slot 2
bool exists = Progression.SlotExists(1);
var preview = Progression.GetSlotPreview(1); // Non-destructive peek
int slot = Progression.ActiveSlot;
```

### Battle-to-Roster Persistence

At battle end and on run, `BattleController.PersistPlayerPartyToRoster()` writes the current MonsterInstance state (hp, level, xp, knownMoves) back to the matching `OwnedMonster` in `Progression.Data.roster`.

---

## 6. Multi-Monster Battle (Switch)

### How It Works

1. `BattleController.HandlePlayerTurn()` wires the Switch button via `BattleUI.WireRootButtons(onSwitch: ...)`
2. The switch callback is only provided when `_player.AliveCount > 1` AND `partyPicker != null`
3. When player clicks Switch, `BattlePartyPickerUI.Show()` opens with alive non-active monsters
4. Player picks a monster -> `_player.SwitchTo(index)` swaps the active monster
5. The switch consumes the player's turn (enemy still acts)

### BattleSide API

```csharp
side.Active            // Current active MonsterInstance
side.AliveCount        // Number of non-dead party members
side.AllDead           // True if entire party is KO'd
side.SwitchTo(2)       // Switch to party index 2 (returns false if dead)
side.TryAdvanceToNextAlive() // Auto-advance after KO
side.GetAliveIndicesExcept(0) // List of alive indices excluding index 0
```

---

## 7. Items & Catching

### Battle Item Flow

1. Player clicks Item button -> `BattleItemPickerUI.Show()` opens
2. UI reads `Progression.GetInventory()` and `ItemCatalog.Instance` to build the list
3. Only `Heal` and `CatchDevice` categories are shown in battle
4. Player picks an item -> `BattleController.HandleItemUse()` runs:

**Heal items:**
- Restores `item.healAmount` HP (capped at EffectiveMaxHP)
- Consumed from inventory
- Uses the player's turn

**Catch devices:**
- Blocked in trainer battles (shows "Can't catch trainer's monsters!")
- Blocked when roster is full (shows "Roster is full!")
- Consumed from inventory regardless of success
- Catch chance = `baseCatchRate + catchRateBonus + hpBonus + statusBonus`
- Success: creates OwnedMonster, adds to roster, marks caught, battle ends
- Failure: "It broke free!" message, enemy gets their turn

### Catch Chance Formula

```
hpBonus = (1 - currentHP / maxHP) * hpCatchFactor
statusBonus = statusCatchBonus (if target has any status)
finalChance = Clamp01(baseCatchRate + deviceBonus + hpBonus + statusBonus)
```

With defaults: a full-health monster with no status and a basic device (bonus 0) has 30% catch rate. A half-health statused monster with a Great Device (bonus 0.3) has ~85%.

---

## 8. Town Services

### TownServiceHub

Already exists. Routes `DoorKind` to panels via `SetActive()`. The new UI components attach to those panels.

### 8.1 HealCenterUI

**Attach to:** the `healPanel` referenced by TownServiceHub

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `slots` | Array of `PartySlot` (root, nameText, levelText, hpBar) -- up to 3 |
| `healAllButton` | Button that heals entire party |
| `closeButton` | Button that closes the panel |
| `messageText` | TMP_Text for status messages |

**Behavior:**
- On panel enable (via TownServiceHub.Open), reads party from `Progression.Data`
- Uses `MonsterCatalog.Instance` to look up definitions and calculate max HP
- "Heal All" sets every party member's `currentHp` to their effective max HP
- Calls `Progression.Save()` after healing
- Disables "Heal All" if everyone is already full

### 8.2 ItemShopUI

**Attach to:** the `genericShopPanel` referenced by TownServiceHub

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `shopInventory` | ShopInventory ScriptableObject for this shop |
| `buyTab` / `sellTab` | Tab buttons to switch modes |
| `slots` | Array of `ShopSlot` (root, icon, nameText, priceText, quantityText, actionButton) |
| `moneyText` | TMP_Text showing current money |
| `messageText` | TMP_Text for feedback |
| `closeButton` | Button to close |

**Behavior:**
- **Buy mode:** Shows items from `ShopInventory.itemsForSale`. Button buys 1 unit. Checks `Progression.SpendMoney()`.
- **Sell mode:** Shows player inventory (items with `sellPrice > 0`). Button sells 1 unit. Calls `Progression.AddMoney()`.
- Both modes save after each transaction.

### 8.3 MoveTutorUI

**Attach to:** the `skillShopPanel` referenced by TownServiceHub

This is a 3-step wizard:

**Step 1 -- Item List:** Shows MoveTutor items from player inventory
**Step 2 -- Monster List:** Shows compatible party monsters (filtered by `item.compatibleMonsters`)
**Step 3 -- Move Slot:** Shows the monster's current 4 move slots; pick one to replace

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `itemListPanel` | GameObject for step 1 |
| `tutorSlots` | Array of `TutorSlot` (root, icon, nameText, descText, selectButton) |
| `monsterListPanel` | GameObject for step 2 |
| `monsterSlots` | Array of `MonsterSlot` (root, nameText, levelText, selectButton) |
| `monsterBackButton` | Back button for step 2 |
| `moveSlotPanel` | GameObject for step 3 |
| `moveSlots` | Array of `MoveSlot` (root, moveNameText, selectButton) -- exactly 4 |
| `moveBackButton` | Back button for step 3 |
| `messageText` | TMP_Text for feedback |
| `closeButton` | Close button |

**Behavior:**
- Consumes the MoveTutor item on successful teach
- Replaces the move name in `OwnedMonster.knownMoveNames`
- Skips monsters that already know the move
- If `compatibleMonsters` list is empty, all party members are eligible

---

## 9. Evolution

### EvolutionManager (Static Helper)

```csharp
bool success = EvolutionManager.TryEvolve(rosterIndex, evolutionItem, out string message);
```

**Flow:**
1. Validates the item is `ItemCategory.Evolution` with `evolvedInto` set
2. Checks `targetMonster` compatibility (if set)
3. Consumes the item from inventory
4. Updates the roster entry: swaps `monsterId`, merges new moves
5. Marks the new form as caught in the bestiary
6. Saves

### EvolutionCutsceneUI

**Singleton.** Plays a visual cutscene during evolution.

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `panel` | Root panel (starts inactive) |
| `monsterImage` | Image component to show sprites |
| `evolutionText` | TMP_Text for messages |
| `continueButton` | Button to dismiss |
| `flashDuration` | 0.3 (seconds) |
| `shakeDuration` | 0.4 (seconds) |
| `shakeIntensity` | 15 |
| `holdDuration` | 1.5 (seconds before flash) |

**Usage from code:**

```csharp
// After EvolutionManager.TryEvolve succeeds:
yield return EvolutionCutsceneUI.Instance.PlayEvolution(oldDef, newDef);
```

The cutscene shows the old sprite, pauses, flashes + shakes (via ScreenEffects), swaps to the new sprite, then waits for player input.

### Creating an Evolution Item

1. Create an ItemDefinition asset
2. Set `category` = Evolution
3. Set `targetMonster` = the MonsterDefinition that can evolve
4. Set `evolvedInto` = the MonsterDefinition it becomes
5. Set `consumable` = true
6. Give it a unique `itemId`

Also set `MonsterDefinition.evolvedForm` on the base monster to point to the evolved form (used by the editor for the evolution chain display).

---

## 10. Bestiary

### BestiaryUI

Full-screen panel showing all monsters from `MonsterCatalog`.

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `panel` | Root panel |
| `entrySlots` | Array of `EntrySlot` (root, portrait, nameText, elementText, statsText, descriptionText) |
| `headerText` | TMP_Text showing "Bestiary Seen: X Caught: Y" |
| `prevPageButton` / `nextPageButton` | Navigation buttons |
| `closeButton` | Close button |
| `silhouetteColor` | Color for seen-but-not-caught sprites (default: black) |

**Display logic per monster:**

| State | Portrait | Name | Element | Stats | Description |
|-------|----------|------|---------|-------|-------------|
| Not seen | Transparent | "???" | Hidden | Hidden | Hidden |
| Seen (not caught) | Silhouette (black tint) | Shown | Shown | Hidden | Hidden |
| Caught | Full color | Shown | Shown | Base stats | bestiaryDescription |

**Pagination:** `entrySlots.Length` entries per page. Total pages = ceil(allMonsters.Count / slotsPerPage).

**How to open:** Call `bestiaryUI.Show()` from a menu button or key binding.

---

## 11. Trainer NPCs

### TrainerNPC

**Attach to:** NPC GameObjects in town/overworld scenes.

**Inspector fields:**

| Field | What to assign |
|-------|----------------|
| `enemyDefinition` | EnemyDefinition asset (must have `isTrainer = true` and a unique `trainerId`) |
| `defeatedDialogue` | String shown when re-interacting after defeat |
| `prompt` | Interact prompt text (default: "Challenge") |

**Behavior:**
- Implements `IInteractable`
- On interact: checks `Progression.IsTrainerDefeated(trainerId)`
  - If defeated: shows `defeatedDialogue` via `DialogueManager.ShowSingleLine()`
  - If not defeated: starts battle via `GameFlowManager.Instance.StartBattle()`
- After battle win, `BattleController.EndBattle()` calls `Progression.MarkTrainerDefeated(trainerId)`

### EncounterDirector2D Change

Random encounters now skip defeated trainers:

```csharp
if (enemy.isTrainer && !string.IsNullOrEmpty(enemy.trainerId)
    && Progression.IsTrainerDefeated(enemy.trainerId))
    return; // skip, don't start battle
```

---

## 12. Visual Effects

### 12.1 GBC Palette Post-Processing

**Setup:**
1. Add `GBCPostProcess` component to your **Main Camera**
2. Assign the `GBCPalette` shader (`Assets/Shaders/GBCPalette.shader`) to the `gbcShader` field
3. The component ships with 3 built-in palette presets:
   - **Classic Green** -- original Game Boy green tones
   - **Grayscale** -- 4-shade gray
   - **Pocket** -- Game Boy Pocket palette

**Runtime control:**
```csharp
GameSettings.SetBool(GameSettings.Keys.GBCPaletteEnabled, true);
GameSettings.SetInt(GameSettings.Keys.GBCPaletteIndex, 0); // 0=Green, 1=Gray, 2=Pocket
```

**Custom palettes:** Add more entries to the `presets` array in the Inspector. Each preset has 4 colors (darkest to lightest).

**How the shader works:** Converts each pixel to luminance, then maps to 1 of 4 palette colors based on brightness thresholds (0.25, 0.5, 0.75).

### 12.2 Scanline Overlay

**Setup:**
1. Add `ScanlineOverlay` component to your **Main Camera**
2. Assign `ScanlineOverlay.shader` (`Assets/Shaders/ScanlineOverlay.shader`) to `scanlineShader`
3. Adjust `lineWidth` (default 2 pixels) and `darkness` (default 0.3)

**Runtime control:**
```csharp
GameSettings.SetBool(GameSettings.Keys.ScanlineEnabled, true);
```

**Render order:** If both GBC palette and scanlines are on the same camera, they execute in component order. Put `GBCPostProcess` first (palette quantizes), then `ScanlineOverlay` (adds lines on top).

### Options Menu Integration

Add toggles to your options panel that call:
```csharp
GameSettings.SetBool(GameSettings.Keys.GBCPaletteEnabled, toggle.isOn);
GameSettings.SetBool(GameSettings.Keys.ScanlineEnabled, toggle.isOn);
GameSettings.SetBool(GameSettings.Keys.DayNightEnabled, toggle.isOn);
```

The palette index can be a dropdown:
```csharp
GameSettings.SetInt(GameSettings.Keys.GBCPaletteIndex, dropdown.value);
```

---

## 13. Screen Transition Wipes

### TransitionWipeController

**Setup:**
1. Add `TransitionWipeController` component to your **Main Camera** (or a persistent camera)
2. Assign `TransitionWipe.shader` to `wipeShader`
3. Configure defaults: `wipeColor` (black), `bandCount` (8), `columnCount` (10)

**Wipe types:**

| Type | Visual |
|------|--------|
| `Iris` | Circular mask from center, like classic Pokemon |
| `Blinds` | Horizontal bands that fill in |
| `ColumnDissolve` | Staggered vertical columns dropping down |

### ScreenEffects Wipe Methods

```csharp
yield return ScreenEffects.Instance.IrisWipeOut(0.5f);  // Cover screen
yield return ScreenEffects.Instance.IrisWipeIn(0.5f);   // Reveal screen
yield return ScreenEffects.Instance.BlindsWipe(0.5f, isIn: false);
yield return ScreenEffects.Instance.ColumnDissolve(0.6f, isIn: true);
```

All methods fall back to `FadeOut/FadeIn` if `TransitionWipeController.Instance` is null.

### GameFlowManager Integration

`StartBattle()` and `ReturnToOverworld()` now use iris wipe transitions automatically:
1. Iris wipe out (0.5s)
2. Load scene
3. Iris wipe in (0.5s)

---

## 14. Day/Night Cycle

### DayNightCycle

**Setup:**
1. Create a full-screen `Image` in your overworld Canvas (should cover entire screen, raycast target OFF)
2. Add `DayNightCycle` component to a persistent GameObject
3. Assign the full-screen Image to `tintOverlay`
4. Configure the gradient or use the default:
   - Midnight (0.0, 1.0): Blue tint, high alpha
   - Sunrise (0.25): Orange, medium alpha
   - Noon (0.5): White/clear, zero alpha
   - Sunset (0.75): Orange, medium alpha
5. Set `maxAlpha` (default 0.25 -- subtle tint)
6. Add excluded scenes: `"BattleScreen"`, `"Menu"` etc.

**Runtime control:**
```csharp
GameSettings.SetBool(GameSettings.Keys.DayNightEnabled, true);
```

**Time mapping:** Uses `System.DateTime.Now` mapped to a 0..1 fraction of the 24-hour day.

---

## 15. Save Slots

### How It Works

- Slot 0 (default): `save_progression.json`
- Slot 1+: `save_slot_1.json`, `save_slot_2.json`, etc.
- `Progression.ActiveSlot` tracks which slot is loaded
- `LoadSlot(n)` clears current data and loads from slot n
- `GetSlotPreview(n)` reads a slot's JSON without activating it

### SaveSlotPickerUI

**Setup:**
1. Create a panel with 3 slot buttons
2. Add `SaveSlotPickerUI` component
3. Wire up:

| Field | What to assign |
|-------|----------------|
| `panel` | Root panel GameObject |
| `slotButtons` | Array of 3 `SlotButton` (button, labelText, detailText) |
| `backButton` | Back/cancel button |

Each slot shows:
- **Empty slot:** "Slot N / Empty"
- **Existing save:** "Slot N / Starter: X Money: Y Party: Z Time: Xh Ym"

### MainMenuUI Integration

The `MainMenuUI` now has an optional `slotPicker` field:

1. Drag your `SaveSlotPickerUI` component into `MainMenuUI.slotPicker`
2. When "Start" is pressed, the slot picker opens first
3. Player picks a slot -> `Progression.LoadSlot(slot)` -> scene loads

If `slotPicker` is null, the old behavior (immediate scene load) is preserved.

---

## 16. Speed Boost & Overworld HUD

### Speed Boost (PlayerShipController2D)

**New fields:**
- `boostMultiplier` (float, default 1.8) -- multiplies force and speed cap while held
- `boostHeld` (bool, read-only output)

**Setup:**
1. In your Input Actions asset, add a "Boost" action (Button type)
2. Bind it to Left Shift (keyboard) / Left Trigger (gamepad)
3. The controller auto-finds it: `actions.FindAction("Boost", false)`

When the Boost action is held:
- `moveForce` is multiplied by `boostMultiplier`
- `maxSpeed` cap is multiplied by `boostMultiplier`

### OverworldHudUI

**Setup:**
1. Create a HUD panel in your overworld Canvas
2. Add `OverworldHudUI` component
3. Wire up:

| Field | What to assign |
|-------|----------------|
| `partyCards` | Array of up to 3 `PartyMiniCard` (root, nameText, levelText, hpBar) |
| `moneyText` | TMP_Text for "Money: 500" |
| `locationText` | TMP_Text for area name (set via `SetLocation()`) |
| `autoHide` | true (hides during dialogue/battle) |

**Behavior:**
- Subscribes to `Progression.OnChanged` to auto-refresh
- Shows party member names, levels, HP bars from save data
- Auto-hides when DialogueManager is open or in BattleScreen scene

---

## 17. Editor Tooling

### MonsterDefinitionEditor (Enhanced)

The custom inspector now shows:

1. **Base stat bars** (existing)
2. **Growth rate bars** -- dimmer color, scaled to max 5.0
3. **"Effective at Level X" preview** -- slider from 1..50, shows calculated stats at that level
4. **Evolution chain** -- displays "MonsterA -> MonsterB -> MonsterC" if evolvedForm references are set

No setup needed -- automatically used for all MonsterDefinition assets.

### ItemDefinitionEditor

Shows only the fields relevant to the item's category:
- Heal items see: healAmount, healStatus, healAllParty
- CatchDevice items see: catchRateBonus
- Evolution items see: targetMonster, evolvedInto
- MoveTutor items see: taughtMove, compatibleMonsters
- KeyItem/Misc see: "(No category-specific fields)"

### RosterDebugWindow

**Open:** Menu bar > Nebula > Debug > Roster Debug

**Requires Play Mode.** Shows:
- Active save slot, money, roster count, party indices
- Scrollable roster list with remove buttons
- "Add Monster" form (pick MonsterId + level)
- "Set Level" form (pick roster index + new level)
- Quick actions: "Add 1000 Money", "Heal All", "Force Save"

---

## 18. Formula Reference

### Effective Stat
```
Stat(level) = Max(1, Round(baseStat + growthRate * (level - 1)))
```

### XP to Next Level
```
needed = baseXpThreshold + xpPerLevel * currentLevel
(defaults: 20 + 8 * level)
Level 1 -> 28 XP needed
Level 10 -> 100 XP needed
Level 50 -> cap
```

### XP Gain from Defeat
```
gain = Max(1, Round(defeated.baseXpYield * (defeatedLevel / victorLevel)))
```

### Hit Chance
```
ratio = attacker.Accuracy / defender.Evasion
luckNudge = Clamp(atkLuck - defLuck, -20, 20) * 0.0025
chance = Clamp01(moveAccuracy * (0.75 + 0.25 * ratio) + luckNudge)
```

### Crit Chance
```
chance = Clamp01(moveCritChance + attackerLuck * 0.005)
```

### Damage
```
atkStat = Physical ? PhysAttack : ElemAttack
defStat = Physical ? PhysDefense : ElemDefense
STAB = (moveElement == monsterElement) ? 1.25 : 1.0
typeAdv = IsStrongAgainst ? 2.0 : IsWeakAgainst ? 0.5 : 1.0
crit = isCrit ? 1.5 : 1.0
rng = Random(0.9, 1.1)
damage = Max(1, Round(power * (atkStat / defStat) * STAB * typeAdv * crit * rng))
```

### Catch Chance
```
hpBonus = (1 - hp/maxHP) * 0.5
statusBonus = hasStatus ? 0.15 : 0
chance = Clamp01(0.3 + deviceBonus + hpBonus + statusBonus)
```

### Status Apply Chance
```
ratio = attackerResolve / defenderResolve
chance = Clamp01(baseChance * (0.65 + 0.35 * ratio))
```

### Status Duration
```
ratio = attackerResolve / defenderResolve
factor = Lerp(0.7, 1.0, Clamp01((ratio - 0.5) / 1.5))
turns = Max(1, Round(baseTurns * factor))
```

---

## 19. Full File Index

### New Files Created

| Path | Phase | Purpose |
|------|-------|---------|
| `ScriptableObjects/ScriptableObjectScripts/BattleConfig.cs` | 0 | Battle balance config SO |
| `ScriptableObjects/ScriptableObjectScripts/ItemDefinition.cs` | 0 | Item data template SO |
| `ScriptableObjects/ScriptableObjectScripts/MonsterCatalog.cs` | 0 | Monster lookup catalog SO |
| `ScriptableObjects/ScriptableObjectScripts/ItemCatalog.cs` | 4 | Item lookup catalog SO |
| `ScriptableObjects/ScriptableObjectScripts/ShopInventory.cs` | 5 | Per-shop item list SO |
| `Scripts/Battle/BattlePartyPickerUI.cs` | 3 | Battle switch overlay |
| `Scripts/Battle/BattleItemPickerUI.cs` | 4 | Battle item picker |
| `Scripts/UI/PartyMenuUI.cs` | 2 | Reusable party display |
| `Scripts/UI/BestiaryUI.cs` | 7 | Pokedex-style viewer |
| `Scripts/UI/EvolutionCutsceneUI.cs` | 6 | Evolution animation |
| `Scripts/UI/SaveSlotPickerUI.cs` | 11 | Save slot selection |
| `Scripts/UI/OverworldHudUI.cs` | 12 | Persistent overworld HUD |
| `Scripts/Town/HealCenterUI.cs` | 5 | Heal center service |
| `Scripts/Town/ItemShopUI.cs` | 5 | Buy/sell shop |
| `Scripts/Town/MoveTutorUI.cs` | 5 | Move teaching wizard |
| `Scripts/Town/TrainerNPC.cs` | 7 | Trainer NPC interactable |
| `Scripts/Progression/EvolutionManager.cs` | 6 | Evolution logic |
| `Scripts/Visual/GBCPostProcess.cs` | 8 | GBC palette post-process |
| `Scripts/Visual/ScanlineOverlay.cs` | 8 | Scanline post-process |
| `Scripts/Visual/TransitionWipeController.cs` | 9 | Wipe transition controller |
| `Scripts/Visual/DayNightCycle.cs` | 10 | Day/night tinting |
| `Scripts/Editor/Inspectors/ItemDefinitionEditor.cs` | 13 | Conditional item inspector |
| `Scripts/Editor/Windows/RosterDebugWindow.cs` | 13 | Debug window |
| `Shaders/GBCPalette.shader` | 8 | 4-color palette shader |
| `Shaders/ScanlineOverlay.shader` | 8 | Scanline shader |
| `Shaders/TransitionWipe.shader` | 9 | Iris/blinds/column wipe shader |

### Modified Files

| Path | What Changed |
|------|-------------|
| `MonsterDefinition.cs` | Added monsterId, growth rates, evolvedForm, baseXpYield, bestiaryDescription |
| `MonsterInstance.cs` | Added level/xp, effective stats, TryGainXP, Evolve, knownMoves |
| `ProgressionData.cs` | Added OwnedMonster, roster, partyIndices, inventory, bestiary, trainers, playTime |
| `Progression.cs` | Added roster/inventory/bestiary/trainer APIs, save slots |
| `BattleMath.cs` | All methods take MonsterInstance, added CalcXpGain, CalcCatchChance |
| `BattleController.cs` | Added Switch/Item actions, XP awards, roster persistence, catch mechanic |
| `BattleUI.cs` | Added switchButton, itemButton, updated WireRootButtons |
| `BattleSide.cs` | Added SwitchTo, AliveCount, GetAliveIndicesExcept |
| `BattleScreenBootstrapper.cs` | Builds party from Progression, sets MonsterCatalog.Instance |
| `BattleTurnHudUI.cs` | Uses growth formula for speed |
| `EnemyDefinition.cs` | Added rewardMoney, trainerId, isTrainer |
| `EncounterDirector2D.cs` | Skips defeated trainers |
| `DialogueManager.cs` | Added ShowSingleLine() |
| `GameFlowManager.cs` | Uses iris wipe transitions |
| `ScreenEffects.cs` | Added IrisWipeOut/In, BlindsWipe, ColumnDissolve |
| `GameSettings.cs` | Added GBCPalette, Scanline, DayNight keys |
| `PlayerShipController2D.cs` | Added boost multiplier and input |
| `MainMenuUI.cs` | Added save slot picker integration |
| `MonsterDefinitionEditor.cs` | Added growth bars, level preview, evolution chain |

---

## Quick Start Checklist

1. **Create ScriptableObjects:**
   - [ ] 1x BattleConfig
   - [ ] 1x MonsterCatalog (add all MonsterDefinitions)
   - [ ] 1x ItemCatalog (add all ItemDefinitions)
   - [ ] Set unique `monsterId` on every MonsterDefinition
   - [ ] Set unique `itemId` on every ItemDefinition
   - [ ] Create ItemDefinitions for: basic heal, catch device, evolution items
   - [ ] Create ShopInventory assets per shop

2. **BattleScreen scene:**
   - [ ] Wire BattleConfig to BattleController
   - [ ] Wire MonsterCatalog to BattleScreenBootstrapper
   - [ ] Add Switch and Item buttons to BattleUI root menu
   - [ ] Create and wire BattlePartyPickerUI panel
   - [ ] Create and wire BattleItemPickerUI panel
   - [ ] Add TransitionWipeController to camera (+ assign shader)

3. **Town/SpacePort scene:**
   - [ ] Add CatalogBootstrap with MonsterCatalog + ItemCatalog refs
   - [ ] Wire HealCenterUI to TownServiceHub's healPanel
   - [ ] Wire ItemShopUI to genericShopPanel (+ assign ShopInventory)
   - [ ] Wire MoveTutorUI to skillShopPanel

4. **Overworld scene:**
   - [ ] Add CatalogBootstrap
   - [ ] Add OverworldHudUI with party cards + money text
   - [ ] Add DayNightCycle with tint overlay Image
   - [ ] Add "Boost" input action to Input Actions asset

5. **Camera (all scenes or persistent):**
   - [ ] Add GBCPostProcess (+ assign GBCPalette.shader)
   - [ ] Add ScanlineOverlay (+ assign ScanlineOverlay.shader)
   - [ ] Add TransitionWipeController (+ assign TransitionWipe.shader)

6. **Main Menu scene:**
   - [ ] Add SaveSlotPickerUI panel
   - [ ] Wire to MainMenuUI.slotPicker

7. **Options Menu:**
   - [ ] Add toggles for GBC Palette, Scanlines, Day/Night
   - [ ] Add dropdown for palette preset selection
