using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

public class HandView : MonoBehaviour
{ 
    [SerializeField] private SplineContainer splineContainer;
    private readonly List<CardView> cards = new();
    private readonly Dictionary<CardView, Vector3> originalScales = new();
    public IEnumerator AddCard(CardView cardView)
    {
        if (cardView == null) yield break;
        cards.Add(cardView);
        originalScales[cardView] = Vector3.one;
        yield return UpdateCardPositions(0.38f, cardView);
    }
    
    public CardView RemoveCard(Card card)
    {
        CardView cardView = GetCardView(card);
        if (cardView == null) return null;
        cards.Remove(cardView);
        cardView.transform.DOKill();
        if (originalScales.TryGetValue(cardView, out Vector3 originalScale))
        {
            cardView.transform.localScale = originalScale;
            originalScales.Remove(cardView);
        }
        StartCoroutine(UpdateCardPositions(0.15f));
        return cardView;
    }
    
    private CardView GetCardView(Card card)
    {
        return cards.Where(cardView => cardView.Card == card).FirstOrDefault();
    }
    
    private IEnumerator UpdateCardPositions(float duration, CardView arrivingCard = null)
    {
        cards.RemoveAll(card => card == null);
        if (cards.Count == 0) yield break;

        // Spread larger hands across more of the spline and scale them down gently.
        float cardSpacing = cards.Count > 1 ? Mathf.Min(0.14f, 0.88f / (cards.Count - 1)) : 0f;
        float firstCardPosition = 0.5f - (cards.Count - 1) * cardSpacing / 2;
        float scaleFactor = Mathf.Max(0.72f, 1f - Mathf.Max(0, cards.Count - 5) * 0.08f);
        float extraLift = Mathf.Min(0.6f, Mathf.Max(0, cards.Count - 5) * 0.2f);

        // The hand used to leave new cards at scale zero if its serialized spline was
        // missing or empty. Keep a visible fan layout as a fallback for such scenes.
        Spline spline = splineContainer != null ? splineContainer.Spline : null;
        if (spline == null || spline.Count < 2)
        {
            Vector3 center = splineContainer != null
                ? splineContainer.transform.position
                : transform.position + Vector3.down * 5.8f;
            float fallbackSpacing = Mathf.Min(1.35f, 7.2f / Mathf.Max(1, cards.Count - 1));
            float midpoint = (cards.Count - 1) * 0.5f;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = cards[i];
                Vector3 baseScale = originalScales.TryGetValue(card, out Vector3 originalScale)
                    ? originalScale
                    : Vector3.one;
                Vector3 targetScale = baseScale * scaleFactor;
                Vector3 targetPosition = center + new Vector3(
                    (i - midpoint) * fallbackSpacing,
                    0.18f - Mathf.Abs(i - midpoint) * 0.12f + extraLift,
                    0f);
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, (midpoint - i) * 4f);
                SortingGroup sortingGroup = card.GetComponent<SortingGroup>();
                if (sortingGroup != null) sortingGroup.sortingOrder = card == arrivingCard ? 45 : 30;

                card.transform.DOKill();
                if (card == arrivingCard)
                {
                    Tween arrival = card.transform.DOJump(targetPosition, 0.55f, 1, duration)
                        .SetEase(Ease.OutCubic);
                    card.transform.DORotate(targetRotation.eulerAngles, duration).SetEase(Ease.OutCubic);
                    card.transform.DOScale(targetScale, duration * 0.72f).SetEase(Ease.OutBack);
                    yield return arrival.WaitForCompletion();
                    if (sortingGroup != null) sortingGroup.sortingOrder = 30;
                }
                else
                {
                    card.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
                    card.transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic);
                    card.transform.DORotate(targetRotation.eulerAngles, duration).SetEase(Ease.OutCubic);
                }
            }

            if (arrivingCard == null) yield return new WaitForSeconds(duration);
            yield break;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardView card = cards[i];
            float p = firstCardPosition + i * cardSpacing;
            Vector3 splinePosition = splineContainer.transform.TransformPoint(spline.EvaluatePosition(p));
            splinePosition.y += extraLift;
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);
            Quaternion rotation = Quaternion.LookRotation(-up, Vector3.Cross(-up, forward).normalized);
            Vector3 baseScale = originalScales.TryGetValue(card, out Vector3 originalScale)
                ? originalScale
                : Vector3.one;
            Vector3 targetScale = baseScale * scaleFactor;
            SortingGroup sortingGroup = card.GetComponent<SortingGroup>();

            card.transform.DOKill();
            if (card == arrivingCard)
            {
                // Keep the drawn card above the pile icon while it flies, then
                // restore normal card sorting once it settles into the hand.
                if (sortingGroup != null) sortingGroup.sortingOrder = 45;

                Tween arrival = card.transform.DOJump(splinePosition, 0.55f, 1, duration)
                    .SetEase(Ease.OutCubic);
                card.transform.DORotate(rotation.eulerAngles, duration)
                    .SetEase(Ease.OutCubic);
                card.transform.DOScale(targetScale, duration * 0.72f)
                    .SetEase(Ease.OutBack);

                yield return arrival.WaitForCompletion();

                if (sortingGroup != null) sortingGroup.sortingOrder = 30;
            }
            else
            {
                if (sortingGroup != null) sortingGroup.sortingOrder = 30;
                card.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
                card.transform.DOMove(splinePosition, duration).SetEase(Ease.OutCubic);
                card.transform.DORotate(rotation.eulerAngles, duration).SetEase(Ease.OutCubic);
            }
        }

        if (arrivingCard == null)
            yield return new WaitForSeconds(duration);
    }
    
}
