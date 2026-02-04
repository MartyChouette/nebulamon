using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomPropertyDrawer(typeof(EncounterTable.Entry))]
    public class EncounterEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float totalWidth = position.width;
            float enemyWidth = totalWidth * 0.65f;
            float weightWidth = totalWidth * 0.30f;
            float gap = totalWidth * 0.05f;

            var enemyRect = new Rect(position.x, position.y, enemyWidth, position.height);
            var weightRect = new Rect(position.x + enemyWidth + gap, position.y, weightWidth, position.height);

            var enemyProp = property.FindPropertyRelative("enemy");
            var weightProp = property.FindPropertyRelative("weight");

            EditorGUI.PropertyField(enemyRect, enemyProp, GUIContent.none);
            EditorGUI.PropertyField(weightRect, weightProp, new GUIContent("W"));

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
