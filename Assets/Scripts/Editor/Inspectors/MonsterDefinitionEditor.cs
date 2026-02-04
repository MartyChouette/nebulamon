using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomEditor(typeof(MonsterDefinition))]
    public class MonsterDefinitionEditor : UnityEditor.Editor
    {
        private static readonly Color HpColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color SpeedColor = new Color(0.3f, 0.9f, 0.9f);
        private static readonly Color PhysAtkColor = new Color(0.9f, 0.6f, 0.2f);
        private static readonly Color PhysDefColor = new Color(0.8f, 0.5f, 0.2f);
        private static readonly Color ElemAtkColor = new Color(0.5f, 0.3f, 0.9f);
        private static readonly Color ElemDefColor = new Color(0.4f, 0.25f, 0.7f);
        private static readonly Color AccColor = new Color(0.3f, 0.7f, 0.3f);
        private static readonly Color EvaColor = new Color(0.2f, 0.6f, 0.4f);
        private static readonly Color ResColor = new Color(0.7f, 0.7f, 0.3f);
        private static readonly Color LuckColor = new Color(0.9f, 0.85f, 0.2f);

        private const int SoftCap = 150;
        private const int HighCap = 200;
        private const float MaxBarStat = 50f;

        private int _previewLevel = 1;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var monster = (MonsterDefinition)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stat Overview", EditorStyles.boldLabel);

            // Stat total
            int total = monster.maxHP + monster.speed +
                        monster.physAttack + monster.physDefense +
                        monster.elemAttack + monster.elemDefense +
                        monster.accuracy + monster.evasion +
                        monster.resolve + monster.luck;

            Color totalColor;
            if (total <= SoftCap) totalColor = Color.green;
            else if (total <= HighCap) totalColor = Color.yellow;
            else totalColor = Color.red;

            var prevColor = GUI.color;
            GUI.color = totalColor;
            EditorGUILayout.LabelField($"Stat Total: {total}  (soft cap: {SoftCap})");
            GUI.color = prevColor;

            EditorGUILayout.Space(4);

            // Stat bars (base)
            DrawStatBar("HP", monster.maxHP, HpColor);
            DrawStatBar("Speed", monster.speed, SpeedColor);
            DrawStatBar("P.Atk", monster.physAttack, PhysAtkColor);
            DrawStatBar("P.Def", monster.physDefense, PhysDefColor);
            DrawStatBar("E.Atk", monster.elemAttack, ElemAtkColor);
            DrawStatBar("E.Def", monster.elemDefense, ElemDefColor);
            DrawStatBar("Acc", monster.accuracy, AccColor);
            DrawStatBar("Eva", monster.evasion, EvaColor);
            DrawStatBar("Resolve", monster.resolve, ResColor);
            DrawStatBar("Luck", monster.luck, LuckColor);

            // Growth rate bars
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Growth Rates", EditorStyles.boldLabel);

            DrawGrowthBar("HP/Lv", monster.hpGrowth, HpColor);
            DrawGrowthBar("Spd/Lv", monster.speedGrowth, SpeedColor);
            DrawGrowthBar("PAtk/Lv", monster.physAttackGrowth, PhysAtkColor);
            DrawGrowthBar("PDef/Lv", monster.physDefenseGrowth, PhysDefColor);
            DrawGrowthBar("EAtk/Lv", monster.elemAttackGrowth, ElemAtkColor);
            DrawGrowthBar("EDef/Lv", monster.elemDefenseGrowth, ElemDefColor);
            DrawGrowthBar("Acc/Lv", monster.accuracyGrowth, AccColor);
            DrawGrowthBar("Eva/Lv", monster.evasionGrowth, EvaColor);
            DrawGrowthBar("Res/Lv", monster.resolveGrowth, ResColor);
            DrawGrowthBar("Luck/Lv", monster.luckGrowth, LuckColor);

            // Effective at level X preview
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Effective Stats at Level", EditorStyles.boldLabel);
            _previewLevel = EditorGUILayout.IntSlider("Preview Level", _previewLevel, 1, 50);

            DrawStatBar("HP", CalcEff(monster.maxHP, monster.hpGrowth), HpColor);
            DrawStatBar("Speed", CalcEff(monster.speed, monster.speedGrowth), SpeedColor);
            DrawStatBar("P.Atk", CalcEff(monster.physAttack, monster.physAttackGrowth), PhysAtkColor);
            DrawStatBar("P.Def", CalcEff(monster.physDefense, monster.physDefenseGrowth), PhysDefColor);
            DrawStatBar("E.Atk", CalcEff(monster.elemAttack, monster.elemAttackGrowth), ElemAtkColor);
            DrawStatBar("E.Def", CalcEff(monster.elemDefense, monster.elemDefenseGrowth), ElemDefColor);
            DrawStatBar("Acc", CalcEff(monster.accuracy, monster.accuracyGrowth), AccColor);
            DrawStatBar("Eva", CalcEff(monster.evasion, monster.evasionGrowth), EvaColor);
            DrawStatBar("Resolve", CalcEff(monster.resolve, monster.resolveGrowth), ResColor);
            DrawStatBar("Luck", Mathf.Max(0, Mathf.RoundToInt(monster.luck + monster.luckGrowth * (_previewLevel - 1))), LuckColor);

            // Evolution chain
            if (monster.evolvedForm != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Evolution Chain", EditorStyles.boldLabel);
                var chain = monster;
                string chainStr = monster.displayName;
                int safety = 10;
                while (chain.evolvedForm != null && safety-- > 0)
                {
                    chainStr += $" -> {chain.evolvedForm.displayName}";
                    chain = chain.evolvedForm;
                }
                EditorGUILayout.LabelField($"  {chainStr}", EditorStyles.miniLabel);
            }

            // Move summary
            if (monster.moves != null && monster.moves.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Move Summary", EditorStyles.boldLabel);

                foreach (var move in monster.moves)
                {
                    if (move == null)
                    {
                        EditorGUILayout.LabelField("  (null move)", EditorStyles.miniLabel);
                        continue;
                    }

                    string costStr = "";
                    if (move.costs != null && move.costs.Count > 0)
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        foreach (var c in move.costs)
                            parts.Add($"{c.element} x{c.amount}");
                        costStr = string.Join(", ", parts);
                    }
                    else
                    {
                        costStr = "Free";
                    }

                    string line;
                    if (move.kind == MoveKind.Heal)
                    {
                        line = $"[{move.moveName}] {move.category} Heal +{move.healAmount} | Cost: {costStr}";
                    }
                    else if (move.kind == MoveKind.Status)
                    {
                        string statusInfo = move.status.enabled ? move.status.type.ToString() : "None";
                        line = $"[{move.moveName}] {move.category} Status ({statusInfo}) | Cost: {costStr}";
                    }
                    else
                    {
                        line = $"[{move.moveName}] {move.category} {move.power}pwr | Cost: {costStr} | {move.accuracy * 100f:0}% acc";
                    }

                    EditorGUILayout.LabelField($"  {line}", EditorStyles.miniLabel);
                }
            }
        }

        private int CalcEff(int baseStat, float growth)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseStat + growth * (_previewLevel - 1)));
        }

        private void DrawGrowthBar(string label, float value, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float labelWidth = 60f;
            float valueWidth = 40f;
            float barX = rect.x + labelWidth + valueWidth + 4f;
            float barWidth = rect.width - labelWidth - valueWidth - 4f;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            EditorGUI.LabelField(new Rect(rect.x + labelWidth, rect.y, valueWidth, rect.height), value.ToString("0.0"));

            if (barWidth > 0f)
            {
                float fill = Mathf.Clamp01(value / 5f); // 5 = very high growth
                var barRect = new Rect(barX, rect.y + 2f, barWidth, rect.height - 4f);

                Color dimColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f);
                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height), dimColor);
            }
        }

        private void DrawStatBar(string label, int value, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float labelWidth = 60f;
            float valueWidth = 30f;
            float barX = rect.x + labelWidth + valueWidth + 4f;
            float barWidth = rect.width - labelWidth - valueWidth - 4f;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            EditorGUI.LabelField(new Rect(rect.x + labelWidth, rect.y, valueWidth, rect.height), value.ToString());

            if (barWidth > 0f)
            {
                float fill = Mathf.Clamp01(value / MaxBarStat);
                var barRect = new Rect(barX, rect.y + 2f, barWidth, rect.height - 4f);

                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height), color);
            }
        }
    }
}
