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

        // Goal check
        if (targetPos == gridManager.goalZone)
        {
            MovePlayer(targetPos);
            Debug.Log("🏆 Player reached the goal!");
            return;
        }

        // Grid bounds check
        if (targetPos.x < 0 || targetPos.x >= gridManager.width ||
            targetPos.y < 0 || targetPos.y >= gridManager.height)
            return;

        TileType targetTile = gridManager.grid[targetPos.x, targetPos.y];

        if (targetTile == TileType.Empty)
        {
            MovePlayer(targetPos);
        }
        else if (targetTile == TileType.RedBox || targetTile == TileType.BlueBox || targetTile == TileType.GreenBox)
        {
            Vector2Int boxTarget = targetPos + dir;
            if (boxTarget.x < 0 || boxTarget.x >= gridManager.width ||
                boxTarget.y < 0 || boxTarget.y >= gridManager.height)
                return;

            if (gridManager.grid[boxTarget.x, boxTarget.y] == TileType.Empty)
            {
                gridManager.MoveBox(targetPos, boxTarget);
                MovePlayer(targetPos);
            }
        }

        // Check for match-3 after every move
        gridManager.ClearMatches();
    }

    void MovePlayer(Vector2Int newPos)
    {
        playerPos = newPos;
        transform.position = new Vector3(newPos.x, newPos.y, 0);
    }
}
