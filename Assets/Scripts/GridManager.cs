using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Empty,
    Wall,
    RedBox,
    BlueBox,
    GreenBox
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public TileType[,] grid;

    [Header("Prefabs")]
    public GameObject redBoxPrefab;
    public GameObject blueBoxPrefab;
    public GameObject greenBoxPrefab;
    public GameObject tilePrefab;
    public GameObject playerPrefab;
    public GameObject goalPrefab;

    public Transform tileParent;

    [Header("Special Tiles")]
    public Vector2Int playerSpawn;
    public Vector2Int goalZone;

    private Dictionary<Vector2Int, GameObject> boxDict = new Dictionary<Vector2Int, GameObject>();
    private TileType[] boxTypes = { TileType.RedBox, TileType.BlueBox, TileType.GreenBox };

    void OnEnable()
    {
        InitializeGrid();
    }

    void Start()
    {
        GenerateCheckerboard();
        GenerateOutcrops();
        SpawnPlayer();
        SpawnGoal();
        GenerateProceduralPuzzle();
    }

    private void InitializeGrid()
    {
        grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Empty;
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
            for (int y = 0; y < height; y++)
            {
                Color color = ((x + y) % 2 == 0) ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.6f, 0.6f, 0.6f);
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 1), Quaternion.identity, tileParent);
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
    }

    private void GenerateOutcrops()
    {
        bool vertical = Random.value > 0.5f;

        if (vertical)
        {
            int playerY = Random.Range(0, height);
            int goalY = Random.Range(0, height);
            playerSpawn = new Vector2Int(-1, playerY);
            goalZone = new Vector2Int(width, goalY);
        }
        else
        {
            int playerX = Random.Range(0, width);
            int goalX = Random.Range(0, width);
            playerSpawn = new Vector2Int(playerX, -1);
            goalZone = new Vector2Int(goalX, height);
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null) return;
        GameObject playerGO = Instantiate(playerPrefab, new Vector3(playerSpawn.x, playerSpawn.y, 0), Quaternion.identity);
        PlayerController pc = playerGO.GetComponent<PlayerController>();
        pc.gridManager = this;
        pc.playerPos = playerSpawn;
    }

    public void SpawnGoal()
    {
        if (goalPrefab == null) return;
        Instantiate(goalPrefab, new Vector3(goalZone.x, goalZone.y, 0), Quaternion.identity);
    }

    public void AddBox(Vector2Int pos, GameObject box, TileType type)
    {
        boxDict[pos] = box;
        grid[pos.x, pos.y] = type;
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

        grid[to.x, to.y] = grid[from.x, from.y];
        grid[from.x, from.y] = TileType.Empty;
    }

    public bool IsEmpty(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;
        return grid[pos.x, pos.y] == TileType.Empty;
    }

    private GameObject GetPrefabForType(TileType type)
    {
        switch (type)
        {
            case TileType.RedBox: return redBoxPrefab;
            case TileType.BlueBox: return blueBoxPrefab;
            case TileType.GreenBox: return greenBoxPrefab;
            default: return null;
        }
    }

    // ------------------------------
    // Procedural Puzzle Generator
    // ------------------------------
    private void GenerateProceduralPuzzle()
{
    // 1️⃣ Generate a clear path vector
    List<Vector2Int> path = GeneratePath(playerSpawn, goalZone);

    // 2️⃣ Determine number of blocking clusters (2-3)
    int clusterCount = Random.Range(2, 4);

    // Split path into cluster positions
    List<Vector2Int> clusterPositions = new List<Vector2Int>();
    int pathStep = Mathf.Max(1, path.Count / (clusterCount + 1));
    for (int i = 1; i <= clusterCount; i++)
    {
        clusterPositions.Add(path[i * pathStep]);
    }

    // 3️⃣ Place blocking clusters
    foreach (Vector2Int center in clusterPositions)
    {
        PlaceCluster(center);
    }

    // 4️⃣ Add extra side boxes
    int extraBoxes = Random.Range(2, 5);
    int attempts = 0;
    while (extraBoxes > 0 && attempts < 50)
    {
        Vector2Int pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
        if (IsEmpty(pos) && !path.Contains(pos) && !IsEdgeTile(pos))
        {
            TileType type = GetRandomBoxTypeAvoidMatches(pos);
            GameObject prefab = GetPrefabForType(type);
            GameObject boxGO = Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            AddBox(pos, boxGO, type);
            extraBoxes--;
        }
        attempts++;
    }
}

