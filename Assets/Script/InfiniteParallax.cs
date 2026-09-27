using UnityEngine;
using UnityEngine.Tilemaps;

public class InfiniteParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform[] backgrounds;

    [SerializeField, Range(0f, 1f)]
    private float parallaxAmount = 0.3f;

    private float lastCameraX;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("Main Camera not found!");
            enabled = false;
            return;
        }

        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogError("Maglagay ng backgrounds sa Inspector.");
            enabled = false;
            return;
        }

        lastCameraX = cameraTransform.position.x;
    }

    void LateUpdate()
    {
        float cameraMovement =
            cameraTransform.position.x - lastCameraX;

        // Parallax movement
        foreach (Transform bg in backgrounds)
        {
            if (bg != null)
            {
                bg.position += Vector3.right *
                               (cameraMovement * parallaxAmount);
            }
        }

        // Infinite scrolling
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
                continue;

            Tilemap tilemap = backgrounds[i].GetComponent<Tilemap>();

            if (tilemap == null)
                continue;

            float leftEdge =
                tilemap.localBounds.min.x + backgrounds[i].position.x;

            float rightEdge =
                tilemap.localBounds.max.x + backgrounds[i].position.x;

            // Move background to the right
            if (rightEdge <
                cameraTransform.position.x - tilemap.localBounds.size.x)
            {
                float farthestRight = GetFarthestRight();

                backgrounds[i].position = new Vector3(
                    farthestRight - tilemap.localBounds.min.x,
                    backgrounds[i].position.y,
                    backgrounds[i].position.z
                );
            }

            // Move background to the left
            if (leftEdge >
                cameraTransform.position.x + tilemap.localBounds.size.x)
            {
                float farthestLeft = GetFarthestLeft();

                backgrounds[i].position = new Vector3(
                    farthestLeft - tilemap.localBounds.max.x,
                    backgrounds[i].position.y,
                    backgrounds[i].position.z
                );
            }
        }

        lastCameraX = cameraTransform.position.x;
    }

    float GetFarthestRight()
    {
        float farthest = float.MinValue;

        foreach (Transform bg in backgrounds)
        {
            if (bg == null)
                continue;

            Tilemap tilemap = bg.GetComponent<Tilemap>();

            if (tilemap == null)
                continue;

            float right =
                tilemap.localBounds.max.x + bg.position.x;

            if (right > farthest)
                farthest = right;
        }

        return farthest;
    }

    float GetFarthestLeft()
    {
        float farthest = float.MaxValue;

        foreach (Transform bg in backgrounds)
        {
            if (bg == null)
                continue;

            Tilemap tilemap = bg.GetComponent<Tilemap>();

            if (tilemap == null)
                continue;

            float left =
                tilemap.localBounds.min.x + bg.position.x;

            if (left < farthest)
                farthest = left;
        }

        return farthest;
    }
}