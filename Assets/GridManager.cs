using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public TileType[,] grid;

    // Track boxes by position
    private Dictionary<Vector2Int, GameObject> boxDict = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Empty;
    }

    public bool IsEmpty(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;
        return grid[pos.x, pos.y] == TileType.Empty;
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

    // Update box position in dictionary
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
}
