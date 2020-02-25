using UnityEngine;

public class MaterialTool : VoxelTool
{
    public VoxelMaterial material { get; private set; }
    public byte materialIndex { get; private set; }

    public override void apply()
    {
        throw new System.NotImplementedException();
    }

}
