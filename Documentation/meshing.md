# Meshing

To extract the isosurfaces from the volumetric data, we decided to use meshing algoritms as they were deemed as the best solution. They generate a set of vertices which are then connected to form triangle, resulting in a triangulated mesh.
In order to find the best meshing algorithm for our projekt, we implemented **Marching Cubes** and **Dual Contouring**. Originally, we also looked at Surface Nets, but since it was designed for binary segmented data (either solid or air), it would not be possible to make smooth modification of voxels.

After having implemented both, we found out that Marching Cubes was much faster than Dual Contouring, since the Quadratic Error Function (QEF) have to be calculated in Dual Contouring. QEF is a function that minimizes an error along a gradient, and was used to figure out where to place vertices inside the grid cells using the tangent planes of the active edges in the cell. Because of this reason, we decided to continue the project using only Marching Cubes, as the voxel engine heavily relies on realtime performance.

## Large scale terrains

As we needed to support large scale terrains, and all of it has to be rendered, we decided to implement **Level of Detail** (LOD) to reduce the number of triangles and vertices, thereby lowering the mesh resolution. The mesh resolution would be based on how far away a chunk is from the player, and the further away it is, the lower resolution the chunk will be. 

## Level of Detail

Our original idea for the level of detail algorithm, was to sample every * *n* * voxel in a chunk, where * *n* * corresponds to the chunks LOD level + 1. It has to be plussed by one since our LOD levels starts at 0. 


sampling every n voxel generated unwanted offsets where chunks of different levels meet

solution was transvoxel using transitioncells

