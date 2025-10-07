using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Vector2Int playerPos;
    public GridManager gridManager;

    void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
    }

    void Update()
    {
        if (gridManager == null || gridManager.grid == null) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int targetPos = playerPos + dir;

        if (!IsInsideGrid(targetPos)) return;

        TileType targetTile = gridManager.grid[targetPos.x, targetPos.y];

        if (targetTile == TileType.Empty || targetTile == TileType.Exit)
        {
            MovePlayer(targetPos);
            gridManager.CheckMatches();
        }
        else if (targetTile == TileType.Box)
        {
            Vector2Int boxTarget = targetPos + dir;

            if (!IsInsideGrid(boxTarget)) return;

            if (gridManager.grid[boxTarget.x, boxTarget.y] == TileType.Empty)
            {
                gridManager.MoveBox(targetPos, boxTarget);
                MovePlayer(targetPos);
                gridManager.CheckMatches();
            }
        }
    }

    void MovePlayer(Vector2Int newPos)
    {
        playerPos = newPos;
        transform.position = new Vector3(newPos.x, newPos.y, 0);
    }

    private bool IsInsideGrid(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridManager.width ||
            pos.y < 0 || pos.y >= gridManager.height)
            return false;
        return true;
    }
}
