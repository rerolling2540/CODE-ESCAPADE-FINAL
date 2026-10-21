using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Sets up one consistent, looping parallax on each background layer.</summary>
[DefaultExecutionOrder(-1000)]
public class InfiniteParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float parallaxAmount = 0.1f;
    [SerializeField, Min(0f)] private float smoothTime = 0.08f;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("InfiniteParallax: walang Main Camera.", this);
            enabled = false;
            return;
        }

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        var configured = new List<ParallaxBackground>();

        foreach (Renderer renderer in renderers)
        {
            if (!IsBackgroundRenderer(renderer))
                continue;

            ParallaxBackground layer = renderer.GetComponent<ParallaxBackground>();
            if (layer == null)
                layer = renderer.gameObject.AddComponent<ParallaxBackground>();

            layer.cam = cameraTransform;
            layer.parallaxEffect = parallaxAmount;
            layer.smoothTime = smoothTime;
            layer.enabled = true;
            configured.Add(layer);
        }

        if (configured.Count == 0)
        {
            Debug.LogError("InfiniteParallax: walang active background SpriteRenderer o TilemapRenderer. Pangalanan ang layer na Background/BG o i-assign ang script sa background object.", this);
            enabled = false;
            return;
        }

        // This component only wires up the layers. The layer components handle
        // their own seamless copies, so they never fight over the same object.
        enabled = false;
    }

    private bool IsBackgroundRenderer(Renderer renderer)
    {
        if (renderer == null || renderer == GetComponent<Renderer>() ||
            !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            return false;

        if (!(renderer is SpriteRenderer) && !(renderer is TilemapRenderer))
            return false;

        if (renderer.bounds.size.x <= 0f)
            return false;

        string objectName = renderer.gameObject.name.ToLowerInvariant();
        if (objectName.Contains("_tile") || objectName.Contains("_loop"))
            return false;

        return objectName.Contains("background") || objectName.StartsWith("bg") ||
               objectName.Contains("mountain") || objectName.Contains("parallax");
    }
}
