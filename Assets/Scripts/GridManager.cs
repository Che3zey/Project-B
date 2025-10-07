using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Empty,
    Wall,
    Box,
    Player,
    Exit
}

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public TileType[,] grid;
    public GameObject boxPrefab;
    public GameObject tilePrefab; // assign your gray tile prefab
    public Transform tileParent;  // optional for hierarchy

    public int initialBoxCount = 3; // 🔹 Easily change how many boxes spawn

    private Dictionary<Vector2Int, GameObject> boxDict = new Dictionary<Vector2Int, GameObject>();

    void OnEnable()
    {
        InitializeGrid();
    }

    void Start()
    {
        GenerateCheckerboard();

        // Spawn initial boxes
        SpawnRandomBoxes(initialBoxCount);
    }

    private void InitializeGrid()
    {
        grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Empty;

        Debug.Log($"✅ Grid initialized: {width}x{height}");
    }

    private void GenerateCheckerboard()
    {
        if (tilePrefab == null) return;

        if (tileParent == null)
        {
            GameObject parentGO = new GameObject("TilesParent");
            tileParent = parentGO.transform;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = ((x + y) % 2 == 0) ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.6f, 0.6f, 0.6f);
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 1), Quaternion.identity, tileParent);
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
        }
    }

    // --- Box management ---
    public void AddBox(Vector2Int pos, GameObject box)
    {
        boxDict[pos] = box;
        grid[pos.x, pos.y] = TileType.Box;
    }

    public void RemoveBox(Vector2Int pos)
    {
        if (boxDict.ContainsKey(pos))
        {
            Destroy(boxDict[pos]);
            boxDict.Remove(pos);
        }
        grid[pos.x, pos.y] = TileType.Empty;
    }

    public GameObject GetBoxAtPosition(Vector2Int pos)
    {
        if (boxDict.ContainsKey(pos)) return boxDict[pos];
        return null;
    }

    public void MoveBox(Vector2Int from, Vector2Int to)
    {
        if (!boxDict.ContainsKey(from)) return;

        GameObject box = boxDict[from];
        box.transform.position = new Vector3(to.x, to.y, 0);
        boxDict.Remove(from);
        boxDict[to] = box;

        grid[from.x, from.y] = TileType.Empty;
        grid[to.x, to.y] = TileType.Box;
    }

    public bool IsEmpty(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;
        return grid[pos.x, pos.y] == TileType.Empty;
    }

    // --- Spawn a configurable number of boxes randomly ---
    public void SpawnRandomBoxes(int count)
    {
        int spawned = 0;

        while (spawned < count)
        {
            Vector2Int pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));

            if (IsEmpty(pos))
            {
                GameObject boxGO = Instantiate(boxPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                AddBox(pos, boxGO);
                spawned++;
            }
        }
    }

    // --- Match-3 detection ---
    public void CheckMatches()
    {
        List<Vector2Int> boxesToClear = new List<Vector2Int>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            int count = 1;
            for (int x = 1; x < width; x++)
            {
                if (grid[x, y] == TileType.Box && grid[x - 1, y] == TileType.Box) count++;
                else
                {
                    if (count >= 3)
                        for (int i = x - count; i < x; i++)
                            boxesToClear.Add(new Vector2Int(i, y));
                    count = 1;
                }
            }
            if (count >= 3)
                for (int i = width - count; i < width; i++)
                    boxesToClear.Add(new Vector2Int(i, y));
        }

        // Vertical
        for (int x = 0; x < width; x++)
        {
            int count = 1;
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] == TileType.Box && grid[x, y - 1] == TileType.Box) count++;
                else
                {
                    if (count >= 3)
                        for (int i = y - count; i < y; i++)
                            boxesToClear.Add(new Vector2Int(x, i));
                    count = 1;
                }
            }
            if (count >= 3)
                for (int i = height - count; i < height; i++)
                    boxesToClear.Add(new Vector2Int(x, i));
        }

        // Clear boxes
        foreach (Vector2Int pos in boxesToClear)
            RemoveBox(pos);

        if (boxesToClear.Count > 0)
            Debug.Log($"Cleared {boxesToClear.Count} boxes!");
    }
}
