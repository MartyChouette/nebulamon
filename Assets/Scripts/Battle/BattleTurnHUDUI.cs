// Assets/Scripts/Battle/BattleTurnHudUI.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula
{
    public class BattleTurnHudUI : MonoBehaviour
    {
        [Serializable]
        public class TurnOrderSlot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text label;
            public TMP_Text eta; // optional: "in 2 ticks" style
        }

        [Header("ATB Bars")]
        public Slider playerAtb;
        public Slider enemyAtb;

        [Header("ATB Labels (optional)")]
        public TMP_Text playerAtbLabel;
        public TMP_Text enemyAtbLabel;

        [Header("Turn Order Projection")]
        [Min(1)] public int projectedTurns = 5;
        public TurnOrderSlot[] slots;

        [Header("Debug")]
        public bool showEtaTicks = true;

        private float _threshold = 100f;
        private float _leftoverClamp01 = 0.25f;

        public void Configure(float threshold, float leftoverClamp01)
        {
            _threshold = Mathf.Max(1f, threshold);
            _leftoverClamp01 = Mathf.Clamp01(leftoverClamp01);

            if (playerAtb) { playerAtb.minValue = 0f; playerAtb.maxValue = 1f; }
            if (enemyAtb) { enemyAtb.minValue = 0f; enemyAtb.maxValue = 1f; }
        }

        public void Refresh(MonsterInstance player, MonsterInstance enemy)
        {
            RefreshBars(player, enemy);
            RefreshTimeline(player, enemy, projectedTurns);
        }

        public void RefreshBars(MonsterInstance player, MonsterInstance enemy)
        {
            float p01 = (player == null) ? 0f : Mathf.Clamp01(player.initiative / _threshold);
            float e01 = (enemy == null) ? 0f : Mathf.Clamp01(enemy.initiative / _threshold);

            if (playerAtb) playerAtb.value = p01;
            if (enemyAtb) enemyAtb.value = e01;

            if (playerAtbLabel) playerAtbLabel.text = BuildAtbLabel(player);
            if (enemyAtbLabel) enemyAtbLabel.text = BuildAtbLabel(enemy);
        }

        private string BuildAtbLabel(MonsterInstance m)
        {
            if (m == null || m.def == null) return "";

            int eff = Mathf.Max(1, m.EffectiveSpeed());
            int baseSpd = GetBaseSpeedSafe(m);

            string tempo =
                eff > baseSpd ? "Haste" :
                eff < baseSpd ? "Slow" :
                "Normal";

            // Example: "SPD 14 (Haste)"
            return $"SPD {eff} ({tempo})";
        }

        private int GetBaseSpeedSafe(MonsterInstance m)
        {
            if (m?.def == null) return Mathf.Max(1, m?.EffectiveSpeed() ?? 1);
            return Mathf.Max(1, Mathf.RoundToInt(m.def.speed + m.def.speedGrowth * (m.level - 1)));
        }

        // ---------------- Turn Order Projection ----------------

        private void RefreshTimeline(MonsterInstance player, MonsterInstance enemy, int count)
        {
            if (slots == null || slots.Length == 0) return;

            var projected = ProjectNextTurns(player, enemy, Mathf.Min(count, slots.Length));

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null || s.root == null) continue;

                if (i >= projected.Count)
                {
                    s.root.SetActive(false);
                    continue;
                }

                s.root.SetActive(true);

                var tok = projected[i];

                if (s.icon) s.icon.sprite = tok.icon;
                if (s.label) s.label.text = tok.label;

                if (s.eta)
                {
                    if (showEtaTicks && tok.ticksUntilAct >= 0)
                        s.eta.text = $"{tok.ticksUntilAct}t";
                    else
                        s.eta.text = "";
                }
            }
        }

        private struct TurnToken
        {
            public bool isPlayer;
            public Sprite icon;
            public string label;
            public int ticksUntilAct;
        }

        private List<TurnToken> ProjectNextTurns(MonsterInstance player, MonsterInstance enemy, int count)
        {
            var list = new List<TurnToken>(count);

            if (player == null || enemy == null || player.def == null || enemy.def == null)
                return list;

            float th = _threshold;
            float clamp = th * _leftoverClamp01;

            float pIni = player.initiative;
            float eIni = enemy.initiative;

            int pSpd = Mathf.Max(1, player.EffectiveSpeed());
            int eSpd = Mathf.Max(1, enemy.EffectiveSpeed());

            // Icons: use battleSprite if present; otherwise null is okay.
            Sprite pIcon = GetBattleSpriteSafe(player);
            Sprite eIcon = GetBattleSpriteSafe(enemy);

            // This sim's "ticks" are the same loop-iterations your AdvanceMeters uses.
            // It's not seconds; it's "CTB ticks" (useful for preview).
            int accumulatedTicks = 0;

            for (int i = 0; i < count; i++)
            {
                // If someone already >= threshold, ticks = 0
                int pNeed = (pIni >= th) ? 0 : CeilDiv((int)Mathf.Ceil(th - pIni), pSpd);
                int eNeed = (eIni >= th) ? 0 : CeilDiv((int)Mathf.Ceil(th - eIni), eSpd);

                int stepTicks = Mathf.Min(pNeed, eNeed);
                if (stepTicks < 0) stepTicks = 0;

                // Advance both by the same number of ticks, like your while loop does.
                if (stepTicks > 0)
                {
                    pIni += pSpd * stepTicks;
                    eIni += eSpd * stepTicks;
                    accumulatedTicks += stepTicks;
                }

                // Decide who acts next using same tie-break:
                bool playerActs = DecideActsNext(pIni, eIni, pSpd, eSpd);

                if (playerActs)
                {
                    list.Add(new TurnToken
                    {
                        isPlayer = true,
                        icon = pIcon,
                        label = player.def.displayName,
                        ticksUntilAct = accumulatedTicks
                    });

                    pIni -= th;
                    pIni = Mathf.Clamp(pIni, 0f, clamp);
                }
                else
                {
                    list.Add(new TurnToken
                    {
                        isPlayer = false,
                        icon = eIcon,
                        label = enemy.def.displayName,
                        ticksUntilAct = accumulatedTicks
                    });

                    eIni -= th;
                    eIni = Mathf.Clamp(eIni, 0f, clamp);
                }

                // After someone acts, next prediction continues with same speeds.
                // (If you later want future statuses to change speed, we can simulate that too.)
            }

            return list;
        }

        private static int CeilDiv(int a, int b)
        {
            if (b <= 0) return 0;
            if (a <= 0) return 0;
            return (a + b - 1) / b;
        }

        private static bool DecideActsNext(float pIni, float eIni, int pSpd, int eSpd)
        {
            if (pIni > eIni) return true;
            if (eIni > pIni) return false;

            if (pSpd != eSpd) return pSpd > eSpd;
            return true;
        }

        private static Sprite GetBattleSpriteSafe(MonsterInstance m)
        {
            if (m?.def == null) return null;
            return m.def.battleSprite;
        }
    }
}
