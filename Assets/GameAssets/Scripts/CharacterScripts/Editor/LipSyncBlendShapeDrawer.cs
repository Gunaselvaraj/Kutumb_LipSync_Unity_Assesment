using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom property drawer for LipSyncBlendShape to show blend shape dropdown
/// </summary>
[CustomPropertyDrawer(typeof(LipSyncData.LipSyncBlendShape))]
public class LipSyncBlendShapeDrawer : PropertyDrawer
{
    private static List<string> cachedBlendShapes = new List<string>();
    private static bool blendShapesScanned = false;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
            property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yOffset = position.y + EditorGUIUtility.singleLineHeight + 2;
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            
            // Get properties
            SerializedProperty blendShapeNameProp = property.FindPropertyRelative("blendShapeName");
            SerializedProperty isStaticProp = property.FindPropertyRelative("isStatic");
            SerializedProperty staticValueProp = property.FindPropertyRelative("staticValue");
            SerializedProperty minValueProp = property.FindPropertyRelative("minValue");
            SerializedProperty maxValueProp = property.FindPropertyRelative("maxValue");
            SerializedProperty changeIntervalProp = property.FindPropertyRelative("changeInterval");
            SerializedProperty easeTypeProp = property.FindPropertyRelative("easeType");
            
            // Scan for blend shapes button
            Rect scanButtonRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(scanButtonRect, "🔍 Scan Scene for Blend Shapes"))
            {
                ScanForBlendShapes();
            }
            yOffset += lineHeight;
            
            // Blend Shape Name Dropdown
            Rect blendShapeRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            if (cachedBlendShapes.Count > 0)
            {
                int currentIndex = cachedBlendShapes.IndexOf(blendShapeNameProp.stringValue);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUI.Popup(blendShapeRect, "Blend Shape", currentIndex, cachedBlendShapes.ToArray());
                if (newIndex >= 0 && newIndex < cachedBlendShapes.Count)
                {
                    blendShapeNameProp.stringValue = cachedBlendShapes[newIndex];
                }
            }
            else
            {
                EditorGUI.PropertyField(blendShapeRect, blendShapeNameProp, new GUIContent("Blend Shape"));
            }
            yOffset += lineHeight;
            
            // Is Static
            Rect isStaticRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(isStaticRect, isStaticProp);
            yOffset += lineHeight;
            
            if (isStaticProp.boolValue)
            {
                // Static Value
                Rect staticValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(staticValueRect, staticValueProp, 0f, 100f, new GUIContent("Static Value"));
                yOffset += lineHeight;
            }
            else
            {
                // Animated Range Header
                Rect headerRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(headerRect, "Animated Range", EditorStyles.boldLabel);
                yOffset += lineHeight;
                
                // Min Value
                Rect minValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(minValueRect, minValueProp, 0f, 100f, new GUIContent("Min Value"));
                yOffset += lineHeight;
                
                // Max Value
                Rect maxValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(maxValueRect, maxValueProp, 0f, 100f, new GUIContent("Max Value"));
                yOffset += lineHeight;
                
                // Animation Settings Header
                Rect animHeaderRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(animHeaderRect, "Animation Settings", EditorStyles.boldLabel);
                yOffset += lineHeight;
                
                // Change Interval
                Rect intervalRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(intervalRect, changeIntervalProp, 0.1f, 2f, new GUIContent("Change Interval"));
                yOffset += lineHeight;
                
                // Ease Type
                Rect easeRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(easeRect, easeTypeProp);
                yOffset += lineHeight;
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        SerializedProperty isStaticProp = property.FindPropertyRelative("isStatic");
        float lineHeight = EditorGUIUtility.singleLineHeight + 2;
        
        float height = lineHeight; // Foldout
        height += lineHeight; // Scan button
        height += lineHeight; // Blend Shape Name
        height += lineHeight; // Is Static
        
        if (isStaticProp.boolValue)
        {
            height += lineHeight; // Static Value
        }
        else
        {
            height += lineHeight; // Header
            height += lineHeight; // Min Value
            height += lineHeight; // Max Value
            height += lineHeight; // Animation Header
            height += lineHeight; // Change Interval
            height += lineHeight; // Ease Type
        }
        
        return height;
    }
    
    private void ScanForBlendShapes()
    {
        cachedBlendShapes.Clear();
        HashSet<string> uniqueBlendShapes = new HashSet<string>();
        
        // Find all SkinnedMeshRenderer components in scene
        SkinnedMeshRenderer[] renderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();
        
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMesh != null)
            {
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string blendShapeName = renderer.sharedMesh.GetBlendShapeName(i);
                    uniqueBlendShapes.Add(blendShapeName);
                }
            }
        }
        
        // Sort and add to list
        cachedBlendShapes = uniqueBlendShapes.OrderBy(x => x).ToList();
        blendShapesScanned = true;
        
        Debug.Log($"Found {cachedBlendShapes.Count} unique blend shapes across {renderers.Length} SkinnedMeshRenderers");
    }
}
