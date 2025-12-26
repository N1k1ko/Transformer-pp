using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Нужен для сортировки слотов по дистанции
using static Unity.Burst.Intrinsics.X86.Avx;


#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase]
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

    // Переменные для перетаскивания и логики возврата
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector3 startDragPosition; // <--- Где блок был до начала перетаскивания

    // Список слотов, с которыми мы сейчас соприкасаемся
    private List<EmptyController> overlappingSlots = new List<EmptyController>();
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
        // 1. ЛОГИКА В РЕДАКТОРЕ (Без изменений: прилипаем к сетке)
        if (!Application.isPlaying)
        {
            if (transform.position != lastPosition && !useManualGridPosition)
            {
                UpdateGridPositionFromWorld();
            }

            if (autoSizeToContent) ResizeSizeInCellsToFitContent();

            SnapToGridEditor(); // Используем упрощенную логику для редактора
            ResizeCollider();
            EnforceStructurePosition();
        }
        // 2. ЛОГИКА В ИГРЕ (PLAY MODE)
        else
        {
            if (isDragging)
            {
                DragBlock();
            }
            // Если не тащим, ничего не делаем, блок стоит там, куда его поставили
        }
    }

    private void OnValidate()
    {
        InitializeComponents();
        RefreshStructures();
        ApplyStructureVisibility();

        if (autoSizeToContent) ResizeSizeInCellsToFitContent();

        ResizeCollider();
        SnapToGridEditor();
        EnforceStructurePosition();
    }
    #endregion

    #region Interaction & Logic (NEW)

    private void OnMouseDown()
    {
        if (!Application.isPlaying) return;

        isDragging = true;

        // 1. Запоминаем, где стояли, чтобы вернуться в случае неудачи
        startDragPosition = transform.position;

        // Очищаем список слотов перед началом нового движения
        overlappingSlots.Clear();

        Vector3 mousePos = GetMouseWorldPos();
        dragOffset = transform.position - mousePos;
    }

    private void OnMouseUp()
    {
        if (!Application.isPlaying) return;

        isDragging = false;

        // ЛОГИКА ПРИЛИПАНИЯ К СЛОТУ
        EmptyController bestSlot = GetNearestValidSlot();

        if (bestSlot != null)
        {
            // Слот найден! Прилипаем к нему
            transform.position = bestSlot.transform.position;

            // Обновляем нашу внутреннюю координату сетки, чтобы она совпадала со слотом
            gridPosition = bestSlot.gridPosition;

            Debug.Log($"<color=green>[SUCCESS]</color> Block snapped to Slot {bestSlot.name}");
        }
        else
        {
            // Слот не найден! Возвращаемся домой
            transform.position = startDragPosition;
            Debug.Log($"<color=yellow>[RETURN]</color> No valid slot found. Returning to start.");
        }
    }

    // Ищем ближайший слот из тех, с которыми соприкасаемся
    private EmptyController GetNearestValidSlot()
    {
        // Удаляем null (если слот был уничтожен)
        overlappingSlots.RemoveAll(s => s == null);

        if (overlappingSlots.Count == 0) return null;

        EmptyController nearest = null;
        float minDistance = float.MaxValue;

        foreach (var slot in overlappingSlots)
        {
            float dist = Vector3.Distance(transform.position, slot.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = slot;
            }
        }
        return nearest;
    }

    // Входим в триггер слота
    private void OnTriggerEnter2D(Collider2D other)
    {
        var slot = other.GetComponent<EmptyController>();
        if (slot != null)
        {
            if (!overlappingSlots.Contains(slot))
            {
                overlappingSlots.Add(slot);

                // --- ЛОГ СООБЩЕНИЯ ---
                Debug.Log($"[INTERACTION] Block: <b>{this.hashIndex}</b> <--> Slot: <b>{slot.hashIndex}</b>");
            }
        }
    }

    // Выходим из триггера слота
    private void OnTriggerExit2D(Collider2D other)
    {
        var slot = other.GetComponent<EmptyController>();
        if (slot != null)
        {
            if (overlappingSlots.Contains(slot))
            {
                overlappingSlots.Remove(slot);
            }
        }
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
        SnapToGridEditor();
        EnforceStructurePosition();
    }

    private void ResizeCollider()
    {
        if (LevelController.Instance == null || collider2D == null) return;
        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 finalSize = new Vector2(sizeInCells.x * modSize.x, sizeInCells.y * modSize.y);
        if (collider2D.size != finalSize) collider2D.size = finalSize;
    }

    private void EnforceStructurePosition()
    {
        if (structuresRoot != null)
        {
            if (structuresRoot.localPosition != Vector3.zero) structuresRoot.localPosition = Vector3.zero;
            foreach (var st in structures)
            {
                if (st != null && st.transform.localPosition != Vector3.zero) st.transform.localPosition = Vector3.zero;
            }
        }
    }

    // Логика прилипания ТОЛЬКО для редактора
    private void SnapToGridEditor()
    {
        if (LevelController.Instance == null) return;

        Vector2 modSize = LevelController.Instance.CurrentModuleSize;
        Vector2 origin = LevelController.Instance.CurrentGridOrigin;

        float halfBlockWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfBlockHeight = (sizeInCells.y * modSize.y) * 0.5f;

        Vector2 targetPos = origin +
                            new Vector2(gridPosition.x * modSize.x, gridPosition.y * modSize.y) +
                            new Vector2(halfBlockWidth, halfBlockHeight);

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
        float halfBlockWidth = (sizeInCells.x * modSize.x) * 0.5f;
        float halfBlockHeight = (sizeInCells.y * modSize.y) * 0.5f;

        Vector2 bottomLeftCorner = (Vector2)transform.position - new Vector2(halfBlockWidth, halfBlockHeight);
        var tmp = gridPosition;
        gridPosition = LevelController.Instance.WorldToGrid(bottomLeftCorner);
        if (Vector2.Distance(tmp, gridPosition) > 0.01f)
            EditorUtility.SetDirty(this);
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
        if (structuresRoot == null) return;
        GameObject newStruct = new GameObject($"Structure_{structures.Count}");
        newStruct.transform.SetParent(structuresRoot);
        newStruct.transform.localPosition = Vector3.zero;
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