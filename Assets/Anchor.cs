using UnityEngine;

[ExecuteAlways] // Работает даже в редакторе, чтобы вы сразу видели результат
public class PaletteScreenAnchor : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Позиция на экране от 0 до 1. (0.5, 0) - это низ центр.")]
    public Vector2 anchorPoint = new Vector2(0.5f, 0.0f);

    [Tooltip("Отступ от края в юнитах")]
    public Vector2 offset = new Vector2(0, 0.5f);

    [Tooltip("Расстояние от камеры (глубина)")]
    public float distanceFromCamera = 10f;

    [Header("Auto-Size")]
    [Tooltip("Если включено, скрипт сам учтет высоту палитры, чтобы она не уходила за экран")]
    public bool snapToBottomEdge = true;
    public bool snapToRightEdge = true;

    private Camera cam;
    private BlockPalette palette;

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // 1. Вычисляем базовую точку на экране в мировых координатах
        // (0,0 - левый низ, 1,1 - правый верх)
        Vector3 anchorPos = cam.ViewportToWorldPoint(new Vector3(anchorPoint.x, anchorPoint.y, distanceFromCamera));

        // 2. Дополнительная коррекция высоты
        float heightCorrection = 0f;
        float xCorrection = 0f;

        if (snapToBottomEdge)
        {
            if (palette == null) palette = GetComponent<BlockPalette>();

            if (palette != null)
            {
                // Палитра имеет центр в (0,0), поэтому чтобы положить её на "пол", 
                // нужно поднять её на половину её высоты.
                heightCorrection = palette.paletteSize.y / 2f;
            }
        }
        if (snapToRightEdge)
        {
            if (palette == null) palette = GetComponent<BlockPalette>();

            if (palette != null)
            {
                // Палитра имеет центр в (0,0), поэтому чтобы положить её на "пол", 
                // нужно поднять её на половину её высоты.
                xCorrection = palette.paletteSize.x / 2f;
            }
        }

        // 3. Применяем позицию
        Vector3 finalPos = anchorPos;
        finalPos.x += offset.x - xCorrection;
        finalPos.y += offset.y + heightCorrection;

        // Z ставим жестко относительно камеры, чтобы не потерять объект
        finalPos.z = cam.transform.position.z + distanceFromCamera;

        transform.position = finalPos;
    }
}