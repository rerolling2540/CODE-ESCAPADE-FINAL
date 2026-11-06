using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>Receives a single cross-scene portal trip at an authored landing point.</summary>
public sealed class PortalArrivalPoint : MonoBehaviour
{
    public string arrivalId;
    public Transform landingPoint;
    public bool requireSafeLanding;
    private static string pendingScene;
    private static string pendingArrival;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPending()
    {
        pendingScene = null;
        pendingArrival = null;
    }

    public static void Prepare(string sceneName, string destinationId)
    {
        pendingScene = string.IsNullOrWhiteSpace(destinationId) ? null : MapSceneNames.Resolve(sceneName);
        pendingArrival = destinationId;
    }

    private IEnumerator Start()
    {
        if (pendingScene != gameObject.scene.name || string.IsNullOrEmpty(arrivalId) ||
            pendingArrival != arrivalId) yield break;
        ResetPending();
        // Let the scene's character spawner finish before positioning its player.
        yield return null;
        playerMovement player = null;
        float deadline = Time.realtimeSinceStartup + 5f;
        while (player == null && Time.realtimeSinceStartup < deadline)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                player = root.GetComponentInChildren<playerMovement>();
                if (player != null) break;
            }
            if (player == null) yield return null;
        }
        if (player == null)
        {
            Debug.LogError("Portal arrival could not find the destination player.", this);
            yield break;
        }

        Vector3 destination = landingPoint != null ? landingPoint.position : transform.position;
        destination.z = player.transform.position.z;
        Physics2D.SyncTransforms();
        var collider = player.GetComponent<Collider2D>();
        float feetOffset = collider != null ? collider.bounds.min.y - player.transform.position.y : -0.8f;
        foreach (var hit in Physics2D.RaycastAll(destination + Vector3.up * 0.5f, Vector2.down, 6f))
        {
            if (hit.collider == null || hit.collider.isTrigger ||
                hit.transform.IsChildOf(player.transform) || hit.normal.y < 0.5f) continue;
            destination.y = hit.point.y - feetOffset + 0.06f;
            break;
        }

        if(requireSafeLanding&&!SafeCheckpoint.TryFind(player,destination,out destination))
        {Debug.LogWarning("No safe monolith landing found. Keeping the map's player spawn.",this);yield break;}
        Vector3 delta = destination - player.transform.position;
        player.transform.position = destination;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = destination;
            body.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();
        foreach (var camera in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            if (camera.Follow == player.transform) camera.OnTargetObjectWarped(player.transform, delta);
        PlayerDeathFlow.RecordCheckpoint(destination);
        if(requireSafeLanding)GameSave.EnsureInstance().SaveCurrentPlayer();
    }
}
