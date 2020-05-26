using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {
    public abstract class WorldFeatureGenerator {
        public abstract void Generate(float[] heightmap, VoxelChunk chunk, WorldGeneratorSettings settings);
    }
}