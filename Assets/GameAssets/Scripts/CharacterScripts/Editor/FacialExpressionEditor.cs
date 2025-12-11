#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(FacialExpressionSystem))]
public class FacialExpressionEditor : Editor
{
    private FacialExpressionSystem facialSystem;
    private Vector2 scrollPos;
    private bool showBlendShapesList = false;
    
    void OnEnable()
    {
        facialSystem = (FacialExpressionSystem)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Expression Editor", EditorStyles.boldLabel);
        
        if (facialSystem.characterRoot != null)
        {
            showBlendShapesList = EditorGUILayout.Foldout(showBlendShapesList, "Available Blend Shapes");
            if (showBlendShapesList)
            {
                EditorGUILayout.BeginVertical("box");
                
                if (GUILayout.Button("Refresh Blendshapes"))
                {
                    if (Application.isPlaying)
                    {
                        facialSystem.RefreshBlendShapes();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Info", "Enter Play Mode to see blendshapes, or create Expression assets and use the FacialExpressionData editor.", "OK");
                    }
                }
                
                if (Application.isPlaying)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                    
                    var blendShapesByMesh = facialSystem.GetBlendShapesByMesh();
                    foreach (var mesh in blendShapesByMesh)
                    {
                        EditorGUILayout.LabelField($"━━ {mesh.Key} ━━", EditorStyles.boldLabel);
                        foreach (var name in mesh.Value)
                        {
                            EditorGUILayout.LabelField("  • " + name);
                        }
                        EditorGUILayout.Space(5);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to see all available blendshapes grouped by mesh.", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign Character Root to see available blendshapes.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Test Expressions", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var expression in facialSystem.expressions)
            {
                if (GUILayout.Button(expression.expressionName, GUILayout.Height(25)))
                {
                    facialSystem.SetExpression(expression.expressionName);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset to Neutral", GUILayout.Height(25)))
            {
                facialSystem.ResetAllBlendShapes();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
