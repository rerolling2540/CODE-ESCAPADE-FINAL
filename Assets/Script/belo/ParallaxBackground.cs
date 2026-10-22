
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ParallaxBackground : MonoBehaviour
{
    public Transform cam;

    [Range(0f, 1f)]
    public float parallaxEffect = 0.1f;

    [Min(0f)]
    public float smoothTime = 0.08f;

    private Transform[] tiles;
    private float tileWidth;
    private float cameraStartX;
    private float halfViewWidth;
    private float[] velocities;

    private void Start()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (cam == null)
        {
            Debug.LogError("Parallax: Walang Camera!");
            enabled = false;
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("Parallax: Walang SpriteRenderer o Sprite!");
            enabled = false;
            return;
        }

        tileWidth = sr.bounds.size.x;

        Camera cameraComponent = cam.GetComponent<Camera>();
        if (cameraComponent == null)
            cameraComponent = Camera.main;

        halfViewWidth = cameraComponent != null &&
                        cameraComponent.orthographic
            ? cameraComponent.orthographicSize * cameraComponent.aspect
            : tileWidth;

        cameraStartX = cam.position.x;

        int count = Mathf.Max(
            3,
            Mathf.CeilToInt(halfViewWidth * 2f / tileWidth) + 3
        );

        if (count % 2 == 0)
            count++;

        tiles = new Transform[count];
        velocities = new float[count];

        int center = count / 2;
        tiles[center] = transform;

        for (int i = 0; i < count; i++)
        {
            if (i == center)
                continue;

            GameObject copy = Instantiate(gameObject, transform.parent);
            copy.name = gameObject.name + "_Tile_" + i;

            ParallaxBackground script =
                copy.GetComponent<ParallaxBackground>();

            if (script != null)
                script.enabled = false;

            copy.transform.SetPositionAndRotation(
                transform.position, transform.rotation);

            copy.transform.localScale = transform.localScale;
            tiles[i] = copy.transform;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 p = tiles[i].position;
            p.x = transform.position.x + (i - center) * tileWidth;
            tiles[i].position = p;
        }
    }

    private void LateUpdate()
    {
        if (cam == null || tiles == null)
            return;

        // Gumalaw lang kapag gumalaw ang camera.
        float cameraMovement = cam.position.x - cameraStartX;
        float parallaxOffset = cameraMovement * parallaxEffect;

        float cameraX = cam.position.x;
        float left = cameraX - halfViewWidth;
        float right = cameraX + halfViewWidth;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null)
                continue;

            // Target position based on camera movement.
            float targetX =
                tiles[i].position.x +
                (cam.position.x - cameraX) * parallaxEffect;

            // Smoothly follow the parallax target.
            Vector3 pos = tiles[i].position;
            pos.x = Mathf.SmoothDamp(
                pos.x,
                targetX,
                ref velocities[i],
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            tiles[i].position = pos;
        }

        // Update reference for the next frame.
        cameraStartX = cam.position.x;
    }
}
