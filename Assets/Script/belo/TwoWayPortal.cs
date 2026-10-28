
using UnityEngine;
using System.Collections;
using System.Linq;

public class TwoWayPortal : MonoBehaviour
{
    public Transform destination;
    [Min(0)] public int requiredDefeatedEncounters;
    public bool hideUntilUnlocked = true;

    private static bool isTeleporting = false;
    private Renderer[] portalVisuals;
    private bool[] originalVisualStates;
    private bool requiredVictoriesMet;
    private float nextRequirementRefresh;

    private void Start()
    {
        // Nested portals are independent routes; never hide another portal's artwork.
        portalVisuals = GetComponentsInChildren<Renderer>(true)
            .Where(r => r.GetComponentInParent<TwoWayPortal>() == this &&
                (r.GetComponentInParent<PortalNextScene>() == null ||
                 r.GetComponentInParent<PortalNextScene>().gameObject == gameObject)).ToArray();
        originalVisualStates = new bool[portalVisuals.Length];
        for (int i = 0; i < portalVisuals.Length; i++)
            originalVisualStates[i] = portalVisuals[i].enabled;
        RefreshRequirement();
        RefreshPortalVisibility();

        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isTeleporting && HasRequiredVictories() && destination != null,
            player => StartCoroutine(Teleport(player)));
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRequirementRefresh)
            RefreshRequirement();
        RefreshPortalVisibility();
    }

    private void RefreshRequirement()
    {
        nextRequirementRefresh = Time.unscaledTime + 0.5f;
        var saved = requiredDefeatedEncounters > 0 ? SaveSlots.Read(SaveSlots.Active) : null;
        requiredVictoriesMet = requiredDefeatedEncounters <= 0 ||
            ElfQuestProgress.Cleared(saved) >= requiredDefeatedEncounters;
    }

    private bool HasRequiredVictories() => requiredVictoriesMet;

    private void RefreshPortalVisibility()
    {
        if (portalVisuals == null) return;
        for (int i = 0; i < portalVisuals.Length; i++)
            if (portalVisuals[i] != null)
                portalVisuals[i].enabled = originalVisualStates[i] && (!hideUntilUnlocked || requiredVictoriesMet);
    }

    private IEnumerator Teleport(Transform player)
    {
        isTeleporting = true;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (destination != null)
        {
            if (rb != null)
            {
                rb.position = destination.position;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                player.position = destination.position;
            }
            PlayerDeathFlow.RecordCheckpoint(new Vector3(destination.position.x, destination.position.y, player.position.z));
        }

        yield return new WaitForSeconds(0.5f);
        isTeleporting = false;
    }
}
