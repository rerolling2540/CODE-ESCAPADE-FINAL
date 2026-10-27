using Unity.VisualScripting;
using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;
    private const float PreviewScaleFactor = 0.78f;
    private Vector3 originalHoverScale = Vector3.one;
    private Card shownCard;

    protected override void Awake()
    {
        base.Awake();
        if (cardViewHover != null) originalHoverScale = cardViewHover.transform.localScale;
    }

    public void Show(Card card, Vector3 position)
    {
        if (card == null || cardViewHover == null) return;
        if (shownCard != card || !cardViewHover.gameObject.activeSelf)
            BattleAudio.Play(BattleSound.CardHover);
        shownCard = card;
        // Keep the preview readable, but close to the same size as a card in hand.
        cardViewHover.transform.localScale = originalHoverScale * PreviewScaleFactor;
        cardViewHover.gameObject.SetActive(true);
        cardViewHover.Setup(card);
        cardViewHover.transform.position = position;

    }
    public void Hide()
    {
        shownCard = null;
        if (cardViewHover == null) return;
        cardViewHover.gameObject.SetActive(false);
    }
    
}
