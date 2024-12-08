using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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

    List<List<int>> noiseAir = new List<List<int>>();
    List<List<GameObject>> airgrid = new List<List<GameObject>>();

    float zoom = 16.0f;
    int xOffset = 0;
    int yOffset = 0; 

    void Start() {        
        xOffset = Random.Range(0, 1000);
        yOffset = Random.Range(0, 1000);
        
        CreateTileSets();
        CreateFrequency();
        CreateTileGroup();
        GenerateMap();
        GenerateAirMap();
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
        foreach(KeyValuePair<int, GameObject> prefab_pair in tileset) {
            GameObject tilegroup = new(prefab_pair.Value.name);
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

    void GenerateMap() {
        for(int x = 0; x < mapWidth; x++) {
            noiseGrid.Add(new List<int>());
            tilegrid.Add(new List<GameObject>());

            for(int y = 0; y < mapHeight; y++) {
                int tileid = PerlinCave(x, y);
                if (tileid == 0) {
                    tileid = 1;
                }
                noiseGrid[x].Add(tileid);
                CreateTile(tileid, x, y);
            }
        }
    }
    
    void GenerateAirMap() {
        for (int x = 0; x < mapWidth; x++) {
            noiseAir.Add(new List<int>());
            airgrid.Add(new List<GameObject>());

            for (int y = 0; y < mapHeight; y++) {
                int airTileId = PerlinAir(x, y);

                if (airTileId == 0) { 
                    if (tilegrid[x][y] != null) {
                        Destroy(tilegrid[x][y]);
                        tilegrid[x][y] = null;
                    }
                }

                noiseAir[x].Add(airTileId);
            }
        }
    }

    int PerlinCave(int x, int y) {
        float raw = Mathf.PerlinNoise((x - xOffset) / zoom, (y - yOffset) / zoom);
        return GetTileIdFromNoise(raw);
    }

    int PerlinAir(int x, int y) {
        float raw = Mathf.PerlinNoise((x - (xOffset + 100)) / (zoom * 1.5f), (y - (yOffset + 100)) / (zoom * 1.5f));
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

        tilegrid[x].Add(tile);
    }
}