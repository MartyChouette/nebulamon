using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomEditor(typeof(EncounterTable))]
    public class EncounterTableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var table = (EncounterTable)target;
            var entriesProp = serializedObject.FindProperty("entries");

            EditorGUILayout.PropertyField(entriesProp, new GUIContent("Entries"), true);

            if (table.entries == null || table.entries.Count == 0)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Calculate total weight
            float totalWeight = 0f;
            bool hasWarnings = false;

            foreach (var entry in table.entries)
            {
                if (entry.enemy != null && entry.weight > 0f)
                    totalWeight += entry.weight;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Encounter Distribution", EditorStyles.boldLabel);

            // Per-entry display with percentage
            for (int i = 0; i < table.entries.Count; i++)
            {
                var entry = table.entries[i];

                if (entry.enemy == null)
                {
                    var prevCol = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField($"  [{i}] WARNING: Null enemy reference", EditorStyles.miniLabel);
                    GUI.color = prevCol;
                    hasWarnings = true;
                    continue;
                }

                if (entry.weight <= 0f)
                {
                    var prevCol = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField($"  [{i}] WARNING: {entry.enemy.displayName} has zero weight", EditorStyles.miniLabel);
                    GUI.color = prevCol;
                    hasWarnings = true;
                    continue;
                }

                float pct = totalWeight > 0f ? (entry.weight / totalWeight) * 100f : 0f;
                EditorGUILayout.LabelField($"  {entry.enemy.displayName}:  {entry.weight}  ({pct:0.0}%)", EditorStyles.miniLabel);
            }

            // Visual weight bar
            if (totalWeight > 0f)
            {
                EditorGUILayout.Space(4);
                var barRect = EditorGUILayout.GetControlRect(false, 20f);

                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));

                float x = barRect.x;
                int colorIdx = 0;
                var colors = new Color[]
                {
                    new Color(0.4f, 0.6f, 0.9f),
                    new Color(0.9f, 0.5f, 0.3f),
                    new Color(0.4f, 0.8f, 0.4f),
                    new Color(0.8f, 0.4f, 0.8f),
                    new Color(0.9f, 0.8f, 0.3f),
                    new Color(0.3f, 0.8f, 0.8f),
                };

                foreach (var entry in table.entries)
                {
                    if (entry.enemy == null || entry.weight <= 0f) continue;

                    float fraction = entry.weight / totalWeight;
                    float segWidth = barRect.width * fraction;

                    var segRect = new Rect(x, barRect.y, segWidth, barRect.height);
                    EditorGUI.DrawRect(segRect, colors[colorIdx % colors.Length]);

                    // Label if wide enough
                    if (segWidth > 30f)
                    {
                        var style = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white }
                        };
                        EditorGUI.LabelField(segRect, entry.enemy.displayName, style);
                    }

                    x += segWidth;
                    colorIdx++;
                }
            }

            if (hasWarnings)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Some entries have null enemies or zero weights. These will be skipped during encounters.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
