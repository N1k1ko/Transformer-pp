using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EmptyController : MonoBehaviour
{
    [Tooltip("Hash index like #1234 — used to validate incoming blocks")]
    public string hashIndex = "#0000";

    [Header("Grid Settings")]
    public Vector2Int sizeInCells = new Vector2Int(1, 1);
    [Range(0f, 0.5f)] public float colliderPadding = 0.05f;

    [Header("Grid Position")]
    public bool useManualGridPosition = false;
    public Vector2Int gridPosition;// = Vector2Int.zero;

    private BoxCollider2D bc2d;
    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;

    private void Awake() => Initialize();
    private void OnEnable() { Initialize(); FullRefresh(); }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (Vector3.Distance(transform.position, lastPosition) > 0.001f && !useManualGridPosition)
                UpdateGridPositionFromWorld();

            SnapToGrid();
            ResizeVisuals();
            ResizeCollider();
        }
    }

    private void OnValidate() { Initialize(); FullRefresh(); }

    private void Initialize()
    {
        if (!bc2d) bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FullRefresh()
    {
        ResizeVisuals();
        ResizeCollider();
        SnapToGrid();
    }

    // --- Логику столкновений обрабатывает BlockController, 
    // но если вам нужно что-то специфичное для слота, пишите сюда. ---

    #region Grid Logic (Same as before)
    private void ResizeVisuals()
    {
        if (LevelController.Instance == null || spriteRenderer == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 targetSize = new Vector2(sizeInCells.x * modSize.x, sizeInCells.y * modSize.y);

        if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
        {
            if (spriteRenderer.sprite == null) return;
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x == 0 || spriteSize.y == 0) return;
            transform.localScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, 1f);
        }
        else
        {
            spriteRenderer.size = targetSize;
            transform.localScale = Vector3.one;
        }
    }

    private void ResizeCollider()
    {
        if (LevelController.Instance == null || bc2d == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 desiredWorldSize = new Vector2(sizeInCells.x * modSize.x, sizeInCells.y * modSize.y);

        float padX = Mathf.Min(colliderPadding * 2, desiredWorldSize.x - 0.1f);
        float padY = Mathf.Min(colliderPadding * 2, desiredWorldSize.y - 0.1f);
        desiredWorldSize -= new Vector2(padX, padY);

        Vector3 currentScale = transform.localScale;
        if (currentScale.x == 0 || currentScale.y == 0) return;

        Vector2 localColliderSize = new Vector2(desiredWorldSize.x / currentScale.x, desiredWorldSize.y / currentScale.y);

        if (bc2d.size != localColliderSize) bc2d.size = localColliderSize;
    }

    private void SnapToGrid()
    {
        if (LevelController.Instance == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 origin = LevelController.Instance.CurrentGridOrigin;

        float halfWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfHeight = (sizeInCells.y * modSize.y) * 0.5f;

        Vector2 targetPos = origin +
                            new Vector2(gridPosition.x * modSize.x, gridPosition.y * modSize.y) +
                            new Vector2(halfWidth, halfHeight);

        if (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            lastPosition = transform.position;
        }
    }

    private void UpdateGridPositionFromWorld()
    {
        if (LevelController.Instance == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        float halfWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfHeight = (sizeInCells.y * modSize.y) * 0.5f;
        Vector2 bottomLeft = (Vector2)transform.position - new Vector2(halfWidth, halfHeight);
        var tmp = gridPosition;
        gridPosition = LevelController.Instance.WorldToGrid(bottomLeft);
        if(Vector2.Distance(tmp, gridPosition) > 0.01f)
            EditorUtility.SetDirty(this);
    }
    #endregion
}