using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseMap : MonoBehaviour
{
    Dictionary<int, GameObject> tileset;
    Dictionary<int, GameObject> tilegroups;
    Dictionary<int, (float min, float max)> tilefrequency;

    public GameObject prefabAir;
    public GameObject prefabStone;
    public GameObject prefabObsidian;
    public GameObject prefabLava;

    private int chunkSize = 16;
    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    private float zoom = 16.0f;
    private int loadRadius = 2;
    private Vector2Int lastPlayerChunk;

    void Start() {
        CreateTileSets();
        CreateFrequency();
        CreateTileGroup();
        lastPlayerChunk = new Vector2Int(int.MaxValue, int.MaxValue);
    }

    void Update() {
        MineTile();

        Vector2 playerPos = GameObject.Find("Player").transform.position;
        Vector2Int playerChunk = GetChunkCoordinates(playerPos);

        if (playerChunk != lastPlayerChunk) {
            LoadChunksAroundPlayer(playerChunk);
            UnloadDistantChunks(playerChunk);
            lastPlayerChunk = playerChunk;
        }
    }

    Vector2Int GetChunkCoordinates(Vector2 position) {
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkY = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    void LoadChunksAroundPlayer(Vector2Int playerChunk) {
        for (int x = playerChunk.x - loadRadius; x <= playerChunk.x + loadRadius; x++) {
            for (int y = playerChunk.y - loadRadius; y <= playerChunk.y + loadRadius; y++) {
                Vector2Int chunkCoords = new Vector2Int(x, y);

                if (!chunks.ContainsKey(chunkCoords)) {
                    GenerateChunk(chunkCoords);
                }
            }
        }
    }

    void UnloadDistantChunks(Vector2Int playerChunk) {
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        foreach (var chunk in chunks) {
            if (Mathf.Abs(chunk.Key.x - playerChunk.x) > loadRadius || Mathf.Abs(chunk.Key.y - playerChunk.y) > loadRadius) {
                chunksToUnload.Add(chunk.Key);
            }
        }

        foreach (var chunkCoords in chunksToUnload) {
            Destroy(chunks[chunkCoords]);
            chunks.Remove(chunkCoords);
        }
    }

    void GenerateChunk(Vector2Int chunkCoords) {
        GameObject chunk = new GameObject($"Chunk_{chunkCoords.x}_{chunkCoords.y}");
        chunk.transform.parent = transform;

        for (int x = 0; x < chunkSize; x++) {
            for (int y = 0; y < chunkSize; y++) {
                int worldX = chunkCoords.x * chunkSize + x;
                int worldY = chunkCoords.y * chunkSize + y;

                int tileid = PerlinCave(worldX, worldY);
                CreateTile(tileid, worldX, worldY, chunk.transform);
            }
        }

        chunks.Add(chunkCoords, chunk);
    }

    void MineTile() {
        if (Input.GetMouseButton(0)) {
            Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerDist = GameObject.Find("Player").transform.position;

            var diff = worldMousePos - playerDist;

            if (Mathf.Abs(diff.x) > 10 || Mathf.Abs(diff.y) > 10) {
                return;
            }

            int tileX = Mathf.RoundToInt(worldMousePos.x);
            int tileY = Mathf.RoundToInt(worldMousePos.y);

            Vector2Int chunkCoords = GetChunkCoordinates(new Vector2(tileX, tileY));

            if (chunks.ContainsKey(chunkCoords)) {
                GameObject chunk = chunks[chunkCoords];
                Transform tile = chunk.transform.Find($"tile_x{tileX}_y{tileY}");

                if (tile != null) {
                    Destroy(tile.gameObject);
                }
            }
        }
    }

    void CreateTileSets() {
        tileset = new Dictionary<int, GameObject>
        {
            { 0, prefabAir },
            { 1, prefabStone },
            { 2, prefabObsidian },
            { 3, prefabLava },
        };
    }

    void CreateTileGroup() {
        tilegroups = new Dictionary<int, GameObject>();
        foreach (KeyValuePair<int, GameObject> prefab_pair in tileset) {
            GameObject tilegroup = new GameObject(prefab_pair.Value.name);
            tilegroup.transform.parent = transform;
            tilegroup.transform.localPosition = new Vector3(0, 0, 0);
            tilegroups.Add(prefab_pair.Key, tilegroup);
        }
    }

    void CreateFrequency() {
        tilefrequency = new Dictionary<int, (float min, float max)> {
            { 0, (0.0f, 0.4f) },  // Air: 40% frequency
            { 1, (0.4f, 0.7f) },  // Stone: 30% frequency
            { 2, (0.7f, 0.9f) },  // Obsidian: 20% frequency
            { 3, (0.9f, 1.0f) }   // Lava: 10% frequency
        };
    }

    int PerlinCave(int x, int y) {
        float raw = Mathf.PerlinNoise(x / zoom, y / zoom);
        return GetTileIdFromNoise(raw);
    }

    int GetTileIdFromNoise(float noiseValue) {
        float clamp = Mathf.Clamp01(noiseValue);
        foreach (var entry in tilefrequency) {
            if (clamp >= entry.Value.min && clamp < entry.Value.max) {
                return entry.Key;
            }
        }
        return 0;
    }

    void CreateTile(int tileid, int x, int y, Transform parent) {
        GameObject tilePrefab = tileset[tileid];
        GameObject tile = Instantiate(tilePrefab, parent);

        tile.name = $"tile_x{x}_y{y}";
        tile.layer = LayerMask.NameToLayer("Ground");
        tile.transform.localPosition = new Vector3(x, y, 0);

        if (tileid != 0) {
            if (tile.GetComponent<Collider2D>() == null) {
                tile.AddComponent<BoxCollider2D>();
            }
        }
    }
}