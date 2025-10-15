using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Vector2Int playerPos;
    public GridManager gridManager;

    [Header("Wobble Settings")]
    public float moveDuration = 0.1f;
    public float stretchAmount = 0.2f;
    public float squashAmount = 0.15f;
    public float recoverDuration = 0.08f;

    private bool isMoving = false;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
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
        if (!gridManager.IsInsideGrid(targetPos))
        {
            gridManager.PlaySound(gridManager.invalidClip);
            return;
        }

        TileType targetTile = gridManager.grid[targetPos.x, targetPos.y];

        if (targetTile == TileType.Empty)
        {
            gridManager.PlaySound(gridManager.moveClip);
            StartCoroutine(MoveWithWobble(targetPos, dir));
        }
        else if (targetTile == TileType.RedBox || targetTile == TileType.BlueBox || targetTile == TileType.GreenBox)
        {
            Vector2Int boxTarget = targetPos + dir;

            if (!gridManager.IsInsideGrid(boxTarget) || !gridManager.IsEmpty(boxTarget))
            {
                gridManager.PlaySound(gridManager.invalidClip);
                return;
            }

            if (gridManager.MoveBox(targetPos, boxTarget))
            {
                gridManager.PlaySound(gridManager.pushClip);
                StartCoroutine(MoveWithWobble(targetPos, dir));
            }
            else
            {
                gridManager.PlaySound(gridManager.invalidClip);
            }
        }

        // Check matches and play match sound
        int matchesBefore = gridManager.CheckMatches().Count;
        gridManager.ClearMatches();
        int matchesAfter = gridManager.CheckMatches().Count;
        if (matchesBefore > 0) gridManager.PlaySound(gridManager.matchClip);
    }

    IEnumerator MoveWithWobble(Vector2Int newPos, Vector2Int dir)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x, newPos.y, 0f);
        float elapsed = 0f;

        // Stretch in direction of movement
        Vector3 stretchScale = originalScale;
        if (dir.x != 0)
            stretchScale = new Vector3(originalScale.x + stretchAmount, originalScale.y - stretchAmount, originalScale.z);
        else if (dir.y != 0)
            stretchScale = new Vector3(originalScale.x - stretchAmount, originalScale.y + stretchAmount, originalScale.z);

        transform.localScale = stretchScale;

        // Move toward target
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        playerPos = newPos;

        // Squash (impact)
        Vector3 squashScale = new Vector3(
            originalScale.x - squashAmount,
            originalScale.y + squashAmount,
            originalScale.z
        );
        transform.localScale = squashScale;

        // Recover to normal
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
