using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Expression", menuName = "Facial System/Expression", order = 1)]
public class FacialExpressionData : ScriptableObject
{
    public string expressionName;
    public List<BlendShapeEntry> blendShapes = new List<BlendShapeEntry>();
    
    [Range(0f, 1f)]
    public float transitionDuration = 0.3f;
    public FacialExpressionData CreateVariant(float intensityMultiplier)
    {
        var variant = CreateInstance<FacialExpressionData>();
        variant.expressionName = expressionName + $" ({intensityMultiplier:P0})";
        variant.transitionDuration = transitionDuration;
        
        variant.blendShapes = new List<BlendShapeEntry>();
        foreach (var bs in blendShapes)
        {
            variant.blendShapes.Add(new BlendShapeEntry(
                bs.blendShapeName,
                bs.weight * intensityMultiplier
            ));
        }
        
        return variant;
    }
    public List<string> GetBlendShapeNames()
    {
        List<string> names = new List<string>();
        foreach (var bs in blendShapes)
        {
            names.Add(bs.GetBlendShapeName());
        }
        return names;
    }
    public List<string> ValidateBlendShapes(SkinnedMeshRenderer renderer)
    {
        List<string> missing = new List<string>();
        
        if (renderer == null || renderer.sharedMesh == null)
            return missing;
        
        Mesh mesh = renderer.sharedMesh;
        List<string> availableShapes = new List<string>();
        
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            availableShapes.Add(mesh.GetBlendShapeName(i));
        }
        
        foreach (var bs in blendShapes)
        {
            string name = bs.GetBlendShapeName();
            if (!availableShapes.Contains(name))
            {
                missing.Add(name);
            }
        }
        
        return missing;
    }
}
