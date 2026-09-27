using UnityEngine;

public class TreeController : MonoBehaviour
{
    private float startPos;
    private float length;
    private float fixedY;

    public GameObject cam;
    public float parallaxEffect = 0.2f;

    void Start()
    {
        startPos = transform.position.x;
        fixedY = transform.position.y;

        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void LateUpdate()
    {
        if (cam == null)
            return;

        float distance = cam.transform.position.x * parallaxEffect;
        float movement = cam.transform.position.x * (1 - parallaxEffect);

        // Move ONLY left/right
        transform.position = new Vector3(
            startPos + distance,
            fixedY,
            transform.position.z
        );

        // Infinite scrolling
        if (movement > startPos + length)
        {
            startPos += length;
        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }
}