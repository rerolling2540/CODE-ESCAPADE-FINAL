using UnityEngine;
using TMPro;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private SpriteRenderer imageSR;
    [SerializeField] private GameObject wrapper;
    [SerializeField] private LayerMask dropLayer;
    public Card Card { get; private set; }
    private Vector3 draStartPosition;
    private Quaternion dragStartRotation;
    private static Sprite medievalBackground;

    void Awake()
    {
        ApplyMedievalTheme();
    }

    void ApplyMedievalTheme()
    {
        var background = wrapper != null ? wrapper.transform.Find("CardBackground") : null;
        var renderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
        if (renderer != null && renderer.sprite != null)
        {
            if (medievalBackground == null)
            {
                const int width = 300, height = 360;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        if (x < 57 || x > 243 || y < 57 || y > 303) continue;
                        int edge = Mathf.Min(Mathf.Min(x - 57, 243 - x), Mathf.Min(y - 57, 303 - y));
                        int grain = ((x * 17 + y * 31) % 9) - 4;
                        Color32 colour = new Color32((byte)(221 + grain), (byte)(201 + grain), (byte)(158 + grain), 255);
                        if (edge < 3) colour = new Color32(53, 30, 15, 255);
                        else if (edge < 6) colour = new Color32(183, 137, 61, 255);
                        else if (edge < 9) colour = new Color32(89, 55, 28, 255);
                        if (x > 67 && x < 233 && y > 184 && y < 290)
                            colour = new Color32(164, 146, 108, 255);
                        if (y >= 142 && y <= 175)
                            colour = y < 145 || y > 172 ? new Color32(134, 91, 35, 255) : new Color32(235, 214, 167, 255);
                        if ((x - 82) * (x - 82) + (y - 280) * (y - 280) < 20 * 20)
                            colour = new Color32(102, 63, 29, 255);
                        int cornerDistanceX = Mathf.Min(Mathf.Abs(x - 66), Mathf.Abs(x - 234));
                        int cornerDistanceY = Mathf.Min(Mathf.Abs(y - 66), Mathf.Abs(y - 294));
                        if (cornerDistanceX + cornerDistanceY < 5)
                            colour = new Color32(244, 204, 107, 255);
                        pixels[y * width + x] = colour;
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                medievalBackground = Sprite.Create(texture, new Rect(0, 0, width, height),
                    new Vector2(0.5f, 0.5f), width / renderer.sprite.bounds.size.x);
            }
            renderer.sprite = medievalBackground;
            renderer.color = Color.white;
        }
        foreach (var label in new[] { title, description, mana })
        {
            if (label == null) continue;
            label.color = new Color(0.2f, 0.12f, 0.055f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = label == description ? 25 : 32;
        }
        if (title != null)
        {
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.sizeDelta = new Vector2(29, 4);
        }
        if (description != null) description.rectTransform.sizeDelta = new Vector2(28, 10);
        if (mana != null) mana.color = new Color(1f, 0.88f, 0.56f);
    }

    public void Setup(Card card)
    {
        Card = card;
        title.text = card.Title;
        description.text = card.Description;
        mana.text = card.Mana.ToString();
        imageSR.sprite = card.Image;
        if (imageSR.sprite != null)
        {
            // The RPG Icons pack uses 24px sprites. Enlarge those icons to fit the card art window.
            float sourceSize = Mathf.Max(imageSR.sprite.bounds.size.x, imageSR.sprite.bounds.size.y);
            float iconScale = sourceSize < 0.6f ? 6f : 1f;
            imageSR.transform.localScale = Vector3.one * iconScale;
        }
    }

    void OnMouseEnter()
    {
        if (!Interactions.Instance.PlayerCanHover()) return;
        wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.Show(Card, pos);
    }

    void OnMouseExit()
    {
        if (!Interactions.Instance.PlayerCanHover()) return;
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }

    void OnMouseDown()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1)) return;
        if (Card.ManualTargetEffect != null)
        {
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
            Interactions.Instance.PlayerIsDragging = true;
            wrapper.SetActive(true);
            CardViewHoverSystem.Instance.Hide();
            draStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = mousePosition;
        }
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (Card.ManualTargetEffect != null) return;
        if (MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1))
            transform.position = mousePosition;
    }

    void OnMouseUp()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 mousePosition, -1))
        {
            if (Card.ManualTargetEffect != null) ManualTargetSystem.Instance.CancelTargeting();
            else
            {
                transform.position = draStartPosition;
                transform.rotation = dragStartRotation;
            }
            Interactions.Instance.PlayerIsDragging = false;
            return;
        }
        if (Card.ManualTargetEffect != null)
        {
            EnemyView target = ManualTargetSystem.Instance.EndTargeting(mousePosition);
            if (target != null && ManaSystem.Instance.HasEnoughMana(Card.Mana))
            {
                PlayCardGA playCard = new(Card, target);
                ActionSystem.Instance.Perform(playCard);
            }
        }
        else
        {
            if (ManaSystem.Instance.HasEnoughMana(Card.Mana)
            && Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer))
            {
                PlayCardGA playCardGA = new(Card);

                QuestionSystem.Instance.AskRandomQuestion(correct =>
                {
                    playCardGA.QuestionAnsweredCorrectly = correct;
                    ActionSystem.Instance.Perform(playCardGA);
                });
            }
            else
            {
                transform.position = draStartPosition;
                transform.rotation = dragStartRotation;
            }
            Interactions.Instance.PlayerIsDragging = false;
        }
    }
}
