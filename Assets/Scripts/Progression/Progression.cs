using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;



namespace Nebula
{
    public static class Progression
    {
        private const string FileName = "save_progression.json";

        public static ProgressionData Data { get; private set; }
        public static bool IsLoaded => Data != null;

        public static event Action OnChanged;

        private static bool _dirty;

        // Save slots
        public static int ActiveSlot { get; private set; }

        private static string SavePath => SavePathForSlot(ActiveSlot);
        private static string TempPath => SavePath + ".tmp";

        private static string SavePathForSlot(int slot)
        {
            if (slot <= 0) return Path.Combine(Application.persistentDataPath, FileName);
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
        }

        public static void Load()
        {
            if (IsLoaded) return;

            // Recover from interrupted save: if temp exists but main doesn't, rename it
            if (File.Exists(TempPath) && !File.Exists(SavePath))
            {
                try { File.Move(TempPath, SavePath); }
                catch (Exception e) { Debug.LogWarning($"Progression: temp recovery failed: {e.Message}"); }
            }
            // Clean up leftover temp file
            else if (File.Exists(TempPath))
            {
                try { File.Delete(TempPath); }
                catch { /* ignored */ }
            }

            if (!File.Exists(SavePath))
            {
                Data = NewDefault();
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<ProgressionData>(json);
                if (Data == null) Data = NewDefault();

                // sanitize
                if (Data.flags == null) Data.flags = new List<ProgressionData.StringFlag>();
                if (Data.counters == null) Data.counters = new List<ProgressionData.StringCounter>();
                if (Data.planetsDiscovered == null) Data.planetsDiscovered = new System.Collections.Generic.List<PlanetId>();
                if (Data.planetsLanded == null) Data.planetsLanded = new System.Collections.Generic.List<PlanetId>();
                if (Data.knowledgeLearned == null) Data.knowledgeLearned = new System.Collections.Generic.List<KnowledgeId>();
                if (Data.upgradesOwned == null) Data.upgradesOwned = new System.Collections.Generic.List<UpgradeId>();
                if (Data.villainMonsterAssignments == null) Data.villainMonsterAssignments = new System.Collections.Generic.List<VillainMonsterPair>();
                if (Data.romances == null) Data.romances = new System.Collections.Generic.List<RomanceEntry>();
                if (Data.roster == null) Data.roster = new List<ProgressionData.OwnedMonster>();
                if (Data.partyIndices == null) Data.partyIndices = new List<int>();
                if (Data.inventory == null) Data.inventory = new List<ProgressionData.InventorySlot>();
                if (Data.monstersSeen == null) Data.monstersSeen = new List<MonsterId>();
                if (Data.monstersCaught == null) Data.monstersCaught = new List<MonsterId>();
                if (Data.trainersDefeated == null) Data.trainersDefeated = new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Progression.Load failed, making new save. {e.Message}");
                Data = NewDefault();
                Save();
            }
        }

        public static void Save()
        {
            if (!IsLoaded) Load();

            try
            {
                string json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Progression.Save failed: {e}");
            }

            _dirty = false;
        }

        public static void SaveIfDirty()
        {
            if (_dirty) Save();
        }

        public static void HardResetSave()
        {
            Data = NewDefault();
            Save();
            OnChanged?.Invoke();
        }

        private static ProgressionData NewDefault()
        {
            var d = new ProgressionData
            {
                money = 0,
                starterMonster = MonsterId.None,
                currentPlanetString = "",
                activeRomance = RomanceCandidateId.None
            };
            return d;
        }

        private static void Changed()
        {
            _dirty = true;
            OnChanged?.Invoke();
        }

        private static ProgressionData Ensure()
        {
            if (!IsLoaded) Load();
            return Data;
        }

        // -------------------------
        // Money
        // -------------------------
        public static int Money => Ensure().money;

        public static bool SpendMoney(int amount)
        {
            if (amount <= 0) return true;
            var d = Ensure();
            if (d.money < amount) return false;
            d.money -= amount;
            Changed();
            return true;
        }

        public static void AddMoney(int amount)
        {
            var d = Ensure();
            d.money = Mathf.Max(0, d.money + amount);
            Changed();
        }

