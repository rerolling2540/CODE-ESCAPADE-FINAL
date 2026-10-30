using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaUI : MonoBehaviour
{
    public const int MaximumMana = 5;

    [SerializeField] private TMP_Text mana;
    [SerializeField] private Image[] manaBubbles = new Image[MaximumMana];
    private static readonly Color SpentManaTint = new Color(0.22f, 0.27f, 0.35f, 0.82f);

    private void Awake()
    {
        // This was the old "03" label below the hand. Keep its serialized
        // reference for compatibility, but do not show it in battle.
        if (mana != null) mana.gameObject.SetActive(false);
        UpdateManaText(MaximumMana);
    }

    public void UpdateManaText(int currentMana)
    {
        currentMana = Mathf.Clamp(currentMana, 0, MaximumMana);

        if (manaBubbles != null)
        {
            for (int i = 0; i < manaBubbles.Length; i++)
            {
                if (manaBubbles[i] != null)
                    manaBubbles[i].color = i < currentMana ? Color.white : SpentManaTint;
            }
        }

        // Retain a sensible value for the hidden legacy label in case another
        // scene or prefab still reads it.
        if (mana != null) mana.text = currentMana.ToString("D2");
    }
}
