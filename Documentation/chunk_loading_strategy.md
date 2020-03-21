# Chunk Generation/Loading strategy

We need to keep track of the current loaded chunks, the loaded chunks are all the chunks present in the **chunks** Dictionary. If a chunk is unloaded from memory it is completely removed from this list.

```csharp
Dictionary<Vector3Int, VoxelChunk> chunks; // A dictionary containing all loaded chunks
```

Along with the loaded chunks we should also keep track of the currently visible chunks, meaning the chunks that are within the render distance of the camera.

```csharp
Dictionary<Vector3Int, VoxelChunk> visibleChunks; // A dictionary containing all visible chunks
```

---

A loaded chunk is stored in memory along with its voxels. Once it becomes **visible** a mesh should be generated for the corresponding LOD of said chunk.

Meshes are not saved in RAM (for non visible chunks) since we can always generate them from the voxel data, hence the only thing stored is said voxel data.

## Loading chunks

Chunks are loaded whenever the players view distance reaches a chunk coordinate which is missing a chunk.

## Unloading chunks

A chunk is unloaded whenever it is outside a certain multipler of the players view distance. We do not want to unload chunks as fast as we are loading them, such that we can reuse chunks when the player is revisiting previous chunks. Once the RAM usage becomes high enough, the chunks furthest away from the player should be unloaded.

## Saving chunks

Saving chunks should be done periodically rather than every time they are changed. Thus we need some kind of way to mark a chunk as being **dirty** such that we can quickly iterate the chunks that should be saved.

Chunks are saved using a simple run-length encoding to avoid having lots of data for empty/fille spaces, while keeping the required information near surfaces.
