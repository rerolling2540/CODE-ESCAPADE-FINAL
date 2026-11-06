using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;
    [SerializeField, Min(1f)] private float previewScaleFactor = 1.05f;
    private Vector3 originalHoverScale = Vector3.one;
    private Card shownCard;
    private CardView sourceCardView;

    protected override void Awake()
    {
        base.Awake();
        if (cardViewHover != null) originalHoverScale = cardViewHover.transform.localScale;
    }

    void LateUpdate()
    {
        if (MatchSetupSystem.StarfallCutsceneActive && shownCard != null) Hide();
    }

    public void Show(Card card, Vector3 position, CardView source = null)
    {
        if (MatchSetupSystem.StarfallCutsceneActive || GameControlsGuide.GameplayOverlayHidden)
        {
            source?.RestoreHoverVisual();
            Hide();
            return;
        }
        if (card == null || cardViewHover == null) return;
        if (sourceCardView != source) sourceCardView?.RestoreHoverVisual();
        sourceCardView = source;
        if (shownCard != card || !cardViewHover.gameObject.activeSelf)
            BattleAudio.Play(BattleSound.CardHover);
        shownCard = card;
        // Keep the preview readable, but close to the same size as a card in hand.
        cardViewHover.transform.localScale = originalHoverScale * Mathf.Max(1f, previewScaleFactor);
        cardViewHover.gameObject.SetActive(true);
        cardViewHover.Setup(card);
        cardViewHover.transform.position = position;
        // Hand cards sit at sorting order 30 and enemies at 25. The hover card
        // must render in front of both so its text remains readable.
        SortingGroup sortingGroup = cardViewHover.GetComponent<SortingGroup>();
        if (sortingGroup == null) sortingGroup = cardViewHover.GetComponentInChildren<SortingGroup>(true);
        if (sortingGroup != null) sortingGroup.sortingOrder = 200;

    }
    public void Hide()
    {
        shownCard = null;
        sourceCardView?.RestoreHoverVisual();
        sourceCardView = null;
        if (cardViewHover == null) return;
        cardViewHover.gameObject.SetActive(false);
    }
    
}
