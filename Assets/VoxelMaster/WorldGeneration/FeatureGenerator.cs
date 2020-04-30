using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {
    public abstract class FeatureGenerator {
        public abstract void Generate(WorldGeneratorSettings settings, VoxelChunk chunk);
    }
}