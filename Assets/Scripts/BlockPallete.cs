using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))] // <-- Нужен для обнаружения возврата блока
public class BlockPalette : MonoBehaviour
{
    [Header("Initial Data")]
    public List<GameObject> startingBlocks; // Стартовый набор (префабы)

    [Header("Visual Settings")]
    public Vector2 paletteSize = new Vector2(8f, 2f);
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Sprite backgroundSprite;
    public int sortingOrder = 50;

    public RectTransform uiCanvasRect;

    [Header("Layout Settings")]
    [Min(1)] public int visibleSlotsCount = 5;
    [Range(0f, 2f)] public float sidePadding = 0.5f;

    [Header("State")]
    public int currentStartIndex = 0;

    [Header("UI")]
    public Button LeftArrowButton;
    public Button RightArrowButton;

    // --- НОВОЕ: Динамический инвентарь (реальные объекты) ---
    // Мы храним здесь либо префабы, либо уже существующие инстансы, которые "спрятаны"
    private List<BlockController> inventory = new List<BlockController>();

    private List<Transform> generatedSlots = new List<Transform>();
    private BlockController[] visibleBlocks; // Те, что сейчас в слотах
    private SpriteRenderer sr;
    private BoxCollider2D boxCollider;

    private static BlockPalette _instance;
    public static BlockPalette Instance
    {
        get
        {
            // Если ссылка потерялась (после рекомпиляции), ищем объект на сцене
            if (_instance == null)
            {
                _instance = FindObjectOfType<BlockPalette>();
            }
            return _instance;
        }
    }

    private void OnValidate()
    {
        UpdateVisualsAndCollider();
    }

    private void Awake()
    {
        // При старте заполняем инвентарь из стартового списка
        foreach (var prefab in startingBlocks)
        {
            // Создаем скрытые копии для инвентаря
            CreateHiddenInventoryItem(prefab);
        }
    }

    private void Start()
    {
        UpdateVisualsAndCollider();
        GenerateSlotPositions();

        if (LeftArrowButton != null)
        {
            
            LeftArrowButton.onClick.AddListener(ScrollLeft);
        }
        if (RightArrowButton != null)
            RightArrowButton.onClick.AddListener(ScrollRight);

        visibleBlocks = new BlockController[visibleSlotsCount];
        RefreshPalette();
    }

    // Создает блок, настраивает его и прячет (добавляет в инвентарь)
    private void CreateHiddenInventoryItem(GameObject prefab)
    {
        GameObject newObj = Instantiate(prefab, transform);

        //newObj.transform.SetParent(transform.parent, false);

        BlockController bc = newObj.GetComponent<BlockController>();

        // Сразу настраиваем как элемент палитры
        bc.InitializeAsPaletteItem(this, -1);
        newObj.SetActive(false); // Прячем

        inventory.Add(bc);
    }

    private void UpdateVisualsAndCollider()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        if (backgroundSprite != null) sr.sprite = backgroundSprite;
        sr.color = backgroundColor;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = paletteSize;
        sr.sortingOrder = sortingOrder;

        // Настраиваем коллайдер под размер фона (чтобы ловить блоки)
        boxCollider.size = paletteSize;
        boxCollider.isTrigger = true; // Важно! Чтобы не толкаться

        if (uiCanvasRect != null)
        {
            // Берем текущий Scale канваса (который вы настроили, например 0.02)
            float currentScaleX = uiCanvasRect.localScale.x;
            float currentScaleY = uiCanvasRect.localScale.y;

            // Чтобы Канвас был размером с Палитру (в юнитах), 
            // его пиксельный размер должен быть: РазмерВЮнитах / Скейл
            // Например: 8 юнитов / 0.02 скейл = 400 пикселей ширины

            float width = paletteSize.x / currentScaleX;
            float height = paletteSize.y / currentScaleY;

            uiCanvasRect.sizeDelta = new Vector2(width, height);

            // Центрируем
            uiCanvasRect.localPosition = Vector3.zero;
        }
    }

    private void GenerateSlotPositions()
    {
        foreach (Transform child in generatedSlots) if (child != null) Destroy(child.gameObject);
        generatedSlots.Clear();

        float usableWidth = paletteSize.x - (sidePadding * 2);
        float sectionWidth = usableWidth / visibleSlotsCount;
        float startX = -(usableWidth / 2f);

        for (int i = 0; i < visibleSlotsCount; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(transform);
            float xPos = startX + (sectionWidth * i) + (sectionWidth / 2f);
            slotObj.transform.localPosition = new Vector3(xPos, 0, 0);
            generatedSlots.Add(slotObj.transform);
        }
    }

    public void ScrollLeft()
    {
        if (currentStartIndex > 0)
        {
            currentStartIndex--;
            RefreshPalette();
        }
    }

    public void ScrollRight()
    {
        if (currentStartIndex + visibleSlotsCount < inventory.Count)
        {
            currentStartIndex++;
            RefreshPalette();
        }
    }

    // Перерисовывает видимые блоки
    private void RefreshPalette()
    {
        // 1. Сначала убираем текущие видимые блоки обратно в "скрытый запас" (или просто отвязываем от слотов)
        // Но так как у нас список ссылок, мы просто деактивируем все, что не должно быть видно.

        foreach (var block in inventory)
        {
            if (block != null && block.isPaletteItem)
            {
                block.gameObject.SetActive(false);
                block.transform.SetParent(transform); // Возвращаем в корень палитры
            }
        }

        // 2. Активируем и расставляем нужные
        for (int i = 0; i < visibleSlotsCount; i++)
        {
            int inventoryIndex = currentStartIndex + i;

            if (inventoryIndex >= inventory.Count)
            {
                visibleBlocks[i] = null;
                continue;
            }

            BlockController block = inventory[inventoryIndex];
            Transform slot = generatedSlots[i];

            block.gameObject.SetActive(true);
            block.transform.SetParent(slot);
            block.transform.localPosition = new Vector3(0, 0, -1f);
            block.transform.localRotation = Quaternion.identity;

            // Обновляем визуальные слои (чтобы было поверх фона)
            UpdateSorting(block);

            // Обновляем индекс слота
            block.InitializeAsPaletteItem(this, i); // i - это визуальный индекс слота

            visibleBlocks[i] = block;
        }
    }

    private void UpdateSorting(BlockController block)
    {
        SpriteRenderer[] renderers = block.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.sortingOrder = sortingOrder + 10;

        TMP_Text[] texts = block.GetComponentsInChildren<TMP_Text>();
        foreach (var t in texts)
        {
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = sortingOrder + 11;
        }
    }

    // --- ЛОГИКА ИНВЕНТАРЯ ---

    // 1. Блок забрали (удалили из палитры)
    public void OnBlockTakenFromPalette(BlockController block)
    {
        if (inventory.Contains(block))
        {
            inventory.Remove(block);

            // Если мы забрали последний элемент и скролл был в конце, сдвигаем скролл
            if (currentStartIndex > 0 && currentStartIndex >= inventory.Count)
            {
                currentStartIndex--;
            }

            RefreshPalette();
        }
    }

    // 2. Блок вернули (добавили в палитру)
    public void ReturnBlockToPalette(BlockController block)
    {
        if (!inventory.Contains(block))
        {
            // Сбрасываем физику блока
            block.InitializeAsPaletteItem(this, -1);

            // Сбрасываем размер, если нужно (авто-сайз)
            //block.autoSizeToContent = true;

            inventory.Add(block);
            RefreshPalette();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(paletteSize.x, paletteSize.y, 0.1f));
    }
}