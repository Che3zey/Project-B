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
    [Header("Grid Settings (8–15)")]
    [Range(8, 15)] public int width = 8;
    [Range(8, 15)] public int height = 8;
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
    private List<Coroutine> activeShatters = new List<Coroutine>();

    [Header("Generation Tuning")]
    public int minClusters = 2;
    public int maxClusters = 4;
    [Range(0f, 0.3f)] public float fillerDensity = 0.1f;
    public int safeRadius = 1; // tiles around player spawn that stay empty

    void OnEnable() => InitializeGrid();

    void Start()
    {
        width = Mathf.Clamp(width, 8, 15);
        height = Mathf.Clamp(height, 8, 15);
        GenerateLevel();
    }

    // --------------------------------------------------------------------
    // GRID SETUP
    // --------------------------------------------------------------------
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

        for (int i = tileParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(tileParent.GetChild(i).gameObject);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Color color = ((x + y) % 2 == 0)
                    ? new Color(0.86f, 0.86f, 0.86f)
                    : new Color(0.7f, 0.7f, 0.7f);
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 1f), Quaternion.identity, tileParent);
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
    }

    // --------------------------------------------------------------------
    // MAIN LEVEL GENERATION
    // --------------------------------------------------------------------
    public void GenerateLevel()
    {
        ClearAllBoxesAndObjects();
        InitializeGrid();
        GenerateCheckerboard();
        GenerateOutcrops();
        SpawnPlayer();
        SpawnGoal();

        int attempts = 0;
        const int maxAttempts = 8;
        bool success = false;

        while (!success && attempts < maxAttempts)
        {
            ClearAllBoxesOnly();

            List<Vector2Int> path = GenerateJaggedPath(playerSpawn, goalZone);
            int clusterCount = Random.Range(minClusters, maxClusters + 1);
            PlaceClustersAlongPath(path, clusterCount);
            PlaceFillerBoxes(path);

            if (AtLeastOnePushableBoxExists() && !AllBoxesOnEdges())
                success = true;
            else
                attempts++;
        }

        if (!success)
            Debug.LogWarning("Procedural generator may not fully satisfy constraints; level may be simple.");

        AddBorderBoxes();
        BlockGoalAccess();
        CameraController camController = Camera.main.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.FocusOnGrid(width, height);
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
        GameObject playerGO = Instantiate(playerPrefab, new Vector3(playerSpawn.x, playerSpawn.y, 0f), Quaternion.identity);
        PlayerController pc = playerGO.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.gridManager = this;
            pc.playerPos = playerSpawn;
        }
    }

    public void SpawnGoal()
    {
        if (goalPrefab == null) return;
        Instantiate(goalPrefab, new Vector3(goalZone.x, goalZone.y, 0f), Quaternion.identity);
    }

    // --------------------------------------------------------------------
    // BOX MANAGEMENT
    // --------------------------------------------------------------------
    public void AddBox(Vector2Int pos, GameObject box, TileType type)
    {
        boxDict[pos] = box;
        grid[pos.x, pos.y] = type;
    }

    // Called when a box should be removed with animation (e.g., match-3)
    public void RemoveBox(Vector2Int pos)
    {
        if (!boxDict.ContainsKey(pos)) return;

        GameObject box = boxDict[pos];

        // Mark the tile empty immediately so logic sees it as gone
        grid[pos.x, pos.y] = TileType.Empty;

        // Start animation
        Coroutine c = StartCoroutine(ShatterAndFadeBox(box, pos));
        activeShatters.Add(c);
    }

    private System.Collections.IEnumerator ShatterAndFadeBox(GameObject box, Vector2Int pos)
    {
        if (box == null) yield break;

        SpriteRenderer sr = box.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = box.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        Vector3 startScale = box.transform.localScale;
        Vector3 targetScale = startScale * 1.3f;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            box.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        // Remove from dictionary and destroy
        if (boxDict.ContainsKey(pos))
            boxDict.Remove(pos);

        Destroy(box);

        // Cleanup coroutine reference
        activeShatters.RemoveAll(c => c == null);
    }

    // Instantly destroy all boxes (used for restart)
    private void DestroyAllBoxesInstantly()
    {
        foreach (var kvp in boxDict)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        boxDict.Clear();
        activeShatters.Clear();

        // Mark grid empty
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Empty;
    }

    public void ClearAllBoxesOnly()
    {
        foreach (var kvp in new Dictionary<Vector2Int, GameObject>(boxDict))
            RemoveBox(kvp.Key);
    }

    public void ClearAllBoxesAndObjects()
    {
        DestroyAllBoxesInstantly();

        if (tileParent != null)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in tileParent) children.Add(child);

            foreach (Transform child in children)
            {
                if (child.CompareTag("Tile"))
                    Destroy(child.gameObject);
            }
        }
    }

    public bool MoveBox(Vector2Int from, Vector2Int to)
    {
        if (!boxDict.ContainsKey(from)) return false;
        if (!IsInsideGrid(to) || !IsEmpty(to)) return false;

        GameObject box = boxDict[from];
        TileType type = grid[from.x, from.y];

        boxDict.Remove(from);
        boxDict[to] = box;

        grid[from.x, from.y] = TileType.Empty;
        grid[to.x, to.y] = type;

        StartCoroutine(AnimateBoxMove(box, to));
        return true;
    }

    private System.Collections.IEnumerator AnimateBoxMove(GameObject box, Vector2Int target)
    {
        Vector3 startPos = box.transform.position;
        Vector3 endPos = new Vector3(target.x, target.y, 0f);
        float duration = 0.15f;
        Vector3 overshootPos = endPos + (endPos - startPos).normalized * 0.15f;
        float elapsed = 0f;

        Vector3 originalScale = Vector3.one;
        Vector3 squashScale = new Vector3(1.1f, 0.9f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t < 0.7f)
            {
                float pushT = Mathf.SmoothStep(0f, 1f, t / 0.7f);
                box.transform.position = Vector3.Lerp(startPos, overshootPos, pushT);
            }
            else
            {
                float reboundT = Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f);
                box.transform.position = Vector3.Lerp(overshootPos, endPos, reboundT);
            }

            float squash = Mathf.Sin(t * Mathf.PI);
            box.transform.localScale = Vector3.Lerp(originalScale, squashScale, squash);

            yield return null;
        }

        box.transform.position = endPos;
        box.transform.localScale = originalScale;
    }

    // --------------------------------------------------------------------
    // LEVEL RESTART
    // --------------------------------------------------------------------
    public void RestartLevel()
    {
        // Immediately destroy all boxes without animation
        DestroyAllBoxesInstantly();

        // Destroy player
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
            Destroy(existingPlayer.gameObject);

        // Destroy goal
        GameObject existingGoal = GameObject.FindWithTag("Goal");
        if (existingGoal != null)
            Destroy(existingGoal);

        // Clear tiles
        ClearAllBoxesAndObjects();

        // Generate fresh level
        GenerateLevel();
    }

    // --------------------------------------------------------------------
    // MATCH-3 DETECTION
    // --------------------------------------------------------------------
    public List<Vector2Int> CheckMatches()
    {
        HashSet<Vector2Int> toClear = new HashSet<Vector2Int>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            int runLength = 1;
            for (int x = 1; x < width; x++)
            {
                if (grid[x, y] != TileType.Empty && grid[x, y] == grid[x - 1, y])
                    runLength++;
                else
                    runLength = 1;

                if (runLength >= 3)
                    for (int k = 0; k < runLength; k++)
                        toClear.Add(new Vector2Int(x - k, y));
            }
        }

        // Vertical
        for (int x = 0; x < width; x++)
        {
            int runLength = 1;
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] != TileType.Empty && grid[x, y] == grid[x, y - 1])
                    runLength++;
                else
                    runLength = 1;

                if (runLength >= 3)
                    for (int k = 0; k < runLength; k++)
                        toClear.Add(new Vector2Int(x, y - k));
            }
        }

        return new List<Vector2Int>(toClear);
    }

    public void ClearMatches()
    {
        List<Vector2Int> matches = CheckMatches();
        foreach (Vector2Int pos in matches)
            RemoveBox(pos);
    }

    // --------------------------------------------------------------------
    // UTILITY
    // --------------------------------------------------------------------
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rand = Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    private TileType GetRandomBoxTypeAvoidMatches(Vector2Int pos)
    {
        List<TileType> valid = new List<TileType>(boxTypes);
        foreach (TileType t in boxTypes)
            if (WouldCauseMatch(pos, t))
                valid.Remove(t);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : boxTypes[Random.Range(0, boxTypes.Length)];
    }

    private bool WouldCauseMatch(Vector2Int pos, TileType type)
    {
        int horiz = 1;
        for (int dx = -1; dx >= -2 && pos.x + dx >= 0; dx--)
            if (grid[pos.x + dx, pos.y] == type) horiz++; else break;
        for (int dx = 1; dx <= 2 && pos.x + dx < width; dx++)
            if (grid[pos.x + dx, pos.y] == type) horiz++; else break;

        int vert = 1;
        for (int dy = -1; dy >= -2 && pos.y + dy >= 0; dy--)
            if (grid[pos.x, pos.y + dy] == type) vert++; else break;
        for (int dy = 1; dy <= 2 && pos.y + dy < height; dy++)
            if (grid[pos.x, pos.y + dy] == type) vert++; else break;

        return horiz >= 3 || vert >= 3;
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

    public bool IsEmpty(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return false;
        return grid[pos.x, pos.y] == TileType.Empty;
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public bool AllBoxesOnEdges()
    {
        foreach (var kvp in boxDict)
        {
            Vector2Int pos = kvp.Key;
            if (pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1)
                return false;
        }
        return true;
    }

    public bool AtLeastOnePushableBoxExists()
    {
        foreach (var kvp in boxDict)
        {
            Vector2Int pos = kvp.Key;
            foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int next = pos + dir;
                if (IsInsideGrid(next) && IsEmpty(next))
                    return true;
            }
        }
        return false;
    }

    private void AddBorderBoxes()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                bool isEdge = (x == 0 || y == 0 || x == width - 1 || y == height - 1);
                Vector2Int pos = new Vector2Int(x, y);
                if (isEdge && IsEmpty(pos) && Vector2Int.Distance(pos, playerSpawn) > safeRadius && Random.value < 0.65f)
                {
                    TileType type = GetRandomBoxTypeAvoidMatches(pos);
                    GameObject prefab = GetPrefabForType(type);
                    GameObject box = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
                    AddBox(pos, box, type);
                }
            }
    }

    private void BlockGoalAccess()
    {
        Vector2Int goalEntry = goalZone;
        if (goalZone.x < 0) goalEntry = new Vector2Int(0, goalZone.y);
        else if (goalZone.x >= width) goalEntry = new Vector2Int(width - 1, goalZone.y);
        else if (goalZone.y < 0) goalEntry = new Vector2Int(goalZone.x, 0);
        else if (goalZone.y >= height) goalEntry = new Vector2Int(goalZone.x, height - 1);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int pos = goalEntry + new Vector2Int(dx, dy);
                if (IsInsideGrid(pos) && IsEmpty(pos) && Vector2Int.Distance(pos, playerSpawn) > safeRadius)
                {
                    TileType type = GetRandomBoxTypeAvoidMatches(pos);
                    GameObject prefab = GetPrefabForType(type);
                    GameObject box = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
                    AddBox(pos, box, type);
                }
            }
    }

    private List<Vector2Int> GenerateJaggedPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(Mathf.Clamp(start.x, 0, width - 1), Mathf.Clamp(start.y, 0, height - 1));
        Vector2Int target = new Vector2Int(Mathf.Clamp(end.x, 0, width - 1), Mathf.Clamp(end.y, 0, height - 1));
        path.Add(cur);

        int twists = Random.Range(5, 9);
        for (int i = 0; i < twists; i++)
        {
            Vector2Int dir = Random.value > 0.5f ? Vector2Int.right : Vector2Int.up;
            if (Random.value > 0.5f) dir *= -1;

            int steps = Random.Range(2, 4);
            for (int s = 0; s < steps; s++)
            {
                cur += dir;
                cur.x = Mathf.Clamp(cur.x, 0, width - 1);
                cur.y = Mathf.Clamp(cur.y, 0, height - 1);
                if (!path.Contains(cur)) path.Add(cur);
                if (cur == target) return path;
            }
        }
        path.Add(target);
        return path;
    }

    private void PlaceClustersAlongPath(List<Vector2Int> path, int clusterCount)
    {
        ShuffleList(path);
        for (int i = 0; i < clusterCount; i++)
        {
            if (i >= path.Count) break;
            Vector2Int center = path[i];
            int clusterSize = Random.Range(3, 5);

            for (int j = 0; j < clusterSize; j++)
            {
                Vector2Int offset = new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));
                Vector2Int pos = center + offset;
                if (IsInsideGrid(pos) && IsEmpty(pos) && Vector2Int.Distance(pos, playerSpawn) > safeRadius)
                {
                    TileType type = GetRandomBoxTypeAvoidMatches(pos);
                    GameObject prefab = GetPrefabForType(type);
                    GameObject box = Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    AddBox(pos, box, type);
                }
            }
        }
    }

    private void PlaceFillerBoxes(List<Vector2Int> path)
    {
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsEmpty(pos) && Random.value < fillerDensity && Vector2Int.Distance(pos, playerSpawn) > safeRadius)
                {
                    TileType type = GetRandomBoxTypeAvoidMatches(pos);
                    GameObject prefab = GetPrefabForType(type);
                    GameObject box = Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    AddBox(pos, box, type);
                }
            }
    }
}
