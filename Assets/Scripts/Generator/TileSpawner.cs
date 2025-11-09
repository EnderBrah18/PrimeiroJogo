using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSizeX = 10;
    public int gridSizeZ = 10;
    public float cellSize = 1.5f;

    [Header("Spawn Settings")]
    public List<GameObject> spawnablePrefabs;
    [Range(0f, 1f)] public float spawnChance = 0.3f;
    public bool useSeed = false;
    public int customSeed = 0;

    [Header("Spawn Height Offset")]
    public float yOffset = 0.1f;

    public IEnumerator GenerateGridSpawnsCoroutine()
    {
        if (spawnablePrefabs == null || spawnablePrefabs.Count == 0)
            yield break;

        if (useSeed)
            Random.InitState(customSeed + (int)transform.position.sqrMagnitude);
        else
            Random.InitState(System.Environment.TickCount + (int)transform.position.sqrMagnitude);

        Vector3 origin = transform.position;
        float startX = origin.x - (gridSizeX / 2f) * cellSize;
        float startZ = origin.z - (gridSizeZ / 2f) * cellSize;

        int count = 0;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                if (Random.value > spawnChance)
                    continue;

                Vector3 spawnPos = new Vector3(
                    startX + x * cellSize,
                    origin.y + yOffset,
                    startZ + z * cellSize
                );

                if (Physics.Raycast(spawnPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, LayerMask.GetMask("Default", "Terrain")))
                {
                    spawnPos.y = hit.point.y + yOffset;
                }
                else
                {
                    spawnPos.y = Terrain.activeTerrain != null
                        ? Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y + yOffset
                        : origin.y + yOffset;
                }

                GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Count)];
                Instantiate(prefab, spawnPos, Quaternion.identity, transform);

                count++;
                if (count % 10 == 0) // pausa a cada 10 instâncias
                    yield return null;
            }
        }

        // pequena pausa no fim
        yield return null;
    }
}
