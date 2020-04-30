using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace VoxelMaster.Chunk {
    public class ChunkSerializer {


        List<Vector3Int> availableChunkCoordinates = new List<Vector3Int>();
        string folderPath;

        public ChunkSerializer(string folder) {
            folderPath = $"{Application.persistentDataPath}/{folder}";
            Debug.Log(folderPath);

            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }

            FetchExistingChunkCoords();
        }

        private void FetchExistingChunkCoords() {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files) {
                if (!file.Contains(".chunk")) continue;

                var coord = Path.GetFileNameWithoutExtension(file).Split('_');
                availableChunkCoordinates.Add(new Vector3Int(
                    int.Parse(coord[0]),
                    int.Parse(coord[1]),
                    int.Parse(coord[2])
                    )
                );
            }
        }

        public void SaveChunk(VoxelChunk chunk) {
            var formatter = new BinaryFormatter();
            var stream = new FileStream(GetFilePath(chunk.coords), FileMode.OpenOrCreate);

            formatter.Serialize(stream, chunk.voxels.ToArray());
            stream.Close();

            availableChunkCoordinates.Add(chunk.coords);
        }

        public VoxelChunk LoadChunk(Vector3Int coords) {
            var chunkFilepath = GetFilePath(coords);

            if (!File.Exists(chunkFilepath))
                throw new Exception($"Chunk file ({chunkFilepath}) does not exist");


            var formatter = new BinaryFormatter();
            var stream = new FileStream(chunkFilepath, FileMode.Open);

            return formatter.Deserialize(stream) as VoxelChunk;

        }

        public List<Vector3Int> GetAvailableChunks() {
            return new List<Vector3Int>(availableChunkCoordinates);
        }

        string GetFilePath(Vector3Int coords) => Path.Combine(folderPath, $"{coords.x}_{coords.y}_{coords.z}.chunk");
    }
}
