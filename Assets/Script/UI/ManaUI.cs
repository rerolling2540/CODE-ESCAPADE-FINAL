using TMPro;
using UnityEngine;

public class ManaUI : MonoBehaviour
{
    [SerializeField] private TMP_Text mana;

    private void Awake()
    {
        if (mana == null) return;
        mana.alignment = TextAlignmentOptions.Center;
        mana.fontStyle = FontStyles.Bold;
        mana.fontSize = 64f;
        mana.color = new Color(0.78f, 0.96f, 1f);
        mana.outlineColor = new Color(0.12f, 0.62f, 1f, 1f);
        mana.outlineWidth = 0.28f;
    }

    public void UpdateManaText(int currentMana)
    {
        if (mana != null) mana.text = currentMana.ToString("D2");
    }
}
