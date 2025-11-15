using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class EmptyController : MonoBehaviour
{
    [Tooltip("Hash index like #1234 — used to validate incoming blocks")]
    public string hashIndex = "#0000";

    [Header("Grid position (optional)")]
    public bool useManualGridPosition = false;
    public Vector2Int gridPosition = Vector2Int.zero;

    private BoxCollider2D bc2d;

    private void Reset()
    {
        bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;
    }

    private void Awake()
    {
        bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;

        SnapIfManual();
    }

    private void OnValidate()
    {
        bc2d = GetComponent<BoxCollider2D>();
        if (bc2d != null && LevelController.Instance != null)
        {
            bc2d.size = LevelController.Instance.cellSize;
            bc2d.isTrigger = true;
        }
        SnapIfManual();
    }

    private void SnapIfManual()
    {
        if (useManualGridPosition && LevelController.Instance != null)
        {
            transform.position = LevelController.Instance.GridToWorld(gridPosition);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var block = collision.GetComponent<BlockController>();
        if (block != null)
        {
            bool match = string.Equals(hashIndex?.Trim(), block.hashIndex?.Trim(), System.StringComparison.OrdinalIgnoreCase);
            Debug.Log($"[EmptyController] Cell {name} (hash {hashIndex}) got Block {block.name} (hash {block.hashIndex}). Match: {match}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var block = collision.GetComponent<BlockController>();
        if (block != null)
        {
            Debug.Log($"[EmptyController] Block {block.name} exited cell {name}");
        }
    }
}