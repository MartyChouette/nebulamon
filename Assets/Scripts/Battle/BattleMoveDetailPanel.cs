// Assets/Scripts/Battle/BattleMoveDetailPanel.cs
using System.Text;
using TMPro;
using UnityEngine;

namespace Nebula
{
    public class BattleMoveDetailPanel : MonoBehaviour
    {
        [Header("Root (optional)")]
        public GameObject root; // if null, uses this.gameObject

        [Header("Texts")]
        public TMP_Text moveNameText;
        public TMP_Text metaText;
        public TMP_Text descText;

        public TMP_Text costText;
        public TMP_Text poolText;
        public TMP_Text afterText;

        private MonsterInstance _active;
        private MoveDefinition _move;

        private void Awake()
        {
            Hide();
        }

        public void Hide()
        {
            _active = null;
            _move = null;

            if (root) root.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void Show(MonsterInstance active, MoveDefinition move)
        {
            _active = active;
            _move = move;

            if (root) root.SetActive(true);
            else gameObject.SetActive(true);

            Refresh();
        }

        public void RefreshResources(MonsterInstance active)
        {
            _active = active;
            if (!IsVisible()) return;
            Refresh();
        }

        private bool IsVisible()
        {
            if (root) return root.activeInHierarchy;
            return gameObject.activeInHierarchy;
        }

        private void Refresh()
        {
            if (_active == null || _move == null) return;

            if (moveNameText) moveNameText.text = _move.moveName;

            if (metaText)
            {
                var sb = new StringBuilder();
                sb.Append(_move.element).Append(" • ").Append(_move.kind).Append(" • ").Append(_move.category);
                if (_move.power > 0) sb.Append(" • Pow ").Append(_move.power);
                if (_move.accuracy > 0f) sb.Append(" • Acc ").Append(Mathf.RoundToInt(_move.accuracy * 100f)).Append("%");
                if (_move.healAmount > 0) sb.Append(" • Heal ").Append(_move.healAmount);
                metaText.text = sb.ToString();
            }

            if (descText)
            {
                if (_move.kind == MoveKind.Status && _move.status.enabled)
                    descText.text = $"{_move.status.type} ({_move.status.durationTurns}t) @ {Mathf.RoundToInt(_move.status.applyChance * 100f)}%";
                else
                    descText.text = "";
            }

            if (poolText)
            {
                var p = _active.pool;
                poolText.text = $"Pool: Solar {p.solar}   Void {p.voids}   Bio {p.bio}   Time {p.time}";
            }

            if (costText) costText.text = $"Cost: {FormatCost(_move)}";
            if (afterText) afterText.text = $"After: {FormatAfter(_active, _move)}";
        }

        private static string FormatCost(MoveDefinition m)
        {
            if (m == null || m.costs == null || m.costs.Count == 0) return "—";

            var sb = new StringBuilder();
            for (int i = 0; i < m.costs.Count; i++)
            {
                var c = m.costs[i];
                sb.Append(c.element).Append(" ").Append(c.amount);
                if (i < m.costs.Count - 1) sb.Append(", ");
            }
            return sb.ToString();
        }

        private static string FormatAfter(MonsterInstance active, MoveDefinition m)
        {
            if (active == null) return "—";

            int solar = active.pool.solar;
            int voids = active.pool.voids;
            int bio = active.pool.bio;
            int time = active.pool.time;

            if (m?.costs != null)
            {
                for (int i = 0; i < m.costs.Count; i++)
                {
                    var c = m.costs[i];
                    switch (c.element)
                    {
                        case ElementType.Solar: solar -= c.amount; break;
                        case ElementType.Void: voids -= c.amount; break;
                        case ElementType.Bio: bio -= c.amount; break;
                        case ElementType.Time: time -= c.amount; break;
                    }
                }
            }

            return $"Solar {solar}   Void {voids}   Bio {bio}   Time {time}";
        }
    }
}