        // -------------------------
        // Starter monster
        // -------------------------
        public static bool HasChosenStarter => Ensure().starterMonster != MonsterId.None;

        public static void ChooseStarter(MonsterId starter)
        {
            var d = Ensure();
            if (d.starterMonster != MonsterId.None) return; // lock once chosen
            d.starterMonster = starter;
            Changed();
        }

        public static MonsterId GetStarter() => Ensure().starterMonster;

        // -------------------------
        // Planets
        // -------------------------
        public static void DiscoverPlanet(PlanetId planet)
        {
            var d = Ensure();
            if (!d.planetsDiscovered.Contains(planet))
            {
                d.planetsDiscovered.Add(planet);
                Changed();
            }
        }

        public static bool HasDiscoveredPlanet(PlanetId planet) => Ensure().planetsDiscovered.Contains(planet);

        public static void LandOnPlanet(PlanetId planet)
        {
            var d = Ensure();
            DiscoverPlanet(planet);

            if (!d.planetsLanded.Contains(planet))
            {
                d.planetsLanded.Add(planet);
                Changed();
            }

            SetCurrentPlanet(planet);
        }

        public static bool HasLandedOnPlanet(PlanetId planet) => Ensure().planetsLanded.Contains(planet);

        public static void SetCurrentPlanet(PlanetId planet)
        {
            var d = Ensure();
            d.currentPlanetString = planet.ToString();
            Changed();
        }

        public static bool TryGetCurrentPlanet(out PlanetId planet)
        {
            var d = Ensure();
            if (string.IsNullOrWhiteSpace(d.currentPlanetString))
            {
                planet = default;
                return false;
            }
            return Enum.TryParse(d.currentPlanetString, out planet);
        }

        // -------------------------
        // Knowledge
        // -------------------------
        public static void Learn(KnowledgeId knowledge)
        {
            var d = Ensure();
            if (!d.knowledgeLearned.Contains(knowledge))
            {
                d.knowledgeLearned.Add(knowledge);
                Changed();
            }
        }

        public static bool Knows(KnowledgeId knowledge) => Ensure().knowledgeLearned.Contains(knowledge);

        // -------------------------
        // Upgrades
        // -------------------------
        public static void GrantUpgrade(UpgradeId upgrade)
        {
            var d = Ensure();
            if (!d.upgradesOwned.Contains(upgrade))
            {
                d.upgradesOwned.Add(upgrade);
                Changed();
            }
        }

        public static bool HasUpgrade(UpgradeId upgrade) => Ensure().upgradesOwned.Contains(upgrade);

        // -------------------------
        // Villain monster assignments
        // -------------------------
        public static void SetVillainMonster(VillainId villain, MonsterId monster)
        {
            var d = Ensure();

            d.villainMonsterAssignments.RemoveAll(x => x.villain == villain);
            d.villainMonsterAssignments.RemoveAll(x => x.monster == monster);

            d.villainMonsterAssignments.Add(new VillainMonsterPair { villain = villain, monster = monster });
            Changed();
        }

        public static bool TryGetVillainMonster(VillainId villain, out MonsterId monster)
        {
            var d = Ensure();
            for (int i = 0; i < d.villainMonsterAssignments.Count; i++)
            {
                if (d.villainMonsterAssignments[i].villain == villain)
                {
                    monster = d.villainMonsterAssignments[i].monster;
                    return true;
                }
            }

            monster = MonsterId.None;
            return false;
        }

        public static void GenerateVillainAssignmentsIfMissing(int seed = 0)
        {
            var d = Ensure();
            if (d.villainMonsterAssignments != null && d.villainMonsterAssignments.Count >= 8)
                return;

            d.villainMonsterAssignments.Clear();

            var villains = (VillainId[])Enum.GetValues(typeof(VillainId));
            var monsters = new[]
            {
                MonsterId.Solrix, MonsterId.Voidmaw, MonsterId.Biovine, MonsterId.Chronofly,
                MonsterId.Flaredon, MonsterId.Abyssal, MonsterId.Sporethorn, MonsterId.Tempora
            };

            var rng = (seed == 0) ? new System.Random() : new System.Random(seed);

            for (int i = monsters.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (monsters[i], monsters[j]) = (monsters[j], monsters[i]);
            }

            int count = Mathf.Min(villains.Length, monsters.Length);
            for (int i = 0; i < count; i++)
            {
                d.villainMonsterAssignments.Add(new VillainMonsterPair
                {
                    villain = villains[i],
                    monster = monsters[i]
                });
            }

            Changed();
        }

