using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomEditor(typeof(MoveDefinition))]
    public class MoveDefinitionEditor : UnityEditor.Editor
    {
        private bool _damageFoldout;
        private int _previewAtkStat = 10;
        private int _previewDefStat = 10;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var move = (MoveDefinition)target;

            // Plain-English description at top
            EditorGUILayout.Space(4);
            DrawDescription(move);
            EditorGUILayout.Space(8);

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveName"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Classification", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Element", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("element"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Element Costs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("costs"), true);
            DrawCostTotal(move);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Reliability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("accuracy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("critChance"));

            // Conditional: show power only for Damage moves
            if (move.kind == MoveKind.Damage)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Damage", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
            }

            // Conditional: show healAmount only for Heal moves
            if (move.kind == MoveKind.Heal)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Heal", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("healAmount"));
            }

            // Status payload
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);

            // Damage preview foldout (only for damage moves)
            if (move.kind == MoveKind.Damage)
            {
                EditorGUILayout.Space(8);
                _damageFoldout = EditorGUILayout.Foldout(_damageFoldout, "Damage Preview", true);
                if (_damageFoldout)
                {
                    EditorGUI.indentLevel++;
                    _previewAtkStat = EditorGUILayout.IntField("Attacker Stat", _previewAtkStat);
                    _previewDefStat = EditorGUILayout.IntField("Defender Stat", _previewDefStat);

                    _previewAtkStat = Mathf.Max(1, _previewAtkStat);
                    _previewDefStat = Mathf.Max(1, _previewDefStat);

                    float ratio = (float)_previewAtkStat / _previewDefStat;
                    int basePower = Mathf.Max(1, move.power);

                    // Use config values if available, else defaults
                    float rngMin = 0.9f, rngMax = 1.1f, stabVal = 1.25f, critMult = 1.5f;
                    var cfg = BattleConfig.Instance;
                    if (cfg != null)
                    {
                        rngMin = cfg.damageRngMin;
                        rngMax = cfg.damageRngMax;
                        stabVal = cfg.stabBonus;
                        critMult = cfg.critMultiplier;
                    }

                    // No STAB, no advantage, no crit
                    float rawMin = basePower * ratio * rngMin;
                    float rawMax = basePower * ratio * rngMax;
                    float rawAvg = basePower * ratio * ((rngMin + rngMax) * 0.5f);

                    EditorGUILayout.LabelField($"Normal:  min={Mathf.Max(1, Mathf.RoundToInt(rawMin))}  avg={Mathf.Max(1, Mathf.RoundToInt(rawAvg))}  max={Mathf.Max(1, Mathf.RoundToInt(rawMax))}");

                    // With crit
                    float critMin = rawMin * critMult;
                    float critMax = rawMax * critMult;
                    float critAvg = rawAvg * critMult;
                    EditorGUILayout.LabelField($"Crit:    min={Mathf.Max(1, Mathf.RoundToInt(critMin))}  avg={Mathf.Max(1, Mathf.RoundToInt(critAvg))}  max={Mathf.Max(1, Mathf.RoundToInt(critMax))}");

                    // With STAB
                    float stabMin = rawMin * stabVal;
                    float stabMax = rawMax * stabVal;
                    float stabAvg = rawAvg * stabVal;
                    EditorGUILayout.LabelField($"STAB:    min={Mathf.Max(1, Mathf.RoundToInt(stabMin))}  avg={Mathf.Max(1, Mathf.RoundToInt(stabAvg))}  max={Mathf.Max(1, Mathf.RoundToInt(stabMax))}");

                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDescription(MoveDefinition move)
        {
            string kindStr = move.kind.ToString();
            string catStr = move.category.ToString();
            string elemStr = move.element.ToString();

            string desc;
            if (move.kind == MoveKind.Heal)
            {
                desc = $"{move.moveName} is a {catStr} Heal move ({elemStr}). Heals {move.healAmount} HP.";
            }
            else if (move.kind == MoveKind.Status)
            {
                string statusInfo = move.status.enabled ? move.status.type.ToString() : "None";
                desc = $"{move.moveName} is a {catStr} Status move ({elemStr}). Applies {statusInfo}.";
            }
            else
            {
                desc = $"{move.moveName} is a {catStr} Damage move ({elemStr}). Power: {move.power}, Acc: {move.accuracy * 100f:0}%, Crit: {move.critChance * 100f:0}%.";
            }

            // Cost summary
            if (move.costs != null && move.costs.Count > 0)
            {
                var parts = new System.Collections.Generic.List<string>();
                foreach (var c in move.costs)
                    parts.Add($"{c.element} x{c.amount}");
                desc += $" Costs {string.Join(", ", parts)}.";
            }
            else
            {
                desc += " Free to use.";
            }

            var style = new GUIStyle(EditorStyles.helpBox) { richText = true, fontSize = 11 };
            EditorGUILayout.LabelField(desc, style);
        }

        private void DrawCostTotal(MoveDefinition move)
        {
            if (move.costs == null || move.costs.Count == 0) return;

            int total = 0;
            var parts = new System.Collections.Generic.List<string>();
            foreach (var c in move.costs)
            {
                total += c.amount;
                parts.Add($"{c.element} x{c.amount}");
            }

            EditorGUILayout.LabelField($"Total: {string.Join(", ", parts)} ({total} elements)", EditorStyles.miniLabel);
        }
    }
}
