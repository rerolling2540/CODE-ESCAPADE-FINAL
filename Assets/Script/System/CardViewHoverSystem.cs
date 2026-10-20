using Unity.VisualScripting;
using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;
    private Card shownCard;
    public void Show(Card card, Vector3 position)
    {
        if (card == null || cardViewHover == null) return;
        if (shownCard != card || !cardViewHover.gameObject.activeSelf)
            BattleAudio.Play(BattleSound.CardHover);
        shownCard = card;
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