        // -------------------------
        // ROMANCE
        // -------------------------
        private static int FindRomanceIndex(RomanceCandidateId candidate)
        {
            var d = Ensure();
            for (int i = 0; i < d.romances.Count; i++)
                if (d.romances[i].candidate == candidate) return i;
            return -1;
        }

        private static RomanceEntry GetOrCreateRomance(RomanceCandidateId candidate)
        {
            var d = Ensure();
            int idx = FindRomanceIndex(candidate);
            if (idx >= 0) return d.romances[idx];

            var entry = new RomanceEntry
            {
                candidate = candidate,
                state = RomanceState.NotMet,
                affinity = 0,
                giftGiven = false,
                seenConfession = false
            };
            d.romances.Add(entry);
            return entry;
        }
        // Returns every romance candidate ID that belongs to a given planet.
        public static List<RomanceCandidateId> GetRomancesForPlanet(PlanetId planet)
        {
            var all = (RomanceCandidateId[])System.Enum.GetValues(typeof(RomanceCandidateId));

            var list = new List<RomanceCandidateId>();
            for (int i = 0; i < all.Length; i++)
            {
                var id = all[i];
                if (id == RomanceCandidateId.None) continue;

                if (GetRomanceHomePlanet(id) == planet)
                    list.Add(id);
            }

            return list;
        }

        // Returns romances for the current planet (if any).
        public static List<RomanceCandidateId> GetRomancesForCurrentPlanet()
        {
            if (!TryGetCurrentPlanet(out var planet))
                return new List<RomanceCandidateId>();

            return GetRomancesForPlanet(planet);
        }

        // Optional: filter to ones you can still pursue (not rejected/completed).
        public static List<RomanceCandidateId> GetAvailableRomancesForPlanet(PlanetId planet)
        {
            var ids = GetRomancesForPlanet(planet);
            var outList = new List<RomanceCandidateId>();

            for (int i = 0; i < ids.Count; i++)
            {
                var st = GetRomanceState(ids[i]);
                if (st == RomanceState.Rejected || st == RomanceState.Completed)
                    continue;

                outList.Add(ids[i]);
            }

            return outList;
        }

        // Optional: return a "primary romance" for planets with one, or the first option for planets with two.
        public static bool TryGetPrimaryRomanceForPlanet(PlanetId planet, out RomanceCandidateId candidate)
        {
            var list = GetRomancesForPlanet(planet);
            if (list.Count > 0)
            {
                candidate = list[0];
                return true;
            }

            candidate = RomanceCandidateId.None;
            return false;
        }

        private static void SetRomanceInternal(RomanceEntry entry)
        {
            var d = Ensure();
            int idx = FindRomanceIndex(entry.candidate);
            if (idx >= 0) d.romances[idx] = entry;
            else d.romances.Add(entry);
            Changed();
        }

        public static PlanetId GetRomanceHomePlanet(RomanceCandidateId candidate)
            => RomanceCatalog.GetHomePlanet(candidate);

        public static RomanceState GetRomanceState(RomanceCandidateId candidate)
        {
            var d = Ensure();
            int idx = FindRomanceIndex(candidate);
            return (idx >= 0) ? d.romances[idx].state : RomanceState.NotMet;
        }

        public static int GetRomanceAffinity(RomanceCandidateId candidate)
        {
            var d = Ensure();
            int idx = FindRomanceIndex(candidate);
            return (idx >= 0) ? d.romances[idx].affinity : 0;
        }

        public static void MarkRomanceMet(RomanceCandidateId candidate)
        {
            var entry = GetOrCreateRomance(candidate);
            if (entry.state == RomanceState.NotMet)
            {
                entry.state = RomanceState.Met;
                SetRomanceInternal(entry);
            }
        }

