using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

#region Module settings
    [Header("Module settings")]
    [Tooltip("Physical size of ONE module (cell) in Unity units")]
    public Vector2 moduleSize = new Vector2(1f, 1f);
#endregion

#region Grid settings
    [Header("Grid layout")]
    [Tooltip("World position of grid origin (bottom-left corner)")]
    public Vector2 gridOrigin = Vector2.zero;

    [Tooltip("Grid size in modules (columns X, rows Y)")]
    public Vector2Int gridSize = new Vector2Int(10, 10);
#endregion

#region Grid visuals (Editor only)
    [Header("Grid visuals (Editor only)")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    public Color fillColor = new Color(1f, 1f, 1f, 0.05f);
    [Range(0.01f, 0.2f)]
    public float lineThickness = 0.05f;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        else
        {
            Instance = this;
        }
    }
#endregion

#region Grid math
    /// <summary>
    /// Converts grid coordinates (col,row) to world position (cell center).
    /// </summary>
    public Vector2 GridToWorld(Vector2Int gridCoords)
    {
        return gridOrigin +
               new Vector2(
                   (gridCoords.x + 0.5f) * moduleSize.x,
                   (gridCoords.y + 0.5f) * moduleSize.y
               );
    }

    /// <summary>
    /// Snaps any world position to nearest module center.
    /// </summary>
    public Vector2 SnapToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - gridOrigin;

        int col = Mathf.RoundToInt(local.x / moduleSize.x);
        int row = Mathf.RoundToInt(local.y / moduleSize.y);

        col = Mathf.Clamp(col, 0, gridSize.x - 1);
        row = Mathf.Clamp(row, 0, gridSize.y - 1);

        return GridToWorld(new Vector2Int(col, row));
    }

    /// <summary>
    /// Returns grid coordinates from world position.
    /// </summary>
    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - gridOrigin;

        int col = Mathf.FloorToInt(local.x / moduleSize.x);
        int row = Mathf.FloorToInt(local.y / moduleSize.y);

        return new Vector2Int(col, row);
    }
#endregion

#region Editor grid drawing
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        DrawGrid();
    }

    private void DrawGrid()
    {
        Vector2 totalSize = new Vector2(
            gridSize.x * moduleSize.x,
            gridSize.y * moduleSize.y
        );

        // Fill
        Gizmos.color = fillColor;
        Gizmos.DrawCube(
            gridOrigin + totalSize / 2f,
            new Vector3(totalSize.x, totalSize.y, 0.01f)
        );

        // Lines
        Gizmos.color = lineColor;

        for (int x = 0; x <= gridSize.x; x++)
        {
            Vector3 from = gridOrigin + new Vector2(x * moduleSize.x, 0);
            Vector3 to   = gridOrigin + new Vector2(x * moduleSize.x, totalSize.y);
            DrawThickLine(from, to, lineThickness);
        }

        for (int y = 0; y <= gridSize.y; y++)
        {
            Vector3 from = gridOrigin + new Vector2(0, y * moduleSize.y);
            Vector3 to   = gridOrigin + new Vector2(totalSize.x, y * moduleSize.y);
            DrawThickLine(from, to, lineThickness);
        }
    }

    private void DrawThickLine(Vector3 a, Vector3 b, float thickness)
    {
        Vector3 center = (a + b) * 0.5f;
        float length = Vector3.Distance(a, b);

        bool vertical = Mathf.Abs(a.x - b.x) < 0.001f;

        Vector3 size = vertical
            ? new Vector3(thickness, length, 0.01f)
            : new Vector3(length, thickness, 0.01f);

        Gizmos.DrawCube(center, size);
    }
#endregion
}