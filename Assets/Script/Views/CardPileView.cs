using UnityEngine;

public class CardPileView : MonoBehaviour
{
    private CardSystem cardSystem;
    private bool isDrawPile;
    private TextMesh countLabel;

    public void Setup(CardSystem owner, Sprite icon, bool drawPile)
    {
        cardSystem = owner;
        isDrawPile = drawPile;
        transform.localScale = Vector3.one * 0.12f;

        SpriteRenderer iconRenderer = GetComponent<SpriteRenderer>();
        if (iconRenderer == null) iconRenderer = gameObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = 40;

        BoxCollider hitTarget = GetComponent<BoxCollider>();
        if (hitTarget == null) hitTarget = gameObject.AddComponent<BoxCollider>();
        hitTarget.size = icon.bounds.size * 1.1f;

        if (countLabel == null)
        {
            GameObject labelObject = new GameObject("Pile Count Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -5.4f, -0.1f);
            labelObject.transform.localScale = Vector3.one * 5f;
            countLabel = labelObject.AddComponent<TextMesh>();
            countLabel.anchor = TextAnchor.MiddleCenter;
            countLabel.alignment = TextAlignment.Center;
            countLabel.fontSize = 48;
            countLabel.characterSize = 0.12f;
            countLabel.color = new Color(1f, 0.91f, 0.73f);
            countLabel.GetComponent<MeshRenderer>().sortingOrder = 41;
        }

        SetCount(isDrawPile ? cardSystem.DrawPileCount : cardSystem.DiscardPileCount);
    }

    public void SetCount(int count)
    {
        if (countLabel != null)
            countLabel.text = $"{(isDrawPile ? "DRAW" : "DISCARD")}  {count}";
    }

    private void OnMouseDown()
    {
        if (cardSystem != null) cardSystem.OpenPile(isDrawPile);
    }
}
