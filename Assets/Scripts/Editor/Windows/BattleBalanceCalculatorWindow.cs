using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    public class BattleBalanceCalculatorWindow : EditorWindow
    {
        private MonsterDefinition _attacker;
        private MoveDefinition _move;
        private MonsterDefinition _defender;
        private Vector2 _scrollPos;

        [MenuItem("Nebula/Tools/Battle Balance Calculator")]
        public static void ShowWindow()
        {
            var window = GetWindow<BattleBalanceCalculatorWindow>("Balance Calculator");
            window.minSize = new Vector2(420, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Battle Balance Calculator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _attacker = (MonsterDefinition)EditorGUILayout.ObjectField("Attacker", _attacker, typeof(MonsterDefinition), false);
            _move = (MoveDefinition)EditorGUILayout.ObjectField("Move", _move, typeof(MoveDefinition), false);
            _defender = (MonsterDefinition)EditorGUILayout.ObjectField("Defender", _defender, typeof(MonsterDefinition), false);

            if (_attacker == null || _move == null || _defender == null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("Assign an attacker, move, and defender to see calculations.", MessageType.Info);
                return;
            }

            // Read config values (or defaults)
            float strongMult = 2.0f, weakMult = 0.5f, stabVal = 1.25f, critMult = 1.5f;
            float rngMin = 0.9f, rngMax = 1.1f;
            float accBase = 0.75f, accScaling = 0.25f;
            float nudgePerPt = 0.0025f;
            int luckClamp = 20;
            float luckToCrit = 0.005f;
            float statusBaseW = 0.65f, statusScaleW = 0.35f;
            float durFloor = 0.7f, durCeil = 1.0f, durOff = 0.5f, durScale = 1.5f;

            var cfg = BattleConfig.Instance;
            if (cfg != null)
            {
                strongMult = cfg.strongMultiplier;
                weakMult = cfg.weakMultiplier;
                stabVal = cfg.stabBonus;
                critMult = cfg.critMultiplier;
                rngMin = cfg.damageRngMin;
                rngMax = cfg.damageRngMax;
                accBase = cfg.accBase;
                accScaling = cfg.accScaling;
                nudgePerPt = cfg.luckNudgePerPoint;
                luckClamp = cfg.luckClampRange;
                luckToCrit = cfg.luckToCritRate;
                statusBaseW = cfg.statusBaseWeight;
                statusScaleW = cfg.statusScaling;
                durFloor = cfg.durationFloor;
                durCeil = cfg.durationCeil;
                durOff = cfg.durationOffset;
                durScale = cfg.durationScale;
            }

            EditorGUILayout.Space(8);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ── Type Advantage ──
            EditorGUILayout.LabelField("Type Matchup", EditorStyles.boldLabel);
            float adv = 1.0f;
            string advLabel;
            Color advColor;

            if (BattleMath.IsStrongAgainst(_move.element, _defender.element))
            {
                adv = strongMult;
                advLabel = $"Super Effective! ({_move.element} > {_defender.element}) x{adv}";
                advColor = new Color(0.3f, 1f, 0.3f);
            }
            else if (BattleMath.IsStrongAgainst(_defender.element, _move.element))
            {
                adv = weakMult;
                advLabel = $"Not Very Effective ({_move.element} < {_defender.element}) x{adv}";
                advColor = new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                advLabel = $"Neutral ({_move.element} vs {_defender.element}) x1.0";
                advColor = Color.white;
            }

            var prev = GUI.color;
            GUI.color = advColor;
            EditorGUILayout.LabelField($"  {advLabel}");
            GUI.color = prev;

            bool hasStab = _move.element == _attacker.element;
            EditorGUILayout.LabelField($"  STAB: {(hasStab ? $"Yes (x{stabVal})" : "No")}");

            // ── Speed Comparison ──
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Speed Comparison", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Attacker Speed: {_attacker.speed}   Defender Speed: {_defender.speed}");
            if (_attacker.speed > _defender.speed)
                EditorGUILayout.LabelField("  Attacker acts first.");
            else if (_defender.speed > _attacker.speed)
                EditorGUILayout.LabelField("  Defender acts first.");
            else
                EditorGUILayout.LabelField("  Tied speed (attacker wins ties if player).");

            // ── Hit Chance ──
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Accuracy", EditorStyles.boldLabel);

            if (_move.kind == MoveKind.Damage)
            {
                float aAcc = Mathf.Max(1f, _attacker.accuracy);
                float dEva = Mathf.Max(1f, _defender.evasion);
                float luckNudge = Mathf.Clamp(_attacker.luck - _defender.luck, -luckClamp, luckClamp) * nudgePerPt;
                float ratio = aAcc / dEva;
                float hitChance = Mathf.Clamp01(_move.accuracy * (accBase + accScaling * ratio) + luckNudge);

                EditorGUILayout.LabelField($"  Hit Chance: {hitChance * 100f:0.0}%");
            }
            else
            {
                EditorGUILayout.LabelField("  (Non-damage moves auto-hit)");
            }

            // ── Crit Chance ──
            float critBonus = _attacker.luck * luckToCrit;
            float totalCrit = Mathf.Clamp01(_move.critChance + critBonus);
            EditorGUILayout.LabelField($"  Crit Chance: {totalCrit * 100f:0.0}% (base {_move.critChance * 100f:0}% + luck {critBonus * 100f:0.0}%)");

            // ── Damage Range ──
            if (_move.kind == MoveKind.Damage)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Damage", EditorStyles.boldLabel);

                int basePower = Mathf.Max(1, _move.power);
                float atkStat = _move.category == MoveCategory.Physical
                    ? Mathf.Max(1f, _attacker.physAttack)
                    : Mathf.Max(1f, _attacker.elemAttack);
                float defStat = _move.category == MoveCategory.Physical
                    ? Mathf.Max(1f, _defender.physDefense)
                    : Mathf.Max(1f, _defender.elemDefense);

                float stab = hasStab ? stabVal : 1f;
                float baseRaw = basePower * (atkStat / defStat) * stab * adv;

                int minDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * rngMin));
                int maxDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * rngMax));
                int avgDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * (rngMin + rngMax) * 0.5f));

                EditorGUILayout.LabelField($"  Normal:  {minDmg} - {maxDmg}  (avg {avgDmg})");

                int critMinDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * rngMin * critMult));
                int critMaxDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * rngMax * critMult));
                int critAvgDmg = Mathf.Max(1, Mathf.RoundToInt(baseRaw * (rngMin + rngMax) * 0.5f * critMult));

                EditorGUILayout.LabelField($"  Crit:    {critMinDmg} - {critMaxDmg}  (avg {critAvgDmg})");

                // Estimated turns to KO
                EditorGUILayout.Space(4);
                if (avgDmg > 0)
                {
                    float turnsToKO = (float)_defender.maxHP / avgDmg;
                    EditorGUILayout.LabelField($"  Est. turns to KO: {turnsToKO:0.1} (at avg damage vs {_defender.maxHP} HP)");
                }
            }

            // ── Heal ──
            if (_move.kind == MoveKind.Heal)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Heal", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Restores {_move.healAmount} HP ({(_move.healAmount * 100f / _attacker.maxHP):0}% of attacker's max HP)");
            }

            // ── Status ──
            if (_move.status.enabled)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Status Effect", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Type: {_move.status.type}");

                var target = _move.status.applyToSelf ? _attacker : _defender;
                string targetName = _move.status.applyToSelf ? "Self" : "Defender";

                float aRes = Mathf.Max(1f, _attacker.resolve);
                float dRes = Mathf.Max(1f, target.resolve);
                float resRatio = aRes / dRes;

                float baseChance = _move.status.applyChance;
                float finalChance = Mathf.Clamp01(baseChance * (statusBaseW + statusScaleW * resRatio));
                EditorGUILayout.LabelField($"  Apply Chance: {finalChance * 100f:0.0}% (base {baseChance * 100f:0}%, target: {targetName})");

                int baseDur = _move.status.durationTurns;
                float durFactor = Mathf.Lerp(durFloor, durCeil, Mathf.Clamp01((resRatio - durOff) / durScale));
                int finalDur = Mathf.Max(1, Mathf.RoundToInt(baseDur * durFactor));
                EditorGUILayout.LabelField($"  Duration: {finalDur} turns (base {baseDur})");

                if (_move.status.potency > 0f)
                {
                    string potDesc = _move.status.type switch
                    {
                        StatusType.Slow => $"{_move.status.potency * 100f:0}% speed reduction",
                        StatusType.Dazed => $"{_move.status.potency * 100f:0}% skip chance",
                        StatusType.Confused => $"{_move.status.potency * 100f:0}% self-hit chance",
                        _ => $"{_move.status.potency:0.00}"
                    };
                    EditorGUILayout.LabelField($"  Potency: {potDesc}");
                }
            }

            if (cfg == null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("No BattleConfig.Instance found. Using hardcoded defaults. Assign a BattleConfig to BattleController and enter Play mode to use config values.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
