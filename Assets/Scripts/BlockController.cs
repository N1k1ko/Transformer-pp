using UnityEngine;
using System;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class BlockController : MonoBehaviour
{
    [Header("Core")]
    public string hashIndex = "#0000";

#region Structures
    [Header("Structures")]
    [Tooltip("Parent object that contains all block structures")]
    public Transform structuresRoot;

    [Tooltip("Selected structure index")]
    public int selectedStructureIndex = 0;

    [SerializeField, HideInInspector]
    private List<GameObject> structures = new List<GameObject>();
#endregion

#region Grid & Size
    [Header("Grid")]
    public Vector2Int sizeInCells = new Vector2Int(1, 1);
    public bool useManualGridPosition;
    public Vector2Int gridPosition;
#endregion

#region RunTime
    private BoxCollider2D collider2D;
    private Rigidbody2D rb2D;
#endregion

#region Unity
    private void Awake()
    {
        collider2D = GetComponent<BoxCollider2D>();
        collider2D.isTrigger = true;

        rb2D = GetComponent<Rigidbody2D>();
        if (!rb2D)
        {
            rb2D = gameObject.AddComponent<Rigidbody2D>();
            rb2D.bodyType = RigidbodyType2D.Kinematic;
        }

        RefreshStructures();
        ApplyStructureVisibility();
        ResizeToFitContent();
        ApplyColliderSize();
    }

    private void OnValidate()
    {
        RefreshStructures();
        selectedStructureIndex = Mathf.Clamp(selectedStructureIndex, 0, structures.Count - 1);

        ApplyStructureVisibility();
        ResizeToFitContent();
        ApplyColliderSize();
        SnapIfManual();
    }
#endregion

#region Structure Management
    private void RefreshStructures()
    {
        structures.Clear();

        if (structuresRoot == null) return;

        foreach (Transform child in structuresRoot)
            structures.Add(child.gameObject);
    }

    private void ApplyStructureVisibility()
    {
        for (int i = 0; i < structures.Count; i++)
            structures[i].SetActive(i == selectedStructureIndex);
    }

    [ContextMenu("Create Structure")]
    public void CreateStructure()
    {
        if (structuresRoot == null)
        {
            Debug.LogWarning("StructuresRoot not assigned");
            return;
        }

        GameObject newStruct = new GameObject($"Structure_{structures.Count}");
        newStruct.transform.SetParent(structuresRoot);
        newStruct.transform.localPosition = Vector3.zero;
        newStruct.transform.localScale = Vector3.one;

        structures.Add(newStruct);
        selectedStructureIndex = structures.Count - 1;

        ApplyStructureVisibility();
        ResizeToFitContent();
    }
#endregion

#region Auto Resize
    private void ResizeToFitContent()
    {
        if (structures.Count == 0 || LevelController.Instance == null) return;

        GameObject active = structures[selectedStructureIndex];
        if (!active) return;

        Bounds bounds = CalculateBounds(active);
        Vector2 module = LevelController.Instance.moduleSize;

        int requiredX = Mathf.CeilToInt(bounds.size.x / module.x);
        int requiredY = Mathf.CeilToInt(bounds.size.y / module.y);

        requiredX = Mathf.Max(1, requiredX);
        requiredY = Mathf.Max(1, requiredY);

        sizeInCells = new Vector2Int(requiredX, requiredY);
    }

    private Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);

        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds;
    }

    private void ApplyColliderSize()
    {
        if (!collider2D || LevelController.Instance == null) return;

        collider2D.size = new Vector2(
            sizeInCells.x * LevelController.Instance.moduleSize.x,
            sizeInCells.y * LevelController.Instance.moduleSize.y
        );
    }
#endregion

#region Grid Snap
    private void SnapIfManual()
    {
        if (!useManualGridPosition || LevelController.Instance == null) return;

        transform.position = LevelController.Instance.GridToWorld(gridPosition);
    }
#endregion

#region Collision
    private void OnTriggerEnter2D(Collider2D other)
    {
        var empty = other.GetComponent<EmptyController>();
        if (!empty) return;

        bool match = string.Equals(hashIndex, empty.hashIndex, StringComparison.OrdinalIgnoreCase);
        Debug.Log($"[Block] {name} → Cell {empty.name} | Match: {match}");
    }
#endregion
}