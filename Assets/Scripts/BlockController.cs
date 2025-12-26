using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase] // <--- 1. Теперь при клике на спрайт будет выделяться сам Блок
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class BlockController : MonoBehaviour
{
    [Header("Core")]
    public string hashIndex = "#0000";

    #region Structures
    [Header("Structures")]
    public Transform structuresRoot;
    public int selectedStructureIndex = 0;

    [SerializeField, HideInInspector]
    private List<GameObject> structures = new List<GameObject>();
    #endregion

    #region Grid & Size
    [Header("Grid Settings")]
    [Tooltip("Если ВКЛ - размер вычисляется по спрайтам.")]
    public bool autoSizeToContent = true;

    [Tooltip("Size of the block in grid cells")]
    public Vector2Int sizeInCells = new Vector2Int(1, 1);

    [Tooltip("If true, object snaps to 'Grid Position'.")]
    public bool useManualGridPosition;
    public Vector2Int gridPosition;
    #endregion

    #region RunTime
    private BoxCollider2D collider2D;
    private Rigidbody2D rb2D;
    private Vector3 lastPosition;

    // Переменные для перетаскивания
    private bool isDragging = false;
    private Vector3 dragOffset;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        FullRefresh();
    }

    private void OnEnable()
    {
        InitializeComponents();
        FullRefresh();
    }

    private void Update()
    {
        // 1. ЛОГИКА В РЕДАКТОРЕ
        if (!Application.isPlaying)
        {
            if (transform.position != lastPosition && !useManualGridPosition)
            {
                UpdateGridPositionFromWorld();
            }

            if (autoSizeToContent) ResizeSizeInCellsToFitContent();

            SnapToGrid();
            ResizeCollider();

            // <--- 2. Жестко держим структуру в центре
            EnforceStructurePosition();
        }
        // 2. ЛОГИКА В ИГРЕ (PLAY MODE)
        else
        {
            if (isDragging)
            {
                DragBlock();
            }
            else
            {
                SnapToGrid();
            }
        }
    }

    private void OnValidate()
    {
        InitializeComponents();
        RefreshStructures();
        ApplyStructureVisibility();

        if (autoSizeToContent) ResizeSizeInCellsToFitContent();

        ResizeCollider();
        SnapToGrid();
        EnforceStructurePosition();
    }
    #endregion

    #region Position Locking (NEW)

    // Этот метод принудительно ставит визуал в ноль
    private void EnforceStructurePosition()
    {
        if (structuresRoot != null)
        {
            // Возвращаем контейнер структур в центр блока
            if (structuresRoot.localPosition != Vector3.zero)
                structuresRoot.localPosition = Vector3.zero;

            // Возвращаем каждую структуру в центр контейнера
            foreach (var st in structures)
            {
                if (st != null && st.transform.localPosition != Vector3.zero)
                {
                    st.transform.localPosition = Vector3.zero;
                }
            }
        }
    }
    #endregion

    #region Drag & Drop Logic

    private void OnMouseDown()
    {
        if (!Application.isPlaying) return;

        isDragging = true;
        Vector3 mousePos = GetMouseWorldPos();
        dragOffset = transform.position - mousePos;
    }

    private void OnMouseUp()
    {
        if (!Application.isPlaying) return;

        isDragging = false;
        UpdateGridPositionFromWorld();
        SnapToGrid();
    }

    private void DragBlock()
    {
        Vector3 mousePos = GetMouseWorldPos();
        transform.position = mousePos + dragOffset;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    #endregion

    private void InitializeComponents()
    {
        if (!collider2D) collider2D = GetComponent<BoxCollider2D>();
        if (!collider2D) collider2D = gameObject.AddComponent<BoxCollider2D>();
        collider2D.isTrigger = true;

        if (!rb2D) rb2D = GetComponent<Rigidbody2D>();
        if (!rb2D)
        {
            rb2D = gameObject.AddComponent<Rigidbody2D>();
            rb2D.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    #region Main Logic

    private void FullRefresh()
    {
        RefreshStructures();
        ApplyStructureVisibility();
        if (autoSizeToContent) ResizeSizeInCellsToFitContent();
        ResizeCollider();
        SnapToGrid();
        EnforceStructurePosition();
    }

    private void ResizeCollider()
    {
        if (LevelController.Instance == null || collider2D == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 finalSize = new Vector2(sizeInCells.x * modSize.x, sizeInCells.y * modSize.y);
        if (collider2D.size != finalSize) collider2D.size = finalSize;
    }

    private void SnapToGrid()
    {
        if (LevelController.Instance == null) return;

        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 origin = LevelController.Instance.CurrentGridOrigin;

        float halfBlockWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfBlockHeight = (sizeInCells.y * modSize.y) * 0.5f;

        Vector2 targetPos = origin +
                            new Vector2(gridPosition.x * modSize.x, gridPosition.y * modSize.y) +
                            new Vector2(halfBlockWidth, halfBlockHeight);

        if (!isDragging)
        {
            if (Vector3.Distance(transform.position, targetPos) > 0.001f)
            {
                transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
                lastPosition = transform.position;
            }
        }
    }

    private void UpdateGridPositionFromWorld()
    {
        if (LevelController.Instance == null) return;

        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        float halfBlockWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfBlockHeight = (sizeInCells.y * modSize.y) * 0.5f;

        Vector2 bottomLeftCorner = (Vector2)transform.position - new Vector2(halfBlockWidth, halfBlockHeight);

        gridPosition = LevelController.Instance.WorldToGrid(bottomLeftCorner);
    }

    #endregion

    #region Auto-Calculate Size

    private void ResizeSizeInCellsToFitContent()
    {
        if (structures.Count == 0 || LevelController.Instance == null) return;
        if (!autoSizeToContent) return;

        if (selectedStructureIndex >= structures.Count) selectedStructureIndex = 0;
        GameObject active = structures[selectedStructureIndex];
        if (!active) return;

        Bounds bounds = CalculateBounds(active);
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;

        if (modSize.x <= 0.001f || modSize.y <= 0.001f) return;

        int requiredX = Mathf.RoundToInt(bounds.size.x / modSize.x);
        int requiredY = Mathf.RoundToInt(bounds.size.y / modSize.y);

        requiredX = Mathf.Max(1, requiredX);
        requiredY = Mathf.Max(1, requiredY);

        if (sizeInCells.x != requiredX || sizeInCells.y != requiredY)
            sizeInCells = new Vector2Int(requiredX, requiredY);
    }

    private Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    #endregion

    #region Structure Management
    private void RefreshStructures()
    {
        structures.Clear();
        if (structuresRoot == null) return;
        foreach (Transform child in structuresRoot) structures.Add(child.gameObject);
        selectedStructureIndex = Mathf.Clamp(selectedStructureIndex, 0, Mathf.Max(0, structures.Count - 1));
    }

    private void ApplyStructureVisibility()
    {
        for (int i = 0; i < structures.Count; i++)
        {
            if (structures[i] != null) structures[i].SetActive(i == selectedStructureIndex);
        }
    }

    [ContextMenu("Create Structure")]
    public void CreateStructure()
    {
        if (structuresRoot == null)
        {
            Debug.LogWarning("StructuresRoot not assigned!");
            return;
        }
        GameObject newStruct = new GameObject($"Structure_{structures.Count}");
        newStruct.transform.SetParent(structuresRoot);
        newStruct.transform.localPosition = Vector3.zero; // <--- При создании тоже ставим в ноль
        structures.Add(newStruct);
        selectedStructureIndex = structures.Count - 1;
        FullRefresh();
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (collider2D != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(collider2D.bounds.center, collider2D.bounds.size);
        }
    }
}