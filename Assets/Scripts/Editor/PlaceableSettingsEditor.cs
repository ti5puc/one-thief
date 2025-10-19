
using UnityEditor;
using UnityEngine;
using NaughtyAttributes.Editor;

[CustomEditor(typeof(PlaceableSettings), editorForChildClasses: true)]
public class PlaceableSettingsEditor : NaughtyInspector
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PlaceableSettings trapSettings = (PlaceableSettings)target;

        // draw matrix label
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Positioning Matrix", EditorStyles.boldLabel);

        // draw matrix grid using enum popup
        var matrix = trapSettings.PositioningMatrix;
        int rows = matrix.Rows;
        int cols = matrix.Cols;

        if (rows > 0 && cols > 0)
        {
            for (int i = 0; i < rows; i++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < cols; j++)
                {
                    var newVal = (TrapPositioningType)EditorGUILayout.EnumPopup(matrix[i, j], GUILayout.MaxWidth(90));
                    if (!Equals(newVal, matrix[i, j]))
                    {
                        matrix[i, j] = newVal;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Matrix is empty.");
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Increase Matrix Size"))
        {
            matrix.Resize(rows + 2, cols + 2);
            EditorUtility.SetDirty(trapSettings);
        }

        if (GUILayout.Button("Decrease Matrix Size"))
        {
            if (rows > 3 && cols > 3)
            {
                matrix.Resize(rows - 2, cols - 2);
                EditorUtility.SetDirty(trapSettings);
            }
        }
        EditorGUILayout.EndHorizontal();

        // save changes
        if (GUI.changed)
            EditorUtility.SetDirty(trapSettings);
    }
}
