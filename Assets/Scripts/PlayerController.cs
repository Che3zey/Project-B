using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Vector2Int playerPos;
    public GridManager gridManager;

    [Header("Wobble Settings")]
    public float moveDuration = 0.1f;   // how long the move lerp lasts
    public float stretchAmount = 0.2f;  // how much the player stretches in movement direction
    public float squashAmount = 0.15f;  // how much it squashes on impact
    public float recoverDuration = 0.08f; // how fast it returns to normal

    private bool isMoving = false;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Prevent movement while paused or already moving
        if ((GameManager.Instance != null && GameManager.Instance.IsPaused) || isMoving)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int targetPos = playerPos + dir;

        // Goal check first
        if (targetPos == gridManager.goalZone)
        {
            StartCoroutine(MoveWithWobble(targetPos, dir));
            Debug.Log("🏆 Player reached the goal!");
            GameManager.Instance.LoadScene("WinScreen");
            return;
        }

        // Check bounds
        if (targetPos.x < 0 || targetPos.x >= gridManager.width ||
            targetPos.y < 0 || targetPos.y >= gridManager.height)
            return;

        TileType targetTile = gridManager.grid[targetPos.x, targetPos.y];

        if (targetTile == TileType.Empty)
        {
            StartCoroutine(MoveWithWobble(targetPos, dir));
        }
        else if (targetTile == TileType.RedBox || targetTile == TileType.BlueBox || targetTile == TileType.GreenBox)
        {
            Vector2Int boxTarget = targetPos + dir;

            if (boxTarget.x < 0 || boxTarget.x >= gridManager.width ||
                boxTarget.y < 0 || boxTarget.y >= gridManager.height)
                return;

            if (gridManager.MoveBox(targetPos, boxTarget))
            {
                StartCoroutine(MoveWithWobble(targetPos, dir));
            }
        }

        // Check matches
        gridManager.ClearMatches();
    }

    IEnumerator MoveWithWobble(Vector2Int newPos, Vector2Int dir)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x, newPos.y, 0f);
        float elapsed = 0f;

        // ---- Phase 1: stretch in direction of movement ----
        Vector3 stretchScale = originalScale;
        if (dir.x != 0)
            stretchScale = new Vector3(originalScale.x + stretchAmount, originalScale.y - stretchAmount, originalScale.z);
        else if (dir.y != 0)
            stretchScale = new Vector3(originalScale.x - stretchAmount, originalScale.y + stretchAmount, originalScale.z);

        transform.localScale = stretchScale;

        // ---- Phase 2: move toward target ----
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        playerPos = newPos;

        // ---- Phase 3: squash (impact) ----
        Vector3 squashScale = new Vector3(
            originalScale.x - squashAmount,
            originalScale.y + squashAmount,
            originalScale.z
        );
        transform.localScale = squashScale;

        // ---- Phase 4: recover to normal ----
        float recoverTime = 0f;
        while (recoverTime < recoverDuration)
        {
            recoverTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashScale, originalScale, recoverTime / recoverDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        isMoving = false;
    }
}
