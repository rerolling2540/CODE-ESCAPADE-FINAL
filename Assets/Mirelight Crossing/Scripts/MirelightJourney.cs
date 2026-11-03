using UnityEngine;

/// <summary>Local camera and recovery setup for the standalone Mirelight map.</summary>
public sealed class MirelightJourney : MonoBehaviour
{
    public Transform player;
    public Transform[] checkpoints;
    public Camera view;
    public float waterline = -2.8f;
    public Vector2 cameraLimits = new Vector2(8, 122);
    [Tooltip("When disabled, the camera keeps following the player at both horizontal ends of the map.")]
    public bool limitHorizontalCamera = true;
    public Vector2 recoveryLimits = new Vector2(-8, 136);
    public Vector2 cameraHeightLimits = new Vector2(5, 13);
    private Vector3 safePosition;
    private int checkpoint;
    private Vector3 cameraVelocity;

    private void Start()
    {
        if (player == null || view == null) { enabled = false; return; }
        safePosition = player.position;
    }

    private void LateUpdate()
    {
        for (int i = checkpoint + 1; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] != null && Vector2.Distance(player.position, checkpoints[i].position) < 2.2f)
            {
                checkpoint = i;
                safePosition = checkpoints[i].position;
            }
        }
        if (player.position.y < waterline || player.position.x < recoveryLimits.x || player.position.x > recoveryLimits.y)
        {
            player.position = safePosition;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        var target = CameraTargetFor(player.position);
        view.transform.position = Vector3.SmoothDamp(view.transform.position, target, ref cameraVelocity, .22f);
    }

    public Vector3 CameraTargetFor(Vector3 position)
    {
        float x = position.x + 3;
        if (limitHorizontalCamera) x = Mathf.Clamp(x, cameraLimits.x, cameraLimits.y);
        return new Vector3(x, Mathf.Clamp(position.y + 2, cameraHeightLimits.x, cameraHeightLimits.y), -10);
    }
}
