using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomPropertyDrawer(typeof(MoveDefinition.StatusPayload))]
    public class StatusPayloadDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var enabledProp = property.FindPropertyRelative("enabled");
            var typeProp = property.FindPropertyRelative("type");
            var applyChanceProp = property.FindPropertyRelative("applyChance");
            var durationProp = property.FindPropertyRelative("durationTurns");
            var applyToSelfProp = property.FindPropertyRelative("applyToSelf");
            var potencyProp = property.FindPropertyRelative("potency");

            float y = position.y;
            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Header with foldout-style toggle
            var enabledRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(enabledRect, enabledProp, new GUIContent("Status Payload"));
            y += lineH + spacing;

            if (!enabledProp.boolValue)
            {
                EditorGUI.EndProperty();
                return;
            }

            // Indent child fields
            EditorGUI.indentLevel++;

            var typeRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(typeRect, typeProp);
            y += lineH + spacing;

            var chanceRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(chanceRect, applyChanceProp, new GUIContent("Apply Chance"));
            y += lineH + spacing;

            var durRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(durRect, durationProp, new GUIContent("Duration (turns)"));
            y += lineH + spacing;

            var selfRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(selfRect, applyToSelfProp, new GUIContent("Apply to Self"));
            y += lineH + spacing;

            var potRect = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(potRect, potencyProp);
            y += lineH + spacing;

            // Summary label
            string statusName = ((StatusType)typeProp.enumValueIndex).ToString();
            string target = applyToSelfProp.boolValue ? "self" : "target";
            float chance = applyChanceProp.floatValue * 100f;
            int dur = durationProp.intValue;
            string summary = $"{chance:0}% chance to apply {statusName} for {dur} turn{(dur != 1 ? "s" : "")} to {target}";

            // Potency explanation for specific statuses
            float pot = potencyProp.floatValue;
            var statusType = (StatusType)typeProp.enumValueIndex;
            if (pot > 0f)
            {
                switch (statusType)
                {
                    case StatusType.Slow:
                        summary += $" ({pot * 100f:0}% speed reduction)";
                        break;
                    case StatusType.Dazed:
                        summary += $" ({pot * 100f:0}% skip chance)";
                        break;
                    case StatusType.Confused:
                        summary += $" ({pot * 100f:0}% self-hit chance)";
                        break;
                }
            }

            var summaryRect = new Rect(position.x, y, position.width, lineH);
            var prevColor = GUI.color;
            GUI.color = new Color(0.6f, 0.9f, 0.6f);
            EditorGUI.LabelField(summaryRect, summary, EditorStyles.miniLabel);
            GUI.color = prevColor;

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            var enabledProp = property.FindPropertyRelative("enabled");
            if (enabledProp == null || !enabledProp.boolValue)
                return lineH; // Just the toggle

            // toggle + type + chance + duration + self + potency + summary = 7 lines
            return (lineH + spacing) * 7;
        }
    }
}
