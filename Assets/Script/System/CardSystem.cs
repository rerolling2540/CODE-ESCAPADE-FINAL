using System;
using System.Collections;
using System.Collections.Generic;
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
    private GUIStyle pileTitleStyle;
    private GUIStyle pileCardTitleStyle;
    private GUIStyle pileCardDetailsStyle;

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

        for (int i = 0; i < deckData.Count; i++)
        {
            CardData cardData = deckData[i];
            if (cardData == null)
            {
                Debug.LogError($"CardSystem skipped missing card reference at HeroData.Deck index {i}.");
                continue;
            }

            // Repeated CardData references represent separate copies in the deck.
            drawPile.Add(new Card(cardData));
        }

        SetupPileViews();

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

    public void TogglePile(bool draw)
    {
        if (QuestionUI.IsQuestionActive) return;
        if (pileWindowOpen && showDrawPile == draw)
        {
            ClosePileWindow();
            return;
        }

        showDrawPile = draw;
        pileWindowScroll = Vector2.zero;
        pileWindowOpen = true;
    }

    public void ClosePileWindow()
    {
        pileWindowOpen = false;
    }

    private void OnGUI()
    {
        if (QuestionUI.IsQuestionActive)
        {
            pileWindowOpen = false;
            return;
        }
        if (GameControlsGuide.GameplayOverlayHidden) return;
        if (!pileWindowOpen) return;

        float width = Mathf.Min(960f, Screen.width * 0.86f);
        float height = Mathf.Min(850f, Screen.height * 0.84f);
        Rect windowRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown &&
            !windowRect.Contains(currentEvent.mousePosition) &&
            !IsPileIconAt(currentEvent.mousePosition))
        {
            ClosePileWindow();
            currentEvent.Use();
            return;
        }

        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;
        GUI.color = new Color(0.10f, 0.08f, 0.07f, 0.98f);
        GUI.Box(windowRect, GUIContent.none);
        GUI.color = oldColor;
        List<Card> pile = showDrawPile ? drawPile : discardPile;
        EnsurePileWindowStyles();
        GUI.contentColor = new Color(1f, 0.91f, 0.73f);

        Rect inner = new Rect(windowRect.x + 16f, windowRect.y + 12f, windowRect.width - 32f, windowRect.height - 24f);
        Rect titleRect = new Rect(inner.x, inner.y, inner.width - 80f, 34f);
        GUI.Label(titleRect, $"{(showDrawPile ? "DRAW PILE" : "DISCARD PILE")}  ·  {pile.Count} cards", pileTitleStyle);
        if (GUI.Button(new Rect(inner.xMax - 72f, inner.y, 72f, 30f), "Close"))
            pileWindowOpen = false;

        Rect listRect = new Rect(inner.x, inner.y + 40f, inner.width, inner.height - 40f);
        float rowHeight = 172f;
        float contentHeight = Mathf.Max(listRect.height, pile.Count * rowHeight + 8f);
        Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, contentHeight);
        pileWindowScroll = GUI.BeginScrollView(listRect, pileWindowScroll, viewRect);
        if (pile.Count == 0)
        {
            GUI.Label(new Rect(12f, 14f, viewRect.width - 24f, 34f), "This pile is empty.");
        }
        else
        {
            for (int i = 0; i < pile.Count; i++)
                DrawPileCard(new Rect(4f, 4f + i * rowHeight, viewRect.width - 8f, rowHeight - 8f), pile[i], i + 1);
        }
        GUI.EndScrollView();
        GUI.color = oldColor;
        GUI.contentColor = oldContentColor;
    }

    private bool IsPileIconAt(Vector2 guiPosition)
    {
        return (drawPileView != null && drawPileView.ContainsGuiPoint(guiPosition)) ||
               (discardPileView != null && discardPileView.ContainsGuiPoint(guiPosition));
    }

    private void EnsurePileWindowStyles()
    {
        if (pileTitleStyle != null) return;
        pileTitleStyle = new GUIStyle(GUI.skin.GetStyle("boldLabel"))
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleLeft
        };
        pileCardTitleStyle = new GUIStyle(GUI.skin.GetStyle("boldLabel"))
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleLeft
        };
        pileCardDetailsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
    }

    private void DrawPileCard(Rect rect, Card card, int position)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.30f, 0.23f, 0.15f, 1f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = oldColor;

        GUI.contentColor = new Color(1f, 0.89f, 0.62f);
        string title = card != null ? card.Title : "Missing Card";
        GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 170f, 38f), $"{position}.  {title}", pileCardTitleStyle);
        GUI.Label(new Rect(rect.xMax - 145f, rect.y + 10f, 132f, 38f), card != null ? $"{card.Mana} mana" : "—", pileCardTitleStyle);

        if (card != null && card.Image != null)
            DrawPileCardArt(new Rect(rect.x + 14f, rect.y + 54f, 90f, 102f), card.Image);

        GUI.contentColor = new Color(0.96f, 0.91f, 0.80f);
        float detailsX = rect.x + (card != null && card.Image != null ? 118f : 14f);
        GUI.Label(new Rect(detailsX, rect.y + 56f, rect.width - (detailsX - rect.x) - 16f, rect.height - 64f),
            card != null ? card.Description : "The card data asset is missing.", pileCardDetailsStyle);
    }

    private static void DrawPileCardArt(Rect rect, Sprite sprite)
    {
        if (sprite == null || (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)) return;

        Rect uv = sprite.textureRect;
        Rect texCoords = new Rect(
            uv.x / sprite.texture.width,
            uv.y / sprite.texture.height,
            uv.width / sprite.texture.width,
            uv.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords, true);
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
            Dictionary<AutoTargetEffect, List<CombatantView>> effectTargets = new();
            foreach (AutoTargetEffect effect in playCardGA.Card.OtherEffects)
            {
                if (effect == null || effect.Effect == null || effect.TargetMode == null) continue;
                List<CombatantView> targets = effect.TargetMode.GetTargets();
                effectTargets[effect] = targets;
                if (attackTarget == null && effect.Effect is DealDamageEffect)
                    attackTarget = targets.Find(target => target is EnemyView && target.CurrentHealth > 0);
            }
            yield return HeroSystem.Instance.HeroView.PlayBattleAttack(attackTarget);
            if (playCardGA.Card.ManualTargetEffect != null)
            {
                List<CombatantView> targets = new() { playCardGA.ManualTarget };
                if (playCardGA.Card.ManualTargetEffect is DealDamageEffect)
                {
                    DealDamageGA damageGA = (DealDamageGA)playCardGA.Card.ManualTargetEffect.GetGameAction(
                        targets, HeroSystem.Instance.HeroView, playCardGA.Card.CardData);
                    CardRefinementRules.RefineAction(damageGA, playCardGA.Card.Level);
                    yield return DamageSystemRunner(damageGA);
                }
                else
                {
                    ActionSystem.Instance.AddReaction(new PerformEffectGA(
                        playCardGA.Card.ManualTargetEffect, targets, playCardGA.Card.CardData, playCardGA.Card.Level));
                }
            }

            foreach (var effectWrapper in playCardGA.Card.OtherEffects)
            {
                if (effectWrapper == null || !effectTargets.TryGetValue(effectWrapper, out List<CombatantView> targets))
                    continue;

                if (effectWrapper.Effect is DealDamageEffect)
                {
                    DealDamageGA damageGA = (DealDamageGA)effectWrapper.Effect.GetGameAction(
                        targets, HeroSystem.Instance.HeroView, playCardGA.Card.CardData);
                    CardRefinementRules.RefineAction(damageGA, playCardGA.Card.Level);
                    yield return DamageSystemRunner(damageGA);
                }
                else
                {
                    ActionSystem.Instance.AddReaction(new PerformEffectGA(
                        effectWrapper.Effect, targets, playCardGA.Card.CardData, playCardGA.Card.Level));
                }
            }
        }
        else
        {
            AttackHeroGA attackHeroGA = new(EnemySystem.Instance.CurrentEnemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
        }

        ManualTargetSystem.Instance?.ClearTargetHighlight();
    }

    private static IEnumerator DamageSystemRunner(DealDamageGA damageGA)
    {
        DamageSystem damageSystem = FindFirstObjectByType<DamageSystem>();
        if (damageSystem != null)
        {
            yield return damageSystem.ApplyDamageDirect(damageGA);
            yield break;
        }

        // Keep combat functional if a scene is missing the presentation system.
        if (damageGA?.Targets == null) yield break;
        foreach (CombatantView target in damageGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;
            target.Damage(damageGA.GetDamageForTarget(target));
            yield return null;
        }
    }

    private IEnumerator DrawCard()
    {
        Card card = drawPile.Draw();
        if (card == null) yield break;

        hand.Add(card);
        CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);
        yield return handView.AddCard(cardView);
    }

    private void RefillDeck()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
    }

    private IEnumerator DiscardCard(CardView cardView)
    {
        if (cardView == null || cardView.Card == null) yield break;

        discardPile.Add(cardView.Card);
        cardView.transform.DOScale(Vector3.zero, 0.15f);
        Tween tween = cardView.transform.DOMove(discardPilePoint.position, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }
}
