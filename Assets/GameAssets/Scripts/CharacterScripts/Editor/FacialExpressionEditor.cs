#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(FacialExpressionSystem))]
public class FacialExpressionEditor : Editor
{
    private FacialExpressionSystem facialSystem;
    private string newExpressionName = "New Expression";
    private Vector2 scrollPos;
    private bool showBlendShapesList = false;
    private bool showCreateAsset = false;
    
    void OnEnable()
    {
        facialSystem = (FacialExpressionSystem)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Expression Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Expressions are now ScriptableObjects. Create them as assets for easy reuse across characters.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Create new expression ScriptableObject
        showCreateAsset = EditorGUILayout.Foldout(showCreateAsset, "Create Expression Asset");
        if (showCreateAsset)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Create ScriptableObject Expression", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Right-click in Project window → Create → Facial System → Facial Expression", MessageType.Info);
            
            if (GUILayout.Button("Create Expression Asset", GUILayout.Height(30)))
            {
                CreateExpressionAsset();
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Show available blend shapes
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
        
        // Test expressions
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
            EditorGUILayout.HelpBox("Enter Play Mode to test expressions", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void CreateExpressionAsset()
    {
        FacialExpressionData newExpression = ScriptableObject.CreateInstance<FacialExpressionData>();
        newExpression.expressionName = "New Expression";
        
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Facial Expression",
            "NewExpression",
            "asset",
            "Create a new facial expression asset"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newExpression, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newExpression;
            
            Debug.Log($"Created expression asset at: {path}");
        }
    }
}
#endif
