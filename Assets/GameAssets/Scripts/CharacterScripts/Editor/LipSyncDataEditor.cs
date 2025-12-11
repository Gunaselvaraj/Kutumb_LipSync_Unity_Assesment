using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor for LipSyncData matching FacialExpressionData layout
/// </summary>
[CustomEditor(typeof(LipSyncData))]
public class LipSyncDataEditor : Editor
{
    private class BlendShapeInfo
    {
        public string name;
        public string meshName;
        public SkinnedMeshRenderer renderer;
        public string fullName => $"{name} ({meshName})";
    }
    
    private List<BlendShapeInfo> availableBlendShapes = new List<BlendShapeInfo>();
    private SerializedProperty blendShapesProp;
    private SerializedProperty descriptionProp;
    private Vector2 scrollPosition;
    private string searchText = "";
    
    private void OnEnable()
    {
        blendShapesProp = serializedObject.FindProperty("blendShapes");
        descriptionProp = serializedObject.FindProperty("description");
        ScanForBlendShapes();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Info section (like "Expression Info")
        EditorGUILayout.LabelField("LipSync Info", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Description", GUILayout.Width(150));
        descriptionProp.stringValue = EditorGUILayout.TextField(descriptionProp.stringValue);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Blendshapes section
        EditorGUILayout.LabelField("Blendshapes", EditorStyles.boldLabel);
        
        // Draw existing blend shapes
        for (int i = 0; i < blendShapesProp.arraySize; i++)
        {
            SerializedProperty element = blendShapesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("blendShapeName");
            SerializedProperty isStaticProp = element.FindPropertyRelative("isStatic");
            SerializedProperty staticValueProp = element.FindPropertyRelative("staticValue");
            SerializedProperty minValueProp = element.FindPropertyRelative("minValue");
            SerializedProperty maxValueProp = element.FindPropertyRelative("maxValue");
            SerializedProperty changeIntervalProp = element.FindPropertyRelative("changeInterval");
            SerializedProperty easeTypeProp = element.FindPropertyRelative("easeType");
            
            EditorGUILayout.BeginHorizontal();
            
            // Index
            EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(20));
            
            // Blend shape name with mesh name if available
            SerializedProperty meshNameProp = element.FindPropertyRelative("meshName");
            string displayName = string.IsNullOrEmpty(meshNameProp.stringValue) 
                ? nameProp.stringValue 
                : $"{nameProp.stringValue} ({meshNameProp.stringValue})";
            
            EditorGUILayout.LabelField(displayName, GUILayout.Width(200));
            
            if (isStaticProp.boolValue)
            {
                // Static value
                staticValueProp.floatValue = EditorGUILayout.FloatField(staticValueProp.floatValue, GUILayout.Width(60));
                EditorGUILayout.LabelField("(Static)", GUILayout.Width(60));
            }
            else
            {
                // Show range
                EditorGUILayout.LabelField($"{minValueProp.floatValue:F0}-{maxValueProp.floatValue:F0}", GUILayout.Width(60));
                EditorGUILayout.LabelField("(Anim)", GUILayout.Width(60));
            }
            
            // Settings button
            if (GUILayout.Button("⚙", GUILayout.Width(30)))
            {
                ShowBlendShapeSettings(element, i);
            }
            
            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                blendShapesProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(10);
        
        // Add Blendshapes section (matching FacialExpressionData layout)
        EditorGUILayout.LabelField("Add Blendshapes", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
        EditorGUILayout.LabelField("Click on any blendshape below to add it to this data.");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // Search box
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchText = EditorGUILayout.TextField(searchText);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Blend shapes list with scroll
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        if (availableBlendShapes.Count > 0)
        {
            // Group by category
            var grouped = availableBlendShapes
                .Where(bs => string.IsNullOrEmpty(searchText) || 
                            bs.name.ToLower().Contains(searchText.ToLower()) ||
                            bs.meshName.ToLower().Contains(searchText.ToLower()))
                .GroupBy(bs => GetMeshCategory(bs))
                .OrderBy(g => g.Key);
            
            foreach (var group in grouped)
            {
                EditorGUILayout.LabelField($"— {group.Key} —", EditorStyles.boldLabel);
                
                foreach (BlendShapeInfo info in group.OrderBy(x => x.name).ThenBy(x => x.meshName))
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Show blend shape name with mesh name in gray
                    EditorGUILayout.LabelField("  • " + info.name, GUILayout.Width(150));
                    
                    GUIStyle grayStyle = new GUIStyle(EditorStyles.label);
                    grayStyle.normal.textColor = Color.gray;
                    EditorGUILayout.LabelField($"({info.meshName})", grayStyle, GUILayout.Width(100));
                    
                    bool isAdded = IsBlendShapeAdded(info);
                    
                    GUI.enabled = !isAdded;
                    if (GUILayout.Button(isAdded ? "Added" : "Add", GUILayout.Width(70)))
                    {
                        ShowAddBlendShapeDialog(info);
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No blend shapes found. Make sure your character is in the scene.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Refresh button (like "Refresh Blendshapes")
        if (GUILayout.Button("Refresh Blendshapes", GUILayout.Height(30)))
        {
            ScanForBlendShapes();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ScanForBlendShapes()
    {
        availableBlendShapes.Clear();
        
        // Find all SkinnedMeshRenderer components in scene
        SkinnedMeshRenderer[] renderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();
        
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMesh != null)
            {
                string meshName = renderer.gameObject.name;
                
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string blendShapeName = renderer.sharedMesh.GetBlendShapeName(i);
                    
                    availableBlendShapes.Add(new BlendShapeInfo
                    {
                        name = blendShapeName,
                        meshName = meshName,
                        renderer = renderer
                    });
                }
            }
        }
        
        // Sort by name, then by mesh
        availableBlendShapes = availableBlendShapes.OrderBy(x => x.name).ThenBy(x => x.meshName).ToList();
        
        if (availableBlendShapes.Count > 0)
        {
            Debug.Log($"Found {availableBlendShapes.Count} blend shapes across {renderers.Length} meshes");
        }
    }
    
    private string GetMeshCategory(BlendShapeInfo info)
    {
        // Try to categorize based on mesh name first, then blend shape name
        string meshLower = info.meshName.ToLower();
        string nameLower = info.name.ToLower();
        
        if (meshLower.Contains("eye") || nameLower.Contains("eye")) return "Eye";
        if (meshLower.Contains("teeth") || nameLower.Contains("teeth")) return "Teeth";
        if (meshLower.Contains("tongue") || nameLower.Contains("tongue")) return "Tongue";
        if (nameLower.Contains("brow")) return "Brow";
        if (nameLower.Contains("mouth") || nameLower.Contains("jaw")) return "Mouth";
        if (nameLower.Contains("nose")) return "Nose";
        if (nameLower.Contains("cheek")) return "Cheek";
        
        // Use mesh name as category
        return info.meshName;
    }
    
    private bool IsBlendShapeAdded(BlendShapeInfo info)
    {
        for (int i = 0; i < blendShapesProp.arraySize; i++)
        {
            SerializedProperty element = blendShapesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("blendShapeName");
            SerializedProperty meshNameProp = element.FindPropertyRelative("meshName");
            
            // Check both name and mesh match
            if (nameProp.stringValue == info.name && 
                (string.IsNullOrEmpty(meshNameProp.stringValue) || meshNameProp.stringValue == info.meshName))
                return true;
        }
        return false;
    }
    
    private void ShowAddBlendShapeDialog(BlendShapeInfo info)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add as Static (Fixed Value)"), false, () => AddBlendShape(info.name, info.meshName, true));
        menu.AddItem(new GUIContent("Add as Animated (Random Range)"), false, () => AddBlendShape(info.name, info.meshName, false));
        menu.ShowAsContext();
    }
    
    private void AddBlendShape(string blendShapeName, string meshName, bool isStatic)
    {
        // Add new element
        blendShapesProp.InsertArrayElementAtIndex(blendShapesProp.arraySize);
        SerializedProperty newElement = blendShapesProp.GetArrayElementAtIndex(blendShapesProp.arraySize - 1);
        
        newElement.FindPropertyRelative("blendShapeName").stringValue = blendShapeName;
        newElement.FindPropertyRelative("meshName").stringValue = meshName;
        newElement.FindPropertyRelative("isStatic").boolValue = isStatic;
        newElement.FindPropertyRelative("staticValue").floatValue = 100f;
        newElement.FindPropertyRelative("minValue").floatValue = 50f;
        newElement.FindPropertyRelative("maxValue").floatValue = 100f;
        newElement.FindPropertyRelative("changeInterval").floatValue = 0.3f;
        newElement.FindPropertyRelative("easeType").enumValueIndex = (int)DG.Tweening.Ease.InOutSine;
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ShowBlendShapeSettings(SerializedProperty element, int index)
    {
        BlendShapeSettingsWindow.ShowWindow(element, serializedObject, index);
    }
}

/// <summary>
/// Popup window for editing blend shape settings
/// </summary>
public class BlendShapeSettingsWindow : EditorWindow
{
    private SerializedProperty element;
    private SerializedObject serializedObject;
    private int index;
    
    public static void ShowWindow(SerializedProperty element, SerializedObject serializedObject, int index)
    {
        BlendShapeSettingsWindow window = GetWindow<BlendShapeSettingsWindow>(true, "Blend Shape Settings", true);
        window.element = element;
        window.serializedObject = serializedObject;
        window.index = index;
        window.minSize = new Vector2(400, 400);
        window.maxSize = new Vector2(400, 600);
    }
    
    private void OnGUI()
    {
        if (element == null || serializedObject == null)
        {
            Close();
            return;
        }
        
        serializedObject.Update();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Blend Shape #{index + 1} Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        SerializedProperty nameProp = element.FindPropertyRelative("blendShapeName");
        SerializedProperty meshNameProp = element.FindPropertyRelative("meshName");
        SerializedProperty isStaticProp = element.FindPropertyRelative("isStatic");
        SerializedProperty staticValueProp = element.FindPropertyRelative("staticValue");
        SerializedProperty minValueProp = element.FindPropertyRelative("minValue");
        SerializedProperty maxValueProp = element.FindPropertyRelative("maxValue");
        SerializedProperty changeIntervalProp = element.FindPropertyRelative("changeInterval");
        SerializedProperty easeTypeProp = element.FindPropertyRelative("easeType");
        
        // Blend shape name (read-only) with mesh info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Blend Shape:", GUILayout.Width(120));
        string displayName = string.IsNullOrEmpty(meshNameProp.stringValue)
            ? nameProp.stringValue
            : $"{nameProp.stringValue} ({meshNameProp.stringValue})";
        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Animation Type
        EditorGUILayout.LabelField("Animation Type", EditorStyles.boldLabel);
        isStaticProp.boolValue = EditorGUILayout.Toggle("Is Static", isStaticProp.boolValue);
        
        EditorGUILayout.Space(10);
        
        if (isStaticProp.boolValue)
        {
            // Static mode
            EditorGUILayout.HelpBox("Static: Blend shape stays at a fixed value", MessageType.Info);
            staticValueProp.floatValue = EditorGUILayout.Slider("Value", staticValueProp.floatValue, 0f, 100f);
        }
        else
        {
            // Animated mode
            EditorGUILayout.HelpBox("Animated: Blend shape randomly oscillates between min and max values", MessageType.Info);
            
            EditorGUILayout.LabelField("Value Range", EditorStyles.boldLabel);
            minValueProp.floatValue = EditorGUILayout.Slider("Min Value", minValueProp.floatValue, 0f, 100f);
            maxValueProp.floatValue = EditorGUILayout.Slider("Max Value", maxValueProp.floatValue, 0f, 100f);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            changeIntervalProp.floatValue = EditorGUILayout.Slider("Change Interval (seconds)", changeIntervalProp.floatValue, 0.1f, 2f);
            EditorGUILayout.HelpBox($"Changes every {changeIntervalProp.floatValue:F2} seconds", MessageType.None);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Easing Curve", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(easeTypeProp, new GUIContent("Ease Type"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Recommended for natural talking:\n• InOutSine - Smooth natural motion\n• InOutQuad - Gentle ease\n• Linear - Constant speed", MessageType.None);
        }
        
        EditorGUILayout.Space(20);
        
        if (GUILayout.Button("Close", GUILayout.Height(30)))
        {
            serializedObject.ApplyModifiedProperties();
            Close();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
