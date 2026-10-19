
using UnityEngine;
using System.Collections;

public class TwoWayPortal : MonoBehaviour
{
    public Transform destination;

    private static bool isTeleporting = false;

    private void Start()
    {
        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isTeleporting && destination != null,
            player => StartCoroutine(Teleport(player)));
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
        }

        yield return new WaitForSeconds(0.5f);
        isTeleporting = false;
    }
}
