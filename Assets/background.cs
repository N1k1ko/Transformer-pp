using UnityEngine;

[ExecuteAlways] // Работает прямо в редакторе
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    private SpriteRenderer sr;
    private Camera cam;

    void Update()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;

        if (cam == null || sr.sprite == null) return;

        // 1. Ставим фон ровно в центр камеры
        // Z = 10 (или любое положительное), чтобы быть "сзади" всех объектов, которые обычно на Z=0
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);

        // 2. Вычисляем размеры экрана в мировых единицах
        float screenHeight = cam.orthographicSize * 2;
        float screenWidth = screenHeight * cam.aspect;

        // 3. Вычисляем размеры спрайта
        float spriteHeight = sr.sprite.bounds.size.y;
        float spriteWidth = sr.sprite.bounds.size.x;

        // 4. Вычисляем, насколько нужно растянуть спрайт, чтобы он покрыл экран
        // Используем Mathf.Max, чтобы картинка заполняла экран по большей стороне (Crop/Fill),
        // чтобы не было черных полос.
        float scaleY = screenHeight / spriteHeight;
        float scaleX = screenWidth / spriteWidth;

        // Если хотите, чтобы картинка не искажалась, выберите максимальный масштаб
        float finalScale = Mathf.Max(scaleX, scaleY);

        // Если вам не важно искажение и нужно просто растянуть (Stretch):
        // transform.localScale = new Vector3(scaleX, scaleY, 1);

        // Если важно сохранить пропорции (Aspect Fill):
        transform.localScale = new Vector3(finalScale, finalScale, 1);
    }
}