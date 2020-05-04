using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelMaster.Chunk {

    public class VoxelChunk : IVoxelData {

        public VoxelWorld voxelWorld;

        public VoxelDataStructure voxels { get; private set; }
        private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
        public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

        public Vector3Int coords { get; private set; }
        public Vector3Int size { get; private set; }
        public float voxelScale { get; private set; }

        public bool hasData { get; private set; } = false;

        public bool hasSolids = false;
        public bool needsUpdate = false;

        public Voxel this [Vector3 v] {
            get => this [new Vector3Int ((int) v.x, (int) v.y, (int) v.z)];
            set => this [new Vector3Int ((int) v.x, (int) v.y, (int) v.z)] = value;
        }
        public Voxel this [Vector3Int v] {
            get => GetVoxel (v);
            set => SetVoxel (v, value);
        }
        public Voxel this [int x, int y, int z] {
            get => this [new Vector3Int (x, y, z)];
            set => this [new Vector3Int (x, y, z)] = value;
        }

        public VoxelChunk (Vector3Int coords, Vector3Int size, float voxelScale, VoxelDataStructure voxels) {
            this.coords = coords;
            this.size = size;
            this.voxelScale = voxelScale;
            this.voxels = voxels;
            this.voxels.Init (this.size);
        }

        public void AddDensity (Vector3 pos, float[][][] densities) {
            throw new NotImplementedException ();
        }

        public void SetDensity (Vector3 pos, float[][][] densities) {
            throw new NotImplementedException ();
        }

        public void RemoveDensity (Vector3 pos, float[][][] densities) {
            throw new NotImplementedException ();
        }

        public VoxelMaterial GetMaterial (Vector3 pos) {
            throw new NotImplementedException ();
        }

        public void SetMaterial (Vector3 pos, byte materialIndex) {
            throw new NotImplementedException ();
        }

        public void SetMaterialInRadius (Vector3 pos, float radius, byte materialIndex) {
            throw new NotImplementedException ();
        }

        private Voxel GetVoxel (Vector3Int coord) => voxels.GetVoxel (coord);
        private void SetVoxel (Vector3Int coord, Voxel voxel) => voxels.SetVoxel (coord, voxel);

        public void setHasData () {
            this.hasData = true;
        }
    }
}