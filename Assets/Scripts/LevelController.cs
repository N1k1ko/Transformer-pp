using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class LevelController : MonoBehaviour
{
    // --- ИЗМЕНЕНИЕ ЗДЕСЬ: Ленивая инициализация ---
    private static LevelController _instance;
    public static LevelController Instance
    {
        get
        {
            // Если ссылка потерялась (после рекомпиляции), ищем объект на сцене
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelController>();
            }
            return _instance;
        }
    }
    // ----------------------------------------------

    [Header("Режим работы")]
    public bool AutomaticGrid;
    public bool showFrame = true;

    [Header("Настройки Авто-сетки (по экрану)")]
    [Min(1)] public int columns = 5;
    [Min(1)] public int rows = 3;

    #region Manual Module settings
    [Header("Настройки Ручной сетки")]
    public Vector2 moduleSize = new Vector2(1f, 1f);
    public Vector2 gridOrigin = Vector2.zero;
    public Vector2Int gridSize = new Vector2Int(10, 10);
    #endregion

    #region Grid visuals (Editor only)
    [Header("Визуал (Только редактор)")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    public Color fillColor = new Color(1f, 1f, 1f, 0.05f);
    [Range(1f, 10f)]
    public float lineThickness = 2f;
    #endregion

    #region Singleton Logic
    private void Awake()
    {
        SetupSingleton();
    }

    // OnEnable вызывается Unity после каждой пересборки скриптов!
    private void OnEnable()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Application.isPlaying)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        else
        {
            // В режиме редактора просто обновляем ссылку
            _instance = this;
        }
    }
    #endregion

    #region Dynamic Properties
    public Vector2 CurrentModuleSize
    {
        get
        {
            if (AutomaticGrid)
            {
                Camera cam = Camera.main;
                if (cam == null) return moduleSize;

                float camHeight = 2f * cam.orthographicSize;
                float camWidth = camHeight * cam.aspect;
                // Защита от деления на ноль при инициализации
                if (columns == 0) columns = 1;
                if (rows == 0) rows = 1;

                return new Vector2(camWidth / columns, camHeight / rows);
            }
            return moduleSize;
        }
    }

    public Vector2 CurrentGridOrigin
    {
        get
        {
            if (AutomaticGrid)
            {
                Camera cam = Camera.main;
                if (cam == null) return gridOrigin;

                float camHeight = 2f * cam.orthographicSize;
                float camWidth = camHeight * cam.aspect;
                Vector3 camBottomLeft = cam.transform.position - new Vector3(camWidth / 2f, camHeight / 2f, 0);
                return (Vector2)camBottomLeft;
            }
            return gridOrigin;
        }
    }

    public Vector2Int CurrentGridSize
    {
        get
        {
            if (AutomaticGrid) return new Vector2Int(columns, rows);
            return gridSize;
        }
    }
    #endregion

    #region Grid math
    public Vector2 GridToWorld(Vector2Int gridCoords)
    {
        return CurrentGridOrigin +
               new Vector2(
                   (gridCoords.x + 0.5f) * CurrentModuleSize.x,
                   (gridCoords.y + 0.5f) * CurrentModuleSize.y
               );
    }

    public Vector2 SnapToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - CurrentGridOrigin;
        Vector2 size = CurrentModuleSize;
        Vector2Int dim = CurrentGridSize;

        if (size.x <= 0.0001f || size.y <= 0.0001f) return worldPos;

        int col = Mathf.FloorToInt(local.x / size.x);
        int row = Mathf.FloorToInt(local.y / size.y);

        col = Mathf.Clamp(col, 0, dim.x - 1);
        row = Mathf.Clamp(row, 0, dim.y - 1);

        return CurrentGridOrigin + new Vector2((col + 0.5f) * size.x, (row + 0.5f) * size.y);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector2 local = worldPos - CurrentGridOrigin;
        Vector2 size = CurrentModuleSize;

        if (size.x <= 0.0001f || size.y <= 0.0001f) return Vector2Int.zero;

        int col = Mathf.FloorToInt(local.x / size.x);
        int row = Mathf.FloorToInt(local.y / size.y);

        return new Vector2Int(col, row);
    }
    #endregion

    #region Editor grid drawing
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // ВАЖНО: Принудительная перерисовка сцены, если что-то изменилось
        // Это помогает Gizmos обновляться плавно
        if (!Application.isPlaying)
        {
            // Иногда полезно, но может нагружать процессор. 
            // Оставьте закомментированным, если тормозит.
            // EditorUtility.SetDirty(this); 
        }

        Vector2 origin = CurrentGridOrigin;
        Vector2 modSize = CurrentModuleSize;
        Vector2Int dim = CurrentGridSize;

        Vector2 totalSize = new Vector2(dim.x * modSize.x, dim.y * modSize.y);

        if (!AutomaticGrid)
        {
            Gizmos.color = fillColor;
            Gizmos.DrawCube(origin + totalSize / 2f, new Vector3(totalSize.x, totalSize.y, 1f));
        }

        Handles.color = lineColor;
        float zPos = 0f;

        int startX = showFrame ? 0 : 1;
        int endX = showFrame ? dim.x : dim.x - 1;

        for (int i = startX; i <= endX; i++)
        {
            float xPos = origin.x + (i * modSize.x);
            Vector3 p1 = new Vector3(xPos, origin.y, zPos);
            Vector3 p2 = new Vector3(xPos, origin.y + totalSize.y, zPos);
            Handles.DrawAAPolyLine(lineThickness, p1, p2);
        }

        int startY = showFrame ? 0 : 1;
        int endY = showFrame ? dim.y : dim.y - 1;

        for (int j = startY; j <= endY; j++)
        {
            float yPos = origin.y + (j * modSize.y);
            Vector3 p1 = new Vector3(origin.x, yPos, zPos);
            Vector3 p2 = new Vector3(origin.x + totalSize.x, yPos, zPos);
            Handles.DrawAAPolyLine(lineThickness, p1, p2);
        }
#endif
    }
    #endregion

    public bool Verify()
    {
        var result = true;


        var tmp = FindObjectsByType<EmptyController>(FindObjectsSortMode.None);

        foreach (var e in tmp) {
            if (e.hashIndex == "")
                continue;
            if (!e.IsOccupied || !e.hashIndex.Equals(e.linkedBlock.hashIndex))
            {
                result = false;
                break;
            }
        }

        return result;

    }



}