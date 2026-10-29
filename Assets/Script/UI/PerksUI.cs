using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerksUI : MonoBehaviour
{
    [SerializeField] private PerkUI perkUIPrefab;
    private readonly List<PerkUI> perkUIs = new();
    public void AddPerkUI(Perk perk)
    {
        PerkUI perkUI = Instantiate(perkUIPrefab, transform);
        RectTransform iconRect = perkUI.GetComponent<RectTransform>();
        if (iconRect != null) iconRect.sizeDelta = new Vector2(30, 30);
        var iconImage = perkUI.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (iconImage != null)
        {
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        perkUI.SetUp(perk);
        perkUIs.Add(perkUI);
    }
    public void RemovePerkUI(Perk perk)
    {
        PerkUI perkUI = perkUIs.Where(pui => pui.Perk == perk).FirstOrDefault();
        if (perkUI != null)
        {
            perkUIs.Remove(perkUI);
            Destroy(perkUI.gameObject);
        }
    }
}
