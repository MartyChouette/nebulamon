using UnityEngine;
using TMPro;

namespace Nebula
{
    public class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.0f, 0);

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
            Hide();
        }

        public void Show(string text, Transform target)
        {
            if (root != null) root.SetActive(true);
            if (label != null) label.text = text;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null && target != null)
            {
                Vector3 screen = _cam.WorldToScreenPoint(target.position + worldOffset);
                transform.position = screen;
            }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
