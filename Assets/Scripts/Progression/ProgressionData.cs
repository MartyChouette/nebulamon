using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    public enum PlanetId
    {
        Planet1,
        Planet2,
        Planet3,
        Planet4,
        Planet5,
        Planet6,
        Planet7,
        Planet8
    }

    public enum KnowledgeId
    {
        LearnedAsteroidFieldsExist,
        LearnedDeepSpaceCurrents,
        LearnedPirateTerritoryRoutes,
        LearnedAncientSignal,
        LearnedMonsterFusion,
        LearnedVillainMotivation
    }

    public enum UpgradeId
    {
        AsteroidSensor,
        DeepSpaceEngine,
        BetterShields,
        LongRangeScanner,
        CargoHold,
        HyperComms
    }

    public enum MonsterId
    {
        None = 0,
        Solrix,      // Solar — lizard-like creature wreathed in starfire
        Voidmaw,     // Void — shadowy maw that devours light
        Biovine,     // Bio — living vine colony from jungle moons
        Chronofly,   // Time — insectoid that flickers through moments
        Flaredon,    // Solar — armored beast with corona plating
        Abyssal,     // Void — deep-space predator, bioluminescent lure
        Sporethorn,  // Bio — spore-launching thorned symbiote
        Tempora      // Time — crystalline stag that warps local time
    }

    public enum VillainId
    {
        Villain1, Villain2, Villain3, Villain4, Villain5, Villain6, Villain7, Villain8
    }

    // ROMANCE --------------------------------------------
    // Give each romance candidate a stable ID.
    // Two planets can have two candidates: just add two IDs pointing to that planet in RomanceCatalog below.
    public enum RomanceCandidateId
    {
        None = 0,

        // Example naming scheme: PlanetX_A / PlanetX_B
        P1_A,
        P2_A,
        P3_A,
        P4_A,
        P5_A,
        P6_A,
        P7_A,
        P8_A,

        // Extra romances for the two �double-romance� planets:
        P3_B,   // Example: Planet3 has two romance options
        P7_B    // Example: Planet7 has two romance options
    }

    public enum RomanceState
    {
        NotMet,
        Met,
        Dating,
        Committed,
        Rejected,
        Completed
    }

    [Serializable]
    public struct RomanceEntry
    {
        public RomanceCandidateId candidate;
        public RomanceState state;
        public int affinity;          // optional: 0..100
        public bool giftGiven;        // example extra flag
        public bool seenConfession;   // example extra flag
    }

    /// <summary>
    /// �Catalog� describing which planet a romance belongs to.
    /// You can keep this in code for now; later we can move it to a ScriptableObject.
    /// </summary>
    public static class RomanceCatalog
    {
        public static PlanetId GetHomePlanet(RomanceCandidateId id)
        {
            switch (id)
            {
                case RomanceCandidateId.P1_A: return PlanetId.Planet1;
                case RomanceCandidateId.P2_A: return PlanetId.Planet2;

                // Planet3 has two
                case RomanceCandidateId.P3_A: return PlanetId.Planet3;
                case RomanceCandidateId.P3_B: return PlanetId.Planet3;

                case RomanceCandidateId.P4_A: return PlanetId.Planet4;
                case RomanceCandidateId.P5_A: return PlanetId.Planet5;
                case RomanceCandidateId.P6_A: return PlanetId.Planet6;

                // Planet7 has two
                case RomanceCandidateId.P7_A: return PlanetId.Planet7;
                case RomanceCandidateId.P7_B: return PlanetId.Planet7;

                case RomanceCandidateId.P8_A: return PlanetId.Planet8;

                default: return PlanetId.Planet1;
            }
        }
    }
    // ----------------------------------------------------

    [Serializable]
    public struct VillainMonsterPair
    {
        public VillainId villain;
        public MonsterId monster;
    }

    [Serializable]
    public class ProgressionData
    {
        public int saveVersion = 1;

        public int money = 0;

        public MonsterId starterMonster = MonsterId.None;

        public List<PlanetId> planetsDiscovered = new List<PlanetId>();
        public List<PlanetId> planetsLanded = new List<PlanetId>();
        public string currentPlanetString = "";

        public List<KnowledgeId> knowledgeLearned = new List<KnowledgeId>();
        public List<UpgradeId> upgradesOwned = new List<UpgradeId>();

        public List<VillainMonsterPair> villainMonsterAssignments = new List<VillainMonsterPair>();

        // ROMANCE DATA
        public List<RomanceEntry> romances = new List<RomanceEntry>();
        public RomanceCandidateId activeRomance = RomanceCandidateId.None; // optional �current� romance

        // ROSTER & PARTY
        public List<OwnedMonster> roster = new List<OwnedMonster>();
        public List<int> partyIndices = new List<int>();

        // INVENTORY
        public List<InventorySlot> inventory = new List<InventorySlot>();

        // BESTIARY
        public List<MonsterId> monstersSeen = new List<MonsterId>();
        public List<MonsterId> monstersCaught = new List<MonsterId>();

        // TRAINERS
        public List<string> trainersDefeated = new List<string>();

        // PLAY TIME
        public float playTimeSeconds = 0f;

        // Generic extensibility
        public List<StringFlag> flags = new List<StringFlag>();
        public List<StringCounter> counters = new List<StringCounter>();

        [Serializable] public struct StringFlag { public string key; public bool value; }
        [Serializable] public struct StringCounter { public string key; public int value; }

        [Serializable]
        public class OwnedMonster
        {
            public MonsterId monsterId;
            public int level = 1;
            public int xp;
            public int currentHp;
            public List<string> knownMoveNames = new List<string>();
            public string nickname;
        }

        [Serializable]
        public struct InventorySlot
        {
            public string itemId;
            public int quantity;
        }
    }
}
