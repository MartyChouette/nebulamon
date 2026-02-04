using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomPropertyDrawer(typeof(MoveDefinition.ElementCost))]
    public class ElementCostDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Don't indent child fields
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float totalWidth = position.width;
            float elementWidth = totalWidth * 0.6f;
            float amountWidth = totalWidth * 0.35f;
            float gap = totalWidth * 0.05f;

            var elementRect = new Rect(position.x, position.y, elementWidth, position.height);
            var amountRect = new Rect(position.x + elementWidth + gap, position.y, amountWidth, position.height);

            var elementProp = property.FindPropertyRelative("element");
            var amountProp = property.FindPropertyRelative("amount");

            EditorGUI.PropertyField(elementRect, elementProp, GUIContent.none);
            EditorGUI.PropertyField(amountRect, amountProp, new GUIContent("x"));

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
