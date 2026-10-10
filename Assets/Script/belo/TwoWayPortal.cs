
using UnityEngine;
using System.Collections;

public class TwoWayPortal : MonoBehaviour
{
    public Transform destination;

    private static bool isTeleporting = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTeleporting || !other.CompareTag("Player"))
            return;

        StartCoroutine(Teleport(other.transform));
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