        public static void SetRomanceState(RomanceCandidateId candidate, RomanceState state)
        {
            var entry = GetOrCreateRomance(candidate);
            entry.state = state;
            SetRomanceInternal(entry);
        }

        public static void AddRomanceAffinity(RomanceCandidateId candidate, int delta, int min = 0, int max = 100)
        {
            var entry = GetOrCreateRomance(candidate);
            entry.affinity = Mathf.Clamp(entry.affinity + delta, min, max);
            SetRomanceInternal(entry);
        }

        public static void SetRomanceGiftGiven(RomanceCandidateId candidate, bool value)
        {
            var entry = GetOrCreateRomance(candidate);
            entry.giftGiven = value;
            SetRomanceInternal(entry);
        }

        public static void SetRomanceSeenConfession(RomanceCandidateId candidate, bool value)
        {
            var entry = GetOrCreateRomance(candidate);
            entry.seenConfession = value;
            SetRomanceInternal(entry);
        }

        /// <summary>
        /// Optional: set the romance currently "active" (for UI, quests, etc.)
        /// </summary>
        public static void SetActiveRomance(RomanceCandidateId candidate)
        {
            var d = Ensure();
            d.activeRomance = candidate;
            Changed();
        }

        public static RomanceCandidateId GetActiveRomance() => Ensure().activeRomance;

        // -------------------------
        // Roster
        // -------------------------
        public static void AddToRoster(ProgressionData.OwnedMonster monster)
        {
            var d = Ensure();
            d.roster.Add(monster);
            Changed();
        }

        public static List<ProgressionData.OwnedMonster> GetParty()
        {
            var d = Ensure();
            var party = new List<ProgressionData.OwnedMonster>();
            for (int i = 0; i < d.partyIndices.Count; i++)
            {
                int idx = d.partyIndices[i];
                if (idx >= 0 && idx < d.roster.Count)
                    party.Add(d.roster[idx]);
            }
            return party;
        }

        public static void SetPartyOrder(List<int> indices)
        {
            var d = Ensure();
            d.partyIndices = indices ?? new List<int>();
            Changed();
        }

        public static ProgressionData.OwnedMonster GetRosterEntry(int index)
        {
            var d = Ensure();
            if (index < 0 || index >= d.roster.Count) return null;
            return d.roster[index];
        }

        public static int RosterCount => Ensure().roster.Count;

        // -------------------------
        // Inventory
        // -------------------------
        public static void AddItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            var d = Ensure();

            for (int i = 0; i < d.inventory.Count; i++)
            {
                if (d.inventory[i].itemId == itemId)
                {
                    var slot = d.inventory[i];
                    slot.quantity += count;
                    d.inventory[i] = slot;
                    Changed();
                    return;
                }
            }

            d.inventory.Add(new ProgressionData.InventorySlot { itemId = itemId, quantity = count });
            Changed();
        }

        public static bool RemoveItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
            var d = Ensure();

