using UnityEngine;

// This attribute lets you categorize the script in the 'Add Component' menu
[AddComponentMenu("Miscellaneous/Editor Note")]
public class EditorNote : MonoBehaviour
{
    // The TextArea attribute provides a resizable text area in the Inspector
    [TextArea(3, 10)]
    public string Note = "Add your editor notes here.";
}
