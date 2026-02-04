using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomEditor(typeof(ItemDefinition))]
    public class ItemDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var item = (ItemDefinition)target;

            // Always show identity + classification + economy
            DrawProperty("itemId");
            DrawProperty("displayName");
            DrawProperty("description");
            DrawProperty("icon");
            DrawProperty("category");
            DrawProperty("buyPrice");
            DrawProperty("sellPrice");
            DrawProperty("consumable");

            EditorGUILayout.Space(6);

            // Category-specific fields
            switch (item.category)
            {
                case ItemCategory.Heal:
                    EditorGUILayout.LabelField("Heal Properties", EditorStyles.boldLabel);
                    DrawProperty("healAmount");
                    DrawProperty("healStatus");
                    DrawProperty("healAllParty");
                    break;

                case ItemCategory.CatchDevice:
                    EditorGUILayout.LabelField("Catch Properties", EditorStyles.boldLabel);
                    DrawProperty("catchRateBonus");
                    break;

                case ItemCategory.Evolution:
                    EditorGUILayout.LabelField("Evolution Properties", EditorStyles.boldLabel);
                    DrawProperty("targetMonster");
                    DrawProperty("evolvedInto");
                    break;

                case ItemCategory.MoveTutor:
                    EditorGUILayout.LabelField("Move Tutor Properties", EditorStyles.boldLabel);
                    DrawProperty("taughtMove");
                    DrawProperty("compatibleMonsters");
                    break;

                case ItemCategory.KeyItem:
                case ItemCategory.Misc:
                    EditorGUILayout.LabelField("(No category-specific fields)", EditorStyles.miniLabel);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string name)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }
    }
}