            for (int i = 0; i < d.inventory.Count; i++)
            {
                if (d.inventory[i].itemId == itemId)
                {
                    if (d.inventory[i].quantity < count) return false;
                    var slot = d.inventory[i];
                    slot.quantity -= count;
                    if (slot.quantity <= 0)
                        d.inventory.RemoveAt(i);
                    else
                        d.inventory[i] = slot;
                    Changed();
                    return true;
                }
            }
            return false;
        }

        public static int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            var d = Ensure();

            for (int i = 0; i < d.inventory.Count; i++)
            {
                if (d.inventory[i].itemId == itemId)
                    return d.inventory[i].quantity;
            }
            return 0;
        }

        public static List<ProgressionData.InventorySlot> GetInventory() => Ensure().inventory;

        // -------------------------
        // Bestiary
        // -------------------------
        public static void MarkSeen(MonsterId id)
        {
            if (id == MonsterId.None) return;
            var d = Ensure();
            if (!d.monstersSeen.Contains(id))
            {
                d.monstersSeen.Add(id);
                Changed();
            }
        }

        public static void MarkCaught(MonsterId id)
        {
            if (id == MonsterId.None) return;
            var d = Ensure();
            MarkSeen(id);
            if (!d.monstersCaught.Contains(id))
            {
                d.monstersCaught.Add(id);
                Changed();
            }
        }

        public static bool HasSeen(MonsterId id) => Ensure().monstersSeen.Contains(id);
        public static bool HasCaught(MonsterId id) => Ensure().monstersCaught.Contains(id);
        public static int SeenCount() => Ensure().monstersSeen.Count;
        public static int CaughtCount() => Ensure().monstersCaught.Count;

        // -------------------------
        // Trainers
        // -------------------------
        public static void MarkTrainerDefeated(string trainerId)
        {
            if (string.IsNullOrEmpty(trainerId)) return;
            var d = Ensure();
            if (!d.trainersDefeated.Contains(trainerId))
            {
                d.trainersDefeated.Add(trainerId);
                Changed();
            }
        }

        public static bool IsTrainerDefeated(string trainerId)
        {
            if (string.IsNullOrEmpty(trainerId)) return false;
            return Ensure().trainersDefeated.Contains(trainerId);
        }

        // -------------------------
        // Save Slots
        // -------------------------
        public static void LoadSlot(int slot)
        {
            Data = null; // force re-load
            ActiveSlot = slot;
            Load();
        }

        public static void DeleteSlot(int slot)
        {
            string path = SavePathForSlot(slot);
            string tmp = path + ".tmp";
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Progression.DeleteSlot({slot}) failed: {e.Message}");
            }

            if (slot == ActiveSlot)
            {
                Data = null;
            }
        }

        public static bool SlotExists(int slot)
        {
            return File.Exists(SavePathForSlot(slot));
        }

        /// <summary>
        /// Returns a lightweight preview of a slot without loading it as active.
        /// Returns null if the slot doesn't exist.
        /// </summary>
        public static SlotPreview GetSlotPreview(int slot)
        {
            string path = SavePathForSlot(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                var d = JsonUtility.FromJson<ProgressionData>(json);
                if (d == null) return null;

                return new SlotPreview
                {
                    slot = slot,
                    money = d.money,
                    starterMonster = d.starterMonster,
                    rosterCount = d.roster?.Count ?? 0,
                    playTimeSeconds = d.playTimeSeconds
                };
            }
            catch
            {
                return null;
            }
        }

        public class SlotPreview
        {
            public int slot;
            public int money;
            public MonsterId starterMonster;
            public int rosterCount;
            public float playTimeSeconds;
        }

        // -------------------------
        // Generic flags/counters
        // -------------------------
        public static bool GetFlag(string key, bool defaultValue = false)
        {
            var d = Ensure();
            for (int i = 0; i < d.flags.Count; i++)
                if (d.flags[i].key == key) return d.flags[i].value;
            return defaultValue;
        }

        public static void SetFlag(string key, bool value)
        {
            var d = Ensure();
            for (int i = 0; i < d.flags.Count; i++)
            {
                if (d.flags[i].key == key)
                {
                    d.flags[i] = new ProgressionData.StringFlag { key = key, value = value };
                    Changed();
                    return;
                }
            }

            d.flags.Add(new ProgressionData.StringFlag { key = key, value = value });
            Changed();
        }

        public static int GetCounter(string key, int defaultValue = 0)
        {
            var d = Ensure();
            for (int i = 0; i < d.counters.Count; i++)
                if (d.counters[i].key == key) return d.counters[i].value;
            return defaultValue;
        }

        public static void SetCounter(string key, int value)
        {
            var d = Ensure();
            for (int i = 0; i < d.counters.Count; i++)
            {
                if (d.counters[i].key == key)
                {
                    d.counters[i] = new ProgressionData.StringCounter { key = key, value = value };
                    Changed();
                    return;
                }
            }

            d.counters.Add(new ProgressionData.StringCounter { key = key, value = value });
            Changed();
        }

        public static void IncrementCounter(string key, int delta = 1)
        {
            int v = GetCounter(key, 0);
            SetCounter(key, v + delta);
        }
    }
}