// ------------------------------
// Place a 3-box cluster around a center tile
// ------------------------------
private void PlaceCluster(Vector2Int center)
{
    List<Vector2Int> clusterTiles = new List<Vector2Int> { center };

    // Add 1-2 adjacent tiles (orthogonal)
    Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    ShuffleArray(dirs);

    for (int i = 0; i < 2; i++)
    {
        Vector2Int pos = center + dirs[i];
        if (IsInsideGrid(pos) && IsEmpty(pos) && !clusterTiles.Contains(pos))
            clusterTiles.Add(pos);
    }

    // Assign same type for horizontal/vertical match
    TileType type = boxTypes[Random.Range(0, boxTypes.Length)];

    foreach (Vector2Int pos in clusterTiles)
    {
        GameObject prefab = GetPrefabForType(type);
        GameObject boxGO = Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
        AddBox(pos, boxGO, type);
    }
}

// ------------------------------
// Helpers
// ------------------------------
private bool IsInsideGrid(Vector2Int pos)
{
    return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
}

private void ShuffleArray(Vector2Int[] array)
{
    for (int i = array.Length - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);
        Vector2Int temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
}


    // Generate a simple straight-ish path between start and end
    private List<Vector2Int> GeneratePath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        int x = Mathf.Clamp(start.x + 1, 0, width - 1);
        int y = Mathf.Clamp(start.y + 1, 0, height - 1);

        while (x != Mathf.Clamp(end.x - 1, 0, width - 1) || y != Mathf.Clamp(end.y - 1, 0, height - 1))
        {
            path.Add(new Vector2Int(x, y));
            if (x != Mathf.Clamp(end.x - 1, 0, width - 1) && Random.value > 0.5f)
                x += x < end.x ? 1 : -1;
            else if (y != Mathf.Clamp(end.y - 1, 0, height - 1))
                y += y < end.y ? 1 : -1;
        }

        return path;
    }

    private bool IsEdgeTile(Vector2Int pos)
    {
        return pos.x == 0 || pos.x == width - 1 || pos.y == 0 || pos.y == height - 1;
    }

    private TileType GetRandomBoxTypeAvoidMatches(Vector2Int pos)
    {
        // Avoid creating initial matches
        List<TileType> validTypes = new List<TileType>(boxTypes);
        foreach (TileType type in boxTypes)
        {
            if (WouldCauseMatch(pos, type))
                validTypes.Remove(type);
        }

        if (validTypes.Count == 0) validTypes.Add(boxTypes[Random.Range(0, boxTypes.Length)]);
        return validTypes[Random.Range(0, validTypes.Count)];
    }

    private bool WouldCauseMatch(Vector2Int pos, TileType type)
    {
        // Horizontal
        int countH = 1;
        if (pos.x >= 2)
        {
            if (grid[pos.x - 1, pos.y] == type && grid[pos.x - 2, pos.y] == type)
                return true;
        }
        // Vertical
        if (pos.y >= 2)
        {
            if (grid[pos.x, pos.y - 1] == type && grid[pos.x, pos.y - 2] == type)
                return true;
        }
        return false;
    }

    // ------------------------------
    // Match-3 detection
    // ------------------------------
    public void CheckMatches()
    {
        List<Vector2Int> boxesToClear = new List<Vector2Int>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            int count = 1;
            for (int x = 1; x < width; x++)
            {
                TileType current = grid[x, y];
                TileType previous = grid[x - 1, y];

                if (current != TileType.Empty && current == previous)
                    count++;
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
                TileType current = grid[x, y];
                TileType previous = grid[x, y - 1];

                if (current != TileType.Empty && current == previous)
                    count++;
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

        foreach (Vector2Int pos in boxesToClear)
            RemoveBox(pos);

        if (boxesToClear.Count > 0)
            Debug.Log($"Cleared {boxesToClear.Count} boxes!");
    }
}
