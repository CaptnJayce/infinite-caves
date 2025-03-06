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

    int mapWidth = 128;
    int mapHeight = 128;

    List<List<int>> noiseGrid = new List<List<int>>();
    List<List<GameObject>> tilegrid = new List<List<GameObject>>();

    float zoom = 16.0f;
    int xOffset = 0;
    int yOffset = 0;

    private int bufferRadius = 30;
    private Vector2 lastPlayerPos;

    void Start() {
        xOffset = Random.Range(0, 1000);
        yOffset = Random.Range(0, 1000);

        CreateTileSets();
        CreateFrequency();
        CreateTileGroup();
        InitializeGrids();
        lastPlayerPos = Vector2.zero;
    }

    void Update() {
        MineTile();

        Vector2 playerPos = GameObject.Find("Player").transform.position;

        if (Vector2.Distance(playerPos, lastPlayerPos) > 1.0f) {
            LoadTilesAroundPlayer(playerPos);
            UnloadTilesOutsideRadius(playerPos);
            lastPlayerPos = playerPos;
        }
    }

    void InitializeGrids() {
        for (int x = 0; x < mapWidth; x++) {
            noiseGrid.Add(new List<int>());
            tilegrid.Add(new List<GameObject>());

            for (int y = 0; y < mapHeight; y++) {
                noiseGrid[x].Add(0);
                tilegrid[x].Add(null);
            }
        }
    }

    void LoadTilesAroundPlayer(Vector2 playerPos) {
        int startX = Mathf.Clamp(Mathf.FloorToInt(playerPos.x) - bufferRadius, 0, mapWidth - 1);
        int endX = Mathf.Clamp(Mathf.FloorToInt(playerPos.x) + bufferRadius, 0, mapWidth - 1);
        int startY = Mathf.Clamp(Mathf.FloorToInt(playerPos.y) - bufferRadius, 0, mapHeight - 1);
        int endY = Mathf.Clamp(Mathf.FloorToInt(playerPos.y) + bufferRadius, 0, mapHeight - 1);

        for (int x = startX; x <= endX; x++) {
            for (int y = startY; y <= endY; y++) {
                if (tilegrid[x][y] == null) {
                    int tileid = PerlinCave(x, y);
                    CreateTile(tileid, x, y);
                }
            }
        }
    }

    void UnloadTilesOutsideRadius(Vector2 playerPos) {
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                if (tilegrid[x][y] != null) {
                    if (Mathf.Abs(x - playerPos.x) > bufferRadius || Mathf.Abs(y - playerPos.y) > bufferRadius) {
                        Destroy(tilegrid[x][y]);
                        tilegrid[x][y] = null;
                    }
                }
            }
        }
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

            if (tileX >= 0 && tileX < mapWidth && tileY >= 0 && tileY < mapHeight) {
                GameObject tile = tilegrid[tileX][tileY];

                if (tile != null) {
                    int tileID = noiseGrid[tileX][tileY];

                    if (tileID == 2 || tileID == 3) {
                        return;
                    }

                    Destroy(tile);
                    tilegrid[tileX][tileY] = null;
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
            tilegroup.transform.parent = gameObject.transform;
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
        float raw = Mathf.PerlinNoise((x - xOffset) / zoom, (y - yOffset) / zoom);
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

    void CreateTile(int tileid, int x, int y) {
        GameObject tilePrefab = tileset[tileid];
        GameObject tileGroup = tilegroups[tileid];
        GameObject tile = Instantiate(tilePrefab, tileGroup.transform);

        tile.name = string.Format("tile_x{0}_y{1}", x, y);
        tile.layer = LayerMask.NameToLayer("Ground");
        tile.transform.localPosition = new Vector3(x, y, 0);

        if (tileid != 0) {
            if (tile.GetComponent<Collider2D>() == null) {
                tile.AddComponent<BoxCollider2D>();
            }
        }

        tilegrid[x][y] = tile;
        noiseGrid[x][y] = tileid;
    }
}