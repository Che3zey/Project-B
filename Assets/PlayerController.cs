using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Vector2Int playerPos;
    public GridManager gridManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int targetPos = playerPos + dir;
        TileType targetTile = gridManager.grid[targetPos.x, targetPos.y];

        if (targetTile == TileType.Empty || targetTile == TileType.Exit)
        {
            MovePlayer(targetPos);
        }
        else if (targetTile == TileType.Box)
        {
            // Try to push box
            Vector2Int boxTarget = targetPos + dir;
            if (gridManager.grid[boxTarget.x, boxTarget.y] == TileType.Empty)
            {
                MoveBox(targetPos, boxTarget);
                MovePlayer(targetPos);
            }
        }
    }

    void MovePlayer(Vector2Int newPos)
    {
        playerPos = newPos;
        transform.position = new Vector3(newPos.x, newPos.y, 0);
    }

    void MoveBox(Vector2Int currentPos, Vector2Int newPos)
    {
        gridManager.grid[newPos.x, newPos.y] = TileType.Box;
        gridManager.grid[currentPos.x, currentPos.y] = TileType.Empty;
        
        // Move box GameObject
        GameObject boxGO = gridManager.GetBoxAtPosition(currentPos);
        boxGO.transform.position = new Vector3(newPos.x, newPos.y, 0);
    }
}
