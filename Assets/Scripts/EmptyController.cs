using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class EmptyController : MonoBehaviour
{
    [Tooltip("Hash index like #1234 — used to validate incoming blocks")]
    public string hashIndex = "#0000";

    [Header("Grid position")]
    public bool useManualGridPosition = false;
    public Vector2Int gridPosition = Vector2Int.zero;

    private BoxCollider2D bc2d;
    private Vector3 lastPosition;

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        SnapToGrid();
    }

    private void Update()
    {
        // В редакторе обновляем логику, чтобы слот "ездил" за сеткой или мышкой
        if (!Application.isPlaying)
        {
            // Если двигаем слот инструментом Move Tool -> обновляем координаты
            if (transform.position != lastPosition && !useManualGridPosition)
            {
                UpdateGridPositionFromWorld();
            }

            SnapToGrid();
            ResizeCollider();
        }
    }

    private void OnValidate()
    {
        Initialize();
        SnapToGrid();
        ResizeCollider();
    }
    #endregion

    private void Initialize()
    {
        if (!bc2d) bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;
    }

    #region Grid Logic

    private void ResizeCollider()
    {
        if (LevelController.Instance == null || bc2d == null) return;

        // Берем актуальный размер клетки (Авто или Ручной)
        Vector2 currentSize = LevelController.Instance.CurrentModuleSize;

        if (bc2d.size != currentSize)
        {
            bc2d.size = currentSize;
        }
    }

    private void SnapToGrid()
    {
        if (LevelController.Instance == null) return;

        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 origin = LevelController.Instance.CurrentGridOrigin;

        // Центр клетки = Начало + (Коорд * Размер) + (Половина размера)
        Vector2 targetPos = origin +
                            new Vector2(gridPosition.x * modSize.x, gridPosition.y * modSize.y) +
                            (modSize * 0.5f);

        // Применяем позицию
        if (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            lastPosition = transform.position;
        }
    }

    private void UpdateGridPositionFromWorld()
    {
        if (LevelController.Instance == null) return;

        // Чтобы найти координаты, нужно от центра слота отнять половину размера, 
        // чтобы получить левый нижний угол (так работает WorldToGrid)
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 bottomLeft = (Vector2)transform.position - (modSize * 0.5f);

        gridPosition = LevelController.Instance.WorldToGrid(bottomLeft);
    }

    #endregion

    #region Triggers
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Логика проверки совпадения (без изменений)
        var block = collision.GetComponent<BlockController>();
        if (block != null)
        {
            bool match = string.Equals(hashIndex?.Trim(), block.hashIndex?.Trim(), System.StringComparison.OrdinalIgnoreCase);
            Debug.Log($"[EmptyController] Cell {name} (hash {hashIndex}) <-> Block {block.name}. Match: {match}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var block = collision.GetComponent<BlockController>();
        if (block != null)
        {
            // Debug.Log($"[EmptyController] Block {block.name} exited cell {name}");
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        // Рисуем красную рамку для слота, чтобы отличать от блоков
        if (bc2d != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireCube(bc2d.bounds.center, bc2d.bounds.size * 0.9f);
        }
    }
}