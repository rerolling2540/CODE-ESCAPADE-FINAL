using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;

    private Vector3 startPosition;
    private void Update()
    {
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition)) return;
        Vector3 direction = -(startPosition - arrowHead.transform.position).normalized;
        lineRenderer.SetPosition(1, endPosition - direction * 0.5f);
        arrowHead.transform.position = endPosition;
        arrowHead.transform.right = direction;

    }
    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
        lineRenderer.SetPosition(0, startPosition);
        if (MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition))
            lineRenderer.SetPosition(1, endPosition);
    }
}
