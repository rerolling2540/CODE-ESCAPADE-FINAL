using UnityEngine;

/// <summary>Repeats one background sprite horizontally with smooth parallax motion.</summary>
[DefaultExecutionOrder(1000)]
public class ParallaxBackground : MonoBehaviour
{
    public Transform cam;

    [Range(0f, 1f)]
    public float parallaxEffect = 0.1f;

    [Min(0f)]
    public float smoothTime = 0.08f;

    private Transform[] tiles;
    private float[] tileStartX;
    private float[] tileVelocity;
    private float tileWidth;
    private float cameraStartX;
    private Camera targetCamera;
    private float halfViewWidth;

    private void Start()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (cam == null)
        {
            Debug.LogError("ParallaxBackground: Main Camera is missing.", this);
            enabled = false;
            return;
        }

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
        {
            Debug.LogError("ParallaxBackground: kailangan ng SpriteRenderer na may Sprite sa object na ito.", this);
            enabled = false;
            return;
        }

        tileWidth = sprite.bounds.size.x;
        if (tileWidth <= 0f)
        {
            Debug.LogError("ParallaxBackground: zero ang lapad ng sprite.", this);
            enabled = false;
            return;
        }

        targetCamera = cam.GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;
        halfViewWidth = targetCamera != null && targetCamera.orthographic
            ? targetCamera.orthographicSize * targetCamera.aspect
            : tileWidth;

        cameraStartX = cam.position.x;
        int tileCount = Mathf.Max(3, Mathf.CeilToInt((halfViewWidth * 2f) / tileWidth) + 2);
        if (tileCount % 2 == 0)
            tileCount++;

        tiles = new Transform[tileCount];
        tileStartX = new float[tileCount];
        tileVelocity = new float[tileCount];
        int centerIndex = tileCount / 2;
        tiles[centerIndex] = transform;

        for (int i = 0; i < tileCount; i++)
        {
            if (i == centerIndex)
                continue;

            GameObject copy = Instantiate(gameObject, transform.parent);
            copy.name = gameObject.name + "_Tile" + (i - centerIndex);
            ParallaxBackground copyScript = copy.GetComponent<ParallaxBackground>();
            if (copyScript != null)
                copyScript.enabled = false;
            copy.transform.SetPositionAndRotation(transform.position, transform.rotation);
            copy.transform.localScale = transform.localScale;
            tiles[i] = copy.transform;
        }

        float originalX = transform.position.x;
        for (int i = 0; i < tileCount; i++)
        {
            tileStartX[i] = originalX + (i - centerIndex) * tileWidth;
            SetTileX(i, tileStartX[i]);
        }
    }

    private void LateUpdate()
    {
        if (cam == null || tiles == null)
            return;

        float cameraDelta = cam.position.x - cameraStartX;
        float offset = cameraDelta * parallaxEffect;
        float viewLeft = cam.position.x - halfViewWidth;
        float viewRight = cam.position.x + halfViewWidth;

        for (int i = 0; i < tiles.Length; i++)
        {
            float targetX = tileStartX[i] + offset;
            float leftEdge = targetX - tileWidth * 0.5f;
            float rightEdge = targetX + tileWidth * 0.5f;

            if (rightEdge < viewLeft)
            {
                float repeats = Mathf.Ceil((viewLeft - rightEdge) / tileWidth);
                tileStartX[i] += repeats * tileWidth;
                targetX += repeats * tileWidth;
            }
            else if (leftEdge > viewRight)
            {
                float repeats = Mathf.Ceil((leftEdge - viewRight) / tileWidth);
                tileStartX[i] -= repeats * tileWidth;
                targetX -= repeats * tileWidth;
            }

            SetTileXSmooth(i, targetX);
        }
    }

    private void SetTileX(int index, float x)
    {
        Vector3 position = tiles[index].position;
        position.x = x;
        tiles[index].position = position;
    }

    private void SetTileXSmooth(int index, float targetX)
    {
        Vector3 position = tiles[index].position;
        position.x = Mathf.SmoothDamp(position.x, targetX, ref tileVelocity[index],
            smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        tiles[index].position = position;
    }
}
