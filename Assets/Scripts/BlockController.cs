using UnityEngine;
using System;
using UnityEngine.EventSystems;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class BlockController : MonoBehaviour
{
    public enum StructureType
    {
        TextInput,
        FormulaInput
    }

    [Header("Core")]
    [Tooltip("Choose internal structure (displayed child will be enabled).")]
    public StructureType structure = StructureType.TextInput;

    [Tooltip("Hash index like #1234 used to validate against an EmptyCell.")]
    public string hashIndex = "#0000";

    [Header("Grid placement")]
    [Tooltip("Size in grid cells (width x height)")]
    public Vector2Int sizeInCells = new Vector2Int(1, 1);

    [Tooltip("If true — block position will be taken from gridPosition")]
    public bool useManualGridPosition = false;
    public Vector2Int gridPosition = Vector2Int.zero; // column, row (integers)

    [Header("Internal parameters (editable per-structure)")]
    [Tooltip("Text for TextInput structure")]
    [TextArea]
    public string textContent = "Sample text";

    [Tooltip("Formula parameters for FormulaInput: base, exponent, flag upper/lower")]
    public int formulaBase = 2;
    public int formulaExponent = 3;
    public bool formulaUpperCase = true;

    [Header("References (child structure roots)")]
    public GameObject structure_TextInput;
    public GameObject structure_FormulaInput;

    // runtime
    private Camera cam;
    private Vector3 dragOffsetWorld;
    private bool dragging = false;
    private BoxCollider2D bc2d;
    private Rigidbody2D rb2d;

    private void Reset()
    {
        bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;
    }

    private void Awake()
    {
        cam = Camera.main;
        bc2d = GetComponent<BoxCollider2D>();
        if (bc2d) bc2d.isTrigger = true;

        // optional Rigidbody to cooperate with physics triggers
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.simulated = true;
        }

        ApplyStructureVisual();
        ApplySizeToCollider();
    }

    private void OnValidate()
    {
        // editor-time updates
        ApplyStructureVisual();
        ApplySizeToCollider();
        SnapIfManual();
    }

    private void SnapIfManual()
    {
        if (!Application.isPlaying && useManualGridPosition && LevelController.Instance != null)
        {
            transform.position = LevelController.Instance.GridToWorld(gridPosition);
        }
    }

    private void ApplySizeToCollider()
    {
        if (bc2d == null) bc2d = GetComponent<BoxCollider2D>();
        if (bc2d != null && LevelController.Instance != null)
        {
            bc2d.size = new Vector2(sizeInCells.x * LevelController.Instance.moduleSize.x,
                                     sizeInCells.y * LevelController.Instance.moduleSize.y);
        }
    }

    private void ApplyStructureVisual()
    {
        if (structure_TextInput != null) structure_TextInput.SetActive(structure == StructureType.TextInput);
        if (structure_FormulaInput != null) structure_FormulaInput.SetActive(structure == StructureType.FormulaInput);

        // Update internal text displays if present (optional)
        if (structure_TextInput != null && structure_TextInput.activeSelf)
        {
            var txt = structure_TextInput.GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null) txt.text = textContent;
        }

        if (structure_FormulaInput != null && structure_FormulaInput.activeSelf)
        {
            var txt = structure_FormulaInput.GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null)
            {
                string baseStr = formulaUpperCase ? formulaBase.ToString().ToUpper() : formulaBase.ToString().ToLower();
                txt.text = $"{baseStr}^{formulaExponent}";
            }
        }
    }

    #region Drag-n-Drop (mouse-based, simple & reliable)
    private void OnMouseDown()
    {
        // start dragging
        dragging = true;
        cam = Camera.main;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        dragOffsetWorld = transform.position - mouseWorld;
        // bring to front (z) if needed
    }

    private void OnMouseDrag()
    {
        if (!dragging) return;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld + dragOffsetWorld;
    }

    private void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;

        // On drop — snap to grid (if LevelController present)
        if (LevelController.Instance != null)
        {
            Vector2 snapped = LevelController.Instance.SnapToGrid(transform.position);
            transform.position = snapped;
        }
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // When we start overlapping with an EmptyController
        var empty = collision.GetComponent<EmptyController>();
        if (empty != null)
        {
            bool match = IsHashMatching(empty);
            Debug.Log($"[BlockController] Block {name} (hash {hashIndex}) entered Empty {empty.name} (hash {empty.hashIndex}). Match: {match}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var empty = collision.GetComponent<EmptyController>();
        if (empty != null)
        {
            Debug.Log($"[BlockController] Block {name} left Empty {empty.name}");
        }
    }

    public bool IsHashMatching(EmptyController empty)
    {
        if (empty == null) return false;
        return string.Equals(this.hashIndex?.Trim(), empty.hashIndex?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // Utility: apply inspector changes to visuals (callable via context menu if needed)
    [ContextMenu("Apply Structure & Visuals")]
    public void ApplyAll()
    {
        ApplyStructureVisual();
        ApplySizeToCollider();
        if (useManualGridPosition && LevelController.Instance != null)
            transform.position = LevelController.Instance.GridToWorld(gridPosition);
    }
}