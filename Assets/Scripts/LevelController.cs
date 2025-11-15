using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [Header("Grid settings")]
    [Tooltip("Size of one grid cell in world units")]
    public Vector2 cellSize = new Vector2(1f, 1f);

    [Tooltip("World position of grid origin (cell 0,0 center)")]
    public Vector2 gridOrigin = Vector2.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Snap world position to nearest grid cell center.
    /// </summary>
    public Vector2 SnapToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - gridOrigin;
        int col = Mathf.RoundToInt(local.x / cellSize.x);
        int row = Mathf.RoundToInt(local.y / cellSize.y);
        return gridOrigin + new Vector2(col * cellSize.x, row * cellSize.y);
    }

    /// <summary>
    /// Convert grid coordinates (col,row) to world position (cell center).
    /// </summary>
    public Vector2 GridToWorld(Vector2Int gridCoords)
    {
        return gridOrigin + new Vector2(gridCoords.x * cellSize.x, gridCoords.y * cellSize.y);
    }
}
