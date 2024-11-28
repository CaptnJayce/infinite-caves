using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PerlinNoiseMap : MonoBehaviour
{
    Dictionary<int, GameObject> tileset;
    Dictionary<int, GameObject> tilegroups;

    public GameObject prefabAir;
    public GameObject prefabStone;
    public GameObject prefabObsidian;
    public GameObject prefabLava;

    int mapWidth = 256;
    int mapHeight = 512;

    List<List<int>> noiseGrid = new List<List<int>>();
    List<List<GameObject>> tilegrid = new List<List<GameObject>>();

    List<List<int>> noiseAir = new List<List<int>>();
    List<List<GameObject>> airgrid = new List<List<GameObject>>();

    float zoom = 16.0f;
    int xOffset = 0;
    int yOffset = 0; 

    void Start() {        
        CreateTileSet();
        CreateTileGroup();
        GenerateMap();
        GenerateAirMap();
    }

    void CreateTileSet() {
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

    void GenerateMap() {
        for(int x = 0; x < mapWidth; x++) {
            noiseGrid.Add(new List<int>());
            tilegrid.Add(new List<GameObject>());

            for(int y = 0; y < mapHeight; y++) {
                int tileid = PerlinCave(x, y);
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
                int tileid = PerlinAir(x, y);
            
                if (tileid == 3) {
                    if (tilegrid[x][y] != null) {
                        Destroy(tilegrid[x][y]); 
                        tilegrid[x][y] = null; 
                    }
                }
            
                if (tilegrid[x][y] == null) {
                    CreateTile(tileid, x, y);
                }
            
                noiseAir[x].Add(tileid);
            }
        }
    }

    int PerlinCave(int x, int y) {
        float raw = Mathf.PerlinNoise((x - xOffset) / zoom, (y - yOffset) / zoom);
        float clamp = Mathf.Clamp01(raw);
        float scale = clamp * tileset.Count;
        
        if(scale == 4) {
            scale = 3;
        }
        return Mathf.FloorToInt(scale);
    }

    int PerlinAir(int x, int y) {
        float raw = Mathf.PerlinNoise((x - xOffset) / zoom, (y - yOffset) / zoom);
        float clamp = Mathf.Clamp01(raw);
        float scale = clamp * tileset.Count;
        
        if(scale >= 2.5f) {
            scale = 0;
        }
        return Mathf.FloorToInt(scale);
    }

    void CreateTile(int tileid, int x, int y) {
        GameObject tileprefab = tileset[tileid];
        GameObject tilegroup = tilegroups[tileid];
        GameObject tile = Instantiate(tileprefab, tilegroup.transform);

        tile.name = string.Format("tile_x{0}_y{1}", x, y);
        tile.transform.localPosition = new Vector3(x, y, 0);

        tilegrid[x].Add(tile);
    }
}