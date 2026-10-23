using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;
    private readonly List<Card> drawPile = new();
    private readonly List<Card> discardPile = new();
    private readonly List<Card> hand = new();
    private CardPileView drawPileView;
    private CardPileView discardPileView;
    private bool pileWindowOpen;
    private bool showDrawPile;
    private Vector2 pileWindowScroll;

    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
    }

    public void Setup(List<CardData> deckData)
    {
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();

        if (deckData == null)
        {
            Debug.LogError("CardSystem could not set up the deck because HeroData.Deck is missing.");
            return;
        }

        var addedCards = new HashSet<CardData>();
        for (int i = 0; i < deckData.Count; i++)
        {
            CardData cardData = deckData[i];
            if (cardData == null)
            {
                Debug.LogError($"CardSystem skipped missing card reference at HeroData.Deck index {i}.");
                continue;
            }

            if (!addedCards.Add(cardData))
            {
                Debug.LogWarning($"CardSystem skipped duplicate '{cardData.name}' entry in HeroData.Deck. Each card asset can appear only once in battle.");
                continue;
            }

            drawPile.Add(new Card(cardData));
        }

        SetupPileViews();
        RefreshPileViews();

        if (drawPile.Count == 0)
            Debug.LogError("CardSystem has no valid cards to draw. Check the HeroData deck references.");
    }

    private void SetupPileViews()
    {
        if (drawPilePoint == null || discardPilePoint == null)
        {
            Debug.LogError("CardSystem needs DrawPilePoint and DiscardPilePoint transforms to show the card piles.");
            return;
        }

        // Place the piles relative to the camera so their icons and card travel targets stay on screen.
        Camera battleCamera = Camera.main;
        if (battleCamera != null)
        {
            float cameraDepth = Mathf.Abs(battleCamera.transform.position.z);
            Vector3 drawPosition = battleCamera.ViewportToWorldPoint(new Vector3(0.045f, 0.20f, cameraDepth));
            Vector3 discardPosition = battleCamera.ViewportToWorldPoint(new Vector3(0.955f, 0.20f, cameraDepth));
            drawPosition.z = discardPosition.z = 0f;
            drawPilePoint.position = drawPosition;
            discardPilePoint.position = discardPosition;
        }

        Sprite drawSprite = Resources.Load<Sprite>("PileIcons/DrawPile");
        Sprite discardSprite = Resources.Load<Sprite>("PileIcons/DiscardPile");
        drawPileView = CreateOrUpdatePileView(drawPileView, drawPilePoint, drawSprite, true);
        discardPileView = CreateOrUpdatePileView(discardPileView, discardPilePoint, discardSprite, false);
    }

    private CardPileView CreateOrUpdatePileView(CardPileView view, Transform point, Sprite sprite, bool isDrawPile)
    {
        if (sprite == null)
        {
            Debug.LogError($"Card pile sprite for {(isDrawPile ? "draw" : "discard")} pile was not found in Resources/PileIcons.");
            return view;
        }

        if (view == null)
        {
            GameObject pileObject = new GameObject(isDrawPile ? "Draw Pile Display" : "Discard Pile Display");
            pileObject.transform.position = point.position;
            view = pileObject.AddComponent<CardPileView>();
        }

        view.Setup(this, sprite, isDrawPile);
        view.transform.position = point.position;
        return view;
    }

    private void RefreshPileViews()
    {
        if (drawPileView != null) drawPileView.SetCount(drawPile.Count);
        if (discardPileView != null) discardPileView.SetCount(discardPile.Count);
    }

    public void OpenPile(bool draw)
    {
        showDrawPile = draw;
        pileWindowScroll = Vector2.zero;
        pileWindowOpen = true;
    }

    private void OnGUI()
    {
        if (!pileWindowOpen) return;

        const float width = 360f;
        const float height = 460f;
        Rect windowRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;
        GUI.color = new Color(0.22f, 0.14f, 0.08f, 0.98f);
        GUI.Box(windowRect, GUIContent.none);
        GUI.color = oldColor;
        GUI.contentColor = new Color(1f, 0.91f, 0.73f);

        GUILayout.BeginArea(new Rect(windowRect.x + 16f, windowRect.y + 14f, windowRect.width - 32f, windowRect.height - 28f));
        GUILayout.BeginHorizontal();
        List<Card> pile = showDrawPile ? drawPile : discardPile;
        GUILayout.Label($"{(showDrawPile ? "DRAW PILE" : "DISCARD PILE")}  ({pile.Count})", GUI.skin.GetStyle("boldLabel"));
        if (GUILayout.Button("Close", GUILayout.Width(62f))) pileWindowOpen = false;
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        pileWindowScroll = GUILayout.BeginScrollView(pileWindowScroll);
        foreach (var row in CountCardsByTitle(pile))
            GUILayout.Label($"{row.Key}  × {row.Value}");
        if (pile.Count == 0) GUILayout.Label("This pile is empty.");
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.color = oldColor;
        GUI.contentColor = oldContentColor;
    }

    private static List<KeyValuePair<string, int>> CountCardsByTitle(List<Card> cards)
    {
        var counts = new Dictionary<string, int>();
        foreach (Card card in cards)
        {
            string title = card != null ? card.Title : "Missing Card";
            counts[title] = counts.TryGetValue(title, out int count) ? count + 1 : 1;
        }

        return counts.OrderBy(pair => pair.Key, System.StringComparer.Ordinal).ToList();
    }

    private IEnumerator DrawCardsPerformer(DrawCardsGA drawCardsGA)
    {
        for (int i = 0; i < drawCardsGA.Amount; i++)
        {
            if (drawPile.Count == 0 && discardPile.Count > 0)
                RefillDeck();
            if (drawPile.Count == 0)
                yield break;

            yield return DrawCard();
        }
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        foreach (var card in hand)
        {
            CardView cardView = handView.RemoveCard(card);
            yield return DiscardCard(cardView);
        }
        hand.Clear();
    }

    private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    {
        if (playCardGA == null || playCardGA.Card == null || !hand.Remove(playCardGA.Card))
        {
            Debug.LogWarning("CardSystem ignored a played card that is not currently in the player's hand.");
            yield break;
        }

        BattleAudio.Play(BattleSound.CardPlay);
        CardView cardView = handView.RemoveCard(playCardGA.Card);
        yield return DiscardCard(cardView);

        SpendManaGA spendManaGA = new(playCardGA.Card.Mana);
        ActionSystem.Instance.AddReaction(spendManaGA);

        if (playCardGA.QuestionAnsweredCorrectly)
        {
            if (playCardGA.ManualTarget != null)
                HeroSystem.Instance.HeroView.FaceTowards(playCardGA.ManualTarget);
            BattleAudio.HeroAttack(playCardGA.Card.CardData);
            CombatantView attackTarget = playCardGA.Card.ManualTargetEffect is DealDamageEffect ? playCardGA.ManualTarget : null;
            if (attackTarget == null)
            {
                foreach (var effect in playCardGA.Card.OtherEffects)
                {
                    if (!(effect.Effect is DealDamageEffect)) continue;
                    attackTarget = effect.TargetMode.GetTargets().Find(target => target is EnemyView && target.CurrentHealth > 0);
                    if (attackTarget != null) break;
                }
            }
            yield return HeroSystem.Instance.HeroView.PlayBattleAttack(attackTarget);
            if (playCardGA.Card.ManualTargetEffect != null)
            {
                PerformEffectGA performEffectGA = new(
                    playCardGA.Card.ManualTargetEffect,
                    new() { playCardGA.ManualTarget },
                    playCardGA.Card.CardData);

                ActionSystem.Instance.AddReaction(performEffectGA);
            }

            foreach (var effectWrapper in playCardGA.Card.OtherEffects)
            {
                List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();

                PerformEffectGA performEffectGA = new(
                    effectWrapper.Effect, 
                    targets, 
                    playCardGA.Card.CardData);

                ActionSystem.Instance.AddReaction(performEffectGA);
            }
        }
        else
        {
            AttackHeroGA attackHeroGA = new(EnemySystem.Instance.CurrentEnemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
        }
    }

    private IEnumerator DrawCard()
    {
        Card card = drawPile.Draw();
        if (card == null) yield break;

        RefreshPileViews();
        hand.Add(card);
        CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);
        yield return handView.AddCard(cardView);
    }

    private void RefillDeck()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        RefreshPileViews();
    }

    private IEnumerator DiscardCard(CardView cardView)
    {
        if (cardView == null || cardView.Card == null) yield break;

        discardPile.Add(cardView.Card);
        RefreshPileViews();
        cardView.transform.DOScale(Vector3.zero, 0.15f);
        Tween tween = cardView.transform.DOMove(discardPilePoint.position, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }
}
