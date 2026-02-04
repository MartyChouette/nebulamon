using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Nebula.Editor
{
    public class RosterDebugWindow : EditorWindow
    {
        [MenuItem("Nebula/Debug/Roster Debug")]
        public static void ShowWindow()
        {
            GetWindow<RosterDebugWindow>("Roster Debug");
        }

        private Vector2 _scroll;
        private MonsterId _addMonsterId = MonsterId.Solrix;
        private int _addLevel = 5;
        private int _setLevelTarget;
        private int _setLevelValue = 10;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to use roster debug tools.", MessageType.Info);
                return;
            }

            if (!Progression.IsLoaded)
            {
                EditorGUILayout.HelpBox("Progression not loaded.", MessageType.Warning);
                if (GUILayout.Button("Force Load"))
                    Progression.Load();
                return;
            }

            var data = Progression.Data;

            EditorGUILayout.LabelField($"Active Slot: {Progression.ActiveSlot}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Money: {data.money}");
            EditorGUILayout.LabelField($"Roster Count: {data.roster.Count}");
            EditorGUILayout.LabelField($"Party Indices: {string.Join(", ", data.partyIndices)}");

            EditorGUILayout.Space(8);

            // Roster list
            EditorGUILayout.LabelField("Roster", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(300));

            for (int i = 0; i < data.roster.Count; i++)
            {
                var m = data.roster[i];
                bool inParty = data.partyIndices.Contains(i);
                string partyTag = inParty ? " [PARTY]" : "";
                string nick = string.IsNullOrEmpty(m.nickname) ? "" : $" ({m.nickname})";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"[{i}] {m.monsterId}{nick} Lv.{m.level} HP:{m.currentHp} XP:{m.xp}{partyTag}",
                    EditorStyles.miniLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    data.roster.RemoveAt(i);
                    // Fix party indices
                    for (int j = data.partyIndices.Count - 1; j >= 0; j--)
                    {
                        if (data.partyIndices[j] == i)
                            data.partyIndices.RemoveAt(j);
                        else if (data.partyIndices[j] > i)
                            data.partyIndices[j]--;
                    }
                    Progression.Save();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Add monster
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add Monster", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _addMonsterId = (MonsterId)EditorGUILayout.EnumPopup("Monster", _addMonsterId);
            _addLevel = EditorGUILayout.IntField("Level", _addLevel);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add to Roster"))
            {
                var catalog = MonsterCatalog.Instance;
                var def = catalog != null ? catalog.GetByMonsterId(_addMonsterId) : null;

                int hp = 1;
                var moveNames = new List<string>();

                if (def != null)
                {
                    hp = Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (_addLevel - 1)));
                    if (def.moves != null)
                    {
                        foreach (var m in def.moves)
                        {
                            if (m != null) moveNames.Add(m.moveName);
                        }
                    }
                }

                var owned = new ProgressionData.OwnedMonster
                {
                    monsterId = _addMonsterId,
                    level = _addLevel,
                    xp = 0,
                    currentHp = hp,
                    knownMoveNames = moveNames
                };

                Progression.AddToRoster(owned);

                // Auto-add to party if under 3
                if (data.partyIndices.Count < 3)
                    data.partyIndices.Add(data.roster.Count - 1);

                Progression.Save();
            }

            // Set level
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Set Level", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _setLevelTarget = EditorGUILayout.IntField("Roster Index", _setLevelTarget);
            _setLevelValue = EditorGUILayout.IntField("New Level", _setLevelValue);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Level"))
            {
                if (_setLevelTarget >= 0 && _setLevelTarget < data.roster.Count)
                {
                    var m = data.roster[_setLevelTarget];
                    m.level = Mathf.Clamp(_setLevelValue, 1, 50);

                    var catalog = MonsterCatalog.Instance;
                    var def = catalog != null ? catalog.GetByMonsterId(m.monsterId) : null;
                    if (def != null)
                        m.currentHp = Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (m.level - 1)));

                    Progression.Save();
                }
            }

            // Quick actions
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add 1000 Money"))
            {
                Progression.AddMoney(1000);
                Progression.Save();
            }
            if (GUILayout.Button("Heal All"))
            {
                var catalog = MonsterCatalog.Instance;
                for (int i = 0; i < data.partyIndices.Count; i++)
                {
                    int idx = data.partyIndices[i];
                    if (idx < 0 || idx >= data.roster.Count) continue;
                    var m = data.roster[idx];
                    var def = catalog != null ? catalog.GetByMonsterId(m.monsterId) : null;
                    m.currentHp = def != null
                        ? Mathf.Max(1, Mathf.RoundToInt(def.maxHP + def.hpGrowth * (m.level - 1)))
                        : 1;
                }
                Progression.Save();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Force Save"))
                Progression.Save();
        }
    }
}
