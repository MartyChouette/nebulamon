// Assets/Scripts/Battle/BattleMoveButtonHighlight.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nebula
{
    public class BattleMoveButtonHighlight : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private BattleMoveDetailPanel _panel;
        private MonsterInstance _active;
        private MoveDefinition _move;

        public void Bind(BattleMoveDetailPanel panel, MonsterInstance active, MoveDefinition move)
        {
            _panel = panel;
            _active = active;
            _move = move;
        }

        public void OnPointerEnter(PointerEventData eventData) => Show();
        public void OnSelect(BaseEventData eventData) => Show();

        private void Show()
        {
            if (_panel == null || _active == null || _move == null) return;
            _panel.Show(_active, _move);
        }
    }
}
