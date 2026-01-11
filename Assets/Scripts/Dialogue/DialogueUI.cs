using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Nebula
{
    public class DialogueUI : MonoBehaviour
    {
        public GameObject root;              // dialogue panel
        public TMP_Text speakerLabel;         // "Dockmaster"
        public TMP_Text bodyText;             // typewriter text
        public Transform choicesRoot;         // parent for choice buttons
        public Button choiceButtonPrefab;     // prefab with a TMP_Text child

        public void SetVisible(bool v)
        {
            if (root != null) root.SetActive(v);
        }

        public void ClearChoices()
        {
            if (choicesRoot == null) return;
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Destroy(choicesRoot.GetChild(i).gameObject);
        }
    }
}
