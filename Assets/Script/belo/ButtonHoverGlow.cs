
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonTextGlow : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Settings")]
    public TMP_Text buttonText;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    [Header("Outline Settings")]
    public Outline outline;
    public Color normalOutlineColor = new Color(1f, 0.7f, 0.2f, 0.3f);
    public Color hoverOutlineColor = new Color(1f, 0.8f, 0.2f, 1f);

    public Vector2 normalDistance = new Vector2(2, -2);
    public Vector2 hoverDistance = new Vector2(7, -7);

    void Start()
    {
        if (buttonText != null)
            buttonText.color = normalColor;

        if (outline != null)
        {
            outline.effectColor = normalOutlineColor;
            outline.effectDistance = normalDistance;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = hoverColor;

        if (outline != null)
        {
            outline.effectColor = hoverOutlineColor;
            outline.effectDistance = hoverDistance;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = normalColor;

        if (outline != null)
        {
            outline.effectColor = normalOutlineColor;
            outline.effectDistance = normalDistance;
        }
    }
}
