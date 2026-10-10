using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;

    [SerializeField] private float pressedScale = 0.9f;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}