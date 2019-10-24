using UnityEngine;

namespace VoxelMaster
{
    public static class Util
    {
        public static int Map3DTo1D(int x, int y, int z, int size)
        {
            return x + size * (y + size * z);
        }

        public static Vector3Int Map1DTo3D(int i, int size)
        {
            return new Vector3Int(
                    i % (size),
                    (i / size) % size,
                    i / (size * size)
                );
        }


    }
}
