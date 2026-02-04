using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    [CustomEditor(typeof(EnemyDefinition))]
    public class EnemyDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var enemy = (EnemyDefinition)target;

            if (enemy.party == null || enemy.party.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Party Preview", EditorStyles.boldLabel);

            int totalHP = 0;
            int totalSpeed = 0;
            int totalStats = 0;
            int validCount = 0;

            // Horizontal row of monster thumbnails
            EditorGUILayout.BeginHorizontal();
            foreach (var monster in enemy.party)
            {
                if (monster == null)
                {
                    EditorGUILayout.LabelField("(null)", GUILayout.Width(70));
                    continue;
                }

                validCount++;
                totalHP += monster.maxHP;
                totalSpeed += monster.speed;
                totalStats += monster.maxHP + monster.speed + monster.physAttack + monster.physDefense +
                              monster.elemAttack + monster.elemDefense + monster.accuracy + monster.evasion +
                              monster.resolve + monster.luck;

                EditorGUILayout.BeginVertical("box", GUILayout.Width(80));

                // Thumbnail
                if (monster.battleSprite != null)
                {
                    var tex = AssetPreview.GetAssetPreview(monster.battleSprite);
                    if (tex != null)
                    {
                        GUILayout.Label(tex, GUILayout.Width(64), GUILayout.Height(64));
                    }
                    else
                    {
                        GUILayout.Box(GUIContent.none, GUILayout.Width(64), GUILayout.Height(64));
                    }
                }
                else
                {
                    GUILayout.Box("No Sprite", GUILayout.Width(64), GUILayout.Height(64));
                }

                EditorGUILayout.LabelField(monster.displayName, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(monster.element.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"HP: {monster.maxHP}", EditorStyles.miniLabel);

                int statTotal = monster.maxHP + monster.speed + monster.physAttack + monster.physDefense +
                                monster.elemAttack + monster.elemDefense + monster.accuracy + monster.evasion +
                                monster.resolve + monster.luck;
                EditorGUILayout.LabelField($"BST: {statTotal}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            // Difficulty summary
            if (validCount > 0)
            {
                EditorGUILayout.Space(4);
                int avgSpeed = Mathf.RoundToInt((float)totalSpeed / validCount);
                EditorGUILayout.LabelField(
                    $"Party Size: {validCount}  |  Combined HP: {totalHP}  |  Avg Speed: {avgSpeed}  |  Total BST: {totalStats}",
                    EditorStyles.miniLabel);
            }
        }
    }
}
