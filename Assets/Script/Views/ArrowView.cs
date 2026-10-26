using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;

    private Vector3 startPosition;

    private void Awake()
    {
        if (lineRenderer != null)
        {
            // Positions are supplied in world space; local-space rendering was
            // producing a huge white slash across the battle screen.
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = 0.08f;
            lineRenderer.startColor = new Color(1f, 0.78f, 0.28f, 0.95f);
            lineRenderer.endColor = new Color(1f, 0.48f, 0.12f, 0.95f);
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        if (arrowHead != null)
        {
            arrowHead.transform.localScale = Vector3.one * 0.3f;
            SpriteRenderer sprite = arrowHead.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = new Color(1f, 0.72f, 0.2f, 1f);
                sprite.sortingOrder = 100;
            }
        }
    }

    private void Update()
    {
        if (arrowHead == null || lineRenderer == null) return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition)) return;
        endPosition.z = startPosition.z;
        Vector3 direction = (endPosition - startPosition).normalized;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition - direction * 0.18f);
        arrowHead.transform.position = endPosition;
        if (direction.sqrMagnitude > 0.001f)
            arrowHead.transform.right = direction;

    }
    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
        if (lineRenderer == null) return;
        lineRenderer.SetPosition(0, startPosition);
        if (MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition))
        {
            endPosition.z = startPosition.z;
            lineRenderer.SetPosition(1, endPosition);
        }
    }
}
