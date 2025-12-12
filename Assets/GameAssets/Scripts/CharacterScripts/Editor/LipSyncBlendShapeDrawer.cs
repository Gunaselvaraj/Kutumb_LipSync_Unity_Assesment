using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
[CustomPropertyDrawer(typeof(LipSyncData.LipSyncBlendShape))]
public class LipSyncBlendShapeDrawer : PropertyDrawer
{
    private static List<string> cachedBlendShapes = new List<string>();
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
            property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yOffset = position.y + EditorGUIUtility.singleLineHeight + 2;
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            
            SerializedProperty blendShapeNameProp = property.FindPropertyRelative("blendShapeName");
            SerializedProperty isStaticProp = property.FindPropertyRelative("isStatic");
            SerializedProperty staticValueProp = property.FindPropertyRelative("staticValue");
            SerializedProperty minValueProp = property.FindPropertyRelative("minValue");
            SerializedProperty maxValueProp = property.FindPropertyRelative("maxValue");
            SerializedProperty changeIntervalProp = property.FindPropertyRelative("changeInterval");
            SerializedProperty easeTypeProp = property.FindPropertyRelative("easeType");
            SerializedProperty useDelayedAnimationProp = property.FindPropertyRelative("useDelayedAnimation");
            SerializedProperty initialValueProp = property.FindPropertyRelative("initialValue");
            SerializedProperty animationDelayProp = property.FindPropertyRelative("animationDelay");
            
            Rect scanButtonRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(scanButtonRect, "🔍 Scan Scene for Blend Shapes"))
            {
                ScanForBlendShapes();
            }
            yOffset += lineHeight;
            
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
            
            Rect isStaticRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(isStaticRect, isStaticProp);
            yOffset += lineHeight;
            
            if (isStaticProp.boolValue)
            {
                Rect staticValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(staticValueRect, staticValueProp, 0f, 100f, new GUIContent("Static Value"));
                yOffset += lineHeight;
            }
            else
            {
                Rect headerRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(headerRect, "Animated Range", EditorStyles.boldLabel);
                yOffset += lineHeight;
                
                Rect minValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(minValueRect, minValueProp, 0f, 100f, new GUIContent("Min Value"));
                yOffset += lineHeight;
                
                Rect maxValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(maxValueRect, maxValueProp, 0f, 100f, new GUIContent("Max Value"));
                yOffset += lineHeight;
                
                Rect animHeaderRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(animHeaderRect, "Animation Settings", EditorStyles.boldLabel);
                yOffset += lineHeight;
                
                Rect intervalRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(intervalRect, changeIntervalProp, 0.1f, 2f, new GUIContent("Change Interval"));
                yOffset += lineHeight;
                
                Rect easeRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(easeRect, easeTypeProp);
                yOffset += lineHeight;
                
                Rect delayHeaderRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(delayHeaderRect, "Delayed Animation", EditorStyles.boldLabel);
                yOffset += lineHeight;
                
                Rect useDelayedRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(useDelayedRect, useDelayedAnimationProp, new GUIContent("Use Delayed Start"));
                yOffset += lineHeight;
                
                if (useDelayedAnimationProp.boolValue)
                {
                    Rect initialValueRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.Slider(initialValueRect, initialValueProp, 0f, 100f, new GUIContent("Initial Value"));
                    yOffset += lineHeight;
                    
                    Rect delayRect = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(delayRect, animationDelayProp, new GUIContent("Animation Delay (s)"));
                    yOffset += lineHeight;
                }
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
        SerializedProperty useDelayedAnimationProp = property.FindPropertyRelative("useDelayedAnimation");
        float lineHeight = EditorGUIUtility.singleLineHeight + 2;
        
        float height = lineHeight;
        height += lineHeight;
        height += lineHeight;
        height += lineHeight;
        
        if (isStaticProp.boolValue)
        {
            height += lineHeight;
        }
        else
        {
            height += lineHeight;
            height += lineHeight;
            height += lineHeight;
            height += lineHeight;
            height += lineHeight;
            height += lineHeight;
            
            height += lineHeight;
            height += lineHeight;
            
            if (useDelayedAnimationProp != null && useDelayedAnimationProp.boolValue)
            {
                height += lineHeight;
                height += lineHeight;
            }
        }
        
        return height;
    }
    
    private void ScanForBlendShapes()
    {
        cachedBlendShapes.Clear();
        HashSet<string> uniqueBlendShapes = new HashSet<string>();
        
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
        
        cachedBlendShapes = uniqueBlendShapes.OrderBy(x => x).ToList();
    }
}
