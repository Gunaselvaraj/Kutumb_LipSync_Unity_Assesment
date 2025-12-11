using UnityEngine;

/// <summary>
/// Dynamic blendshape entry - stores name as string for maximum flexibility
/// No need to manually update enums!
/// </summary>
[System.Serializable]
public class BlendShapeEntry
{
    [Tooltip("Name of the blendshape (from any mesh: Body, Eye, Teeth, Tongue, etc.)")]
    public string blendShapeName;
    
    [Tooltip("Weight value: 0 = no effect, 100 = full effect")]
    [Range(0, 100)] 
    public float weight;
    
    public BlendShapeEntry() { }
    
    public BlendShapeEntry(string name, float weight)
    {
        this.blendShapeName = name;
        this.weight = weight;
    }
    
    /// <summary>
    /// Get the blendshape name
    /// </summary>
    public string GetBlendShapeName()
    {
        return blendShapeName;
    }
}
