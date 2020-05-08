using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;



public static class PoissonSampler {


    public static List<Vector3> GeneratePoints(Vector3 regionSize, float radius, Vector3 impactPoint, int numSamplesBeforeRejection = 2) {

        var cellSize = radius / Mathf.Sqrt(2);

        int[,,] grid = new int[Mathf.CeilToInt(regionSize.x / cellSize), Mathf.CeilToInt(regionSize.y / cellSize), Mathf.CeilToInt(regionSize.z / cellSize)];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>();

        points.Add(regionSize / 2);
        spawnPoints.Add(regionSize / 2);

        while (spawnPoints.Count > 0) {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCenter = spawnPoints[spawnIndex];

            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++) {
                var angle1 = 2 * Mathf.PI * Random.value;
                var angle2 = 2 * Mathf.PI * Random.value;

                Vector3 dir = new Vector3(Mathf.Cos(angle1) * Mathf.Sin(angle2), Mathf.Sin(angle1) * Mathf.Sin(angle2), Mathf.Cos(angle2));
                Vector3 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);

                float distanceFromImpact = Mathf.Max(.5f, (candidate - impactPoint).sqrMagnitude);

                if (IsValid(candidate, regionSize, cellSize, distanceFromImpact, points, grid)) {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize), (int)(candidate.z / cellSize)] = points.Count;
                    candidateAccepted = true;
                    break;
                }

            }
            if (!candidateAccepted) {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
        return points;

    }

    static bool IsValid(Vector3 candidate, Vector3 regionSize, float cellSize, float radius, List<Vector3> points, int[,,] grid) {
        if (candidate.x >= 0 && candidate.x < regionSize.x && candidate.y >= 0 && candidate.y < regionSize.y && candidate.z >= 0 && candidate.z < regionSize.z) {

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int cellZ = (int)(candidate.z / cellSize);

            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);
            int searchStartZ = Mathf.Max(0, cellZ - 2);
            int searchEndZ = Mathf.Min(cellZ + 2, grid.GetLength(2) - 1);


            for (int x = searchStartX; x <= searchEndX; x++)
                for (int y = searchStartY; y <= searchEndY; y++)
                    for (int z = searchStartZ; z <= searchEndZ; z++) {
                        int pointsIndex = grid[x, y, z] - 1;
                        if (pointsIndex != -1) {
                            float dist = (candidate - points[pointsIndex]).sqrMagnitude;
                            if (dist < radius * radius) {
                                return false;
                            }
                        }
                    }
            return true;
        }
        return false;
    }
}