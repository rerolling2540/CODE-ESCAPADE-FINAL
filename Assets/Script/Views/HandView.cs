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
        if (cards.Count == 0 || splineContainer == null) yield break;

        // Spread larger hands across more of the spline and scale them down gently.
        float cardSpacing = cards.Count > 1 ? Mathf.Min(0.14f, 0.88f / (cards.Count - 1)) : 0f;
        float firstCardPosition = 0.5f - (cards.Count - 1) * cardSpacing / 2;
        Spline spline = splineContainer.Spline;
        float scaleFactor = Mathf.Max(0.72f, 1f - Mathf.Max(0, cards.Count - 5) * 0.08f);
        float extraLift = Mathf.Min(0.6f, Mathf.Max(0, cards.Count - 5) * 0.2f);

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

            card.transform.DOKill();
            if (card == arrivingCard)
            {
                // Keep the drawn card above the pile icon while it flies, then
                // restore normal card sorting once it settles into the hand.
                SortingGroup sortingGroup = card.GetComponent<SortingGroup>();
                if (sortingGroup != null) sortingGroup.sortingOrder = 45;

                Tween arrival = card.transform.DOJump(splinePosition, 0.55f, 1, duration)
                    .SetEase(Ease.OutCubic);
                card.transform.DORotate(rotation.eulerAngles, duration)
                    .SetEase(Ease.OutCubic);
                card.transform.DOScale(targetScale, duration * 0.72f)
                    .SetEase(Ease.OutBack);

                yield return arrival.WaitForCompletion();

                if (sortingGroup != null) sortingGroup.sortingOrder = 0;
            }
            else
            {
                card.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
                card.transform.DOMove(splinePosition, duration).SetEase(Ease.OutCubic);
                card.transform.DORotate(rotation.eulerAngles, duration).SetEase(Ease.OutCubic);
            }
        }

        if (arrivingCard == null)
            yield return new WaitForSeconds(duration);
    }
    
}
