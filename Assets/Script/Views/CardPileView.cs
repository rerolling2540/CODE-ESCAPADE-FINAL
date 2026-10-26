using UnityEngine;

public class CardPileView : MonoBehaviour
{
    private CardSystem cardSystem;
    private bool isDrawPile;

    public void Setup(CardSystem owner, Sprite icon, bool drawPile)
    {
        cardSystem = owner;
        isDrawPile = drawPile;
        transform.localScale = Vector3.one * 0.12f;

        // Remove the old runtime label if this pile object survives a script reload.
        Transform oldLabel = transform.Find("Pile Count Label");
        if (oldLabel != null) Destroy(oldLabel.gameObject);

        SpriteRenderer iconRenderer = GetComponent<SpriteRenderer>();
        if (iconRenderer == null) iconRenderer = gameObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = 40;

        BoxCollider hitTarget = GetComponent<BoxCollider>();
        if (hitTarget == null) hitTarget = gameObject.AddComponent<BoxCollider>();
        hitTarget.size = icon.bounds.size * 1.1f;

    }

    private void OnMouseDown()
    {
        if (cardSystem != null && Interactions.Instance != null && Interactions.Instance.PlayerCanInteract())
            cardSystem.TogglePile(isDrawPile);
    }

    public bool ContainsGuiPoint(Vector2 guiPoint)
    {
        Collider pileCollider = GetComponent<Collider>();
        Camera viewCamera = Camera.main;
        if (pileCollider == null || viewCamera == null) return false;

        Bounds bounds = pileCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float screenMinX = float.PositiveInfinity;
        float screenMinY = float.PositiveInfinity;
        float screenMaxX = float.NegativeInfinity;
        float screenMaxY = float.NegativeInfinity;

        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 corner = new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            Vector3 screen = viewCamera.WorldToScreenPoint(corner);
            screenMinX = Mathf.Min(screenMinX, screen.x);
            screenMaxX = Mathf.Max(screenMaxX, screen.x);
            screenMinY = Mathf.Min(screenMinY, screen.y);
            screenMaxY = Mathf.Max(screenMaxY, screen.y);
        }

        Vector2 screenPoint = new Vector2(guiPoint.x, Screen.height - guiPoint.y);
        return screenPoint.x >= screenMinX && screenPoint.x <= screenMaxX &&
               screenPoint.y >= screenMinY && screenPoint.y <= screenMaxY;
    }
}
