using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaUI : MonoBehaviour
{
    public const int BaseTurnMana = 5;

    [SerializeField] private TMP_Text mana;
    [SerializeField] private Image[] manaBubbles = new Image[BaseTurnMana];
    private static readonly Color SpentManaTint = new Color(0.22f, 0.27f, 0.35f, 0.82f);
    private readonly List<Image> overflowBubbles = new();
    private RectTransform bubbleContainer;
    private float baseContainerWidth;
    private float bubbleStep = 23f;
    private Vector2 overflowStartPosition;

    private void Awake()
    {
        // This was the old "03" label below the hand. Keep its serialized
        // reference for compatibility, but do not show it in battle.
        if (mana != null) mana.gameObject.SetActive(false);
        bubbleContainer = transform as RectTransform;
        if (bubbleContainer != null) baseContainerWidth = bubbleContainer.sizeDelta.x;
        if (manaBubbles != null && manaBubbles.Length > 0 && manaBubbles[manaBubbles.Length - 1] != null)
        {
            RectTransform last = manaBubbles[manaBubbles.Length - 1].rectTransform;
            overflowStartPosition = last.anchoredPosition;
            if (manaBubbles.Length > 1 && manaBubbles[manaBubbles.Length - 2] != null)
                bubbleStep = Mathf.Max(last.rect.width + 3f,
                    last.anchoredPosition.x - manaBubbles[manaBubbles.Length - 2].rectTransform.anchoredPosition.x);
        }
        UpdateManaText(BaseTurnMana);
    }

    public void UpdateManaText(int currentMana)
    {
        currentMana = Mathf.Max(0, currentMana);
        int baseBubbleCount = manaBubbles != null ? manaBubbles.Length : 0;
        int neededOverflow = Mathf.Max(0, currentMana - baseBubbleCount);
        EnsureOverflowBubbles(neededOverflow);

        if (manaBubbles != null)
        {
            for (int i = 0; i < manaBubbles.Length; i++)
            {
                if (manaBubbles[i] != null)
                    manaBubbles[i].color = i < currentMana ? Color.white : SpentManaTint;
            }
        }

        for (int i = 0; i < overflowBubbles.Count; i++)
        {
            Image bubble = overflowBubbles[i];
            if (bubble != null)
                bubble.color = i < neededOverflow ? Color.white : SpentManaTint;
        }

        if (bubbleContainer != null)
        {
            float extraWidth = neededOverflow > 0 ? bubbleStep * neededOverflow : 0f;
            bubbleContainer.sizeDelta = new Vector2(baseContainerWidth + extraWidth, bubbleContainer.sizeDelta.y);
        }

        // Retain a sensible value for the hidden legacy label in case another
        // scene or prefab still reads it.
        if (mana != null) mana.text = currentMana.ToString("D2");
    }

    private void EnsureOverflowBubbles(int count)
    {
        if (manaBubbles == null || manaBubbles.Length == 0 || manaBubbles[manaBubbles.Length - 1] == null)
            return;

        Image template = manaBubbles[manaBubbles.Length - 1];
        while (overflowBubbles.Count < count)
        {
            GameObject clone = Instantiate(template.gameObject, template.transform.parent);
            clone.name = "Mana Overflow Bubble " + (overflowBubbles.Count + 1);
            Image image = clone.GetComponent<Image>();
            if (image == null)
            {
                Destroy(clone);
                break;
            }
            image.rectTransform.anchoredPosition = overflowStartPosition + Vector2.right * bubbleStep * (overflowBubbles.Count + 1);
            overflowBubbles.Add(image);
        }

        while (overflowBubbles.Count > count)
        {
            int last = overflowBubbles.Count - 1;
            Image bubble = overflowBubbles[last];
            overflowBubbles.RemoveAt(last);
            if (bubble != null) Destroy(bubble.gameObject);
        }
    }
}
