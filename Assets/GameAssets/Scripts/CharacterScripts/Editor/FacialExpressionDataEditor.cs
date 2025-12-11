#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(FacialExpressionData))]
public class FacialExpressionDataEditor : Editor
{
    private FacialExpressionData expressionData;
    private FacialExpressionSystem facialSystem;
    private Dictionary<string, List<string>> blendShapesByMesh;
    private Vector2 scrollPos;
    private string searchFilter = "";
    
    void OnEnable()
    {
        expressionData = (FacialExpressionData)target;
        FindFacialSystemInScene();
    }
    
    private void FindFacialSystemInScene()
    {
        facialSystem = FindObjectOfType<FacialExpressionSystem>();
        
        if (facialSystem != null)
        {
            if (Application.isPlaying || facialSystem.characterRoot != null)
            {
                LoadBlendShapesFromSystem();
            }
        }
    }
    
    private void LoadBlendShapesFromSystem()
    {
        if (facialSystem == null) return;
        
        if (!Application.isPlaying && facialSystem.characterRoot != null)
        {
            blendShapesByMesh = new Dictionary<string, List<string>>();
            var renderers = facialSystem.characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;
                
                string meshName = renderer.gameObject.name;
                List<string> shapes = new List<string>();
                
                Mesh mesh = renderer.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    shapes.Add(mesh.GetBlendShapeName(i));
                }
                
                blendShapesByMesh[meshName] = shapes;
            }
        }
        else if (Application.isPlaying)
        {
            blendShapesByMesh = facialSystem.GetBlendShapesByMesh();
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Expression Info", EditorStyles.boldLabel);
        SerializedProperty nameProp = serializedObject.FindProperty("expressionName");
        SerializedProperty transProp = serializedObject.FindProperty("transitionDuration");
        
        EditorGUILayout.PropertyField(nameProp);
        EditorGUILayout.PropertyField(transProp);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Blendshapes", EditorStyles.boldLabel);
        SerializedProperty blendShapesProp = serializedObject.FindProperty("blendShapes");
        
        for (int i = 0; i < blendShapesProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            SerializedProperty element = blendShapesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameField = element.FindPropertyRelative("blendShapeName");
            SerializedProperty weightField = element.FindPropertyRelative("weight");
            
            EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(20));
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(nameField, GUIContent.none, GUILayout.Width(120));
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(weightField, GUIContent.none, GUILayout.Width(80));
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                blendShapesProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add Blendshapes", EditorStyles.boldLabel);
        
        if (facialSystem == null)
        {
            EditorGUILayout.HelpBox("Add FacialExpressionSystem to a character in the scene to see available blendshapes.", MessageType.Info);
            
            if (GUILayout.Button("Find Facial System"))
            {
                FindFacialSystemInScene();
            }
        }
        else
        {
            if (blendShapesByMesh == null || blendShapesByMesh.Count == 0)
            {
                if (facialSystem.characterRoot == null)
                {
                    EditorGUILayout.HelpBox("Assign Character Root in FacialExpressionSystem to see blendshapes.", MessageType.Warning);
                }
                else
                {
                    if (GUILayout.Button("Load Blendshapes"))
                    {
                        LoadBlendShapesFromSystem();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Click on any blendshape below to add it to this expression.", MessageType.Info);
                
                searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
                
                EditorGUILayout.Space();
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
                
                foreach (var mesh in blendShapesByMesh)
                {
                    if (mesh.Value == null || mesh.Value.Count == 0)
                        continue;
                    
                    EditorGUILayout.LabelField($"━━ {mesh.Key} ━━", EditorStyles.boldLabel);
                    
                    foreach (var shapeName in mesh.Value)
                    {
                        if (!string.IsNullOrEmpty(searchFilter) && 
                            !shapeName.ToLower().Contains(searchFilter.ToLower()))
                        {
                            continue;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  • {shapeName}");
                        
                        bool alreadyAdded = expressionData.blendShapes.Any(bs => bs.blendShapeName == shapeName);
                        
                        GUI.enabled = !alreadyAdded;
                        if (GUILayout.Button(alreadyAdded ? "Added" : "Add", GUILayout.Width(50)))
                        {
                            AddBlendShape(shapeName);
                        }
                        GUI.enabled = true;
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.Space(5);
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Refresh Blendshapes"))
                {
                    LoadBlendShapesFromSystem();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        serializedObject.ApplyModifiedProperties();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void AddBlendShape(string shapeName)
    {
        foreach (var bs in expressionData.blendShapes)
        {
            if (bs.blendShapeName == shapeName)
            {
                EditorUtility.DisplayDialog("Already Exists", 
                    $"Blendshape '{shapeName}' is already in this expression.", "OK");
                return;
            }
        }
        
        Undo.RecordObject(expressionData, "Add Blendshape");
        expressionData.blendShapes.Add(new BlendShapeEntry(shapeName, 50f));
        EditorUtility.SetDirty(expressionData);
    }
}
#endif
