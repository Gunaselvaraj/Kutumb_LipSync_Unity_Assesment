using UnityEngine;
[System.Serializable]
public class BlendShapeEntry
{
    [Tooltip("Name of the blendshape (from any mesh: Body, Eye, Teeth, Tongue, etc.)")]
    public string blendShapeName;
    [Range(0, 100)] 
    public float weight;
    
    public BlendShapeEntry() { }
    
    public BlendShapeEntry(string name, float weight)
    {
        this.blendShapeName = name;
        this.weight = weight;
    }
    public string GetBlendShapeName()
    {
        return blendShapeName;
    }
}
