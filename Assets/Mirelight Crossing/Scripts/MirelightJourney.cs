using UnityEngine;

/// <summary>Local camera and recovery setup for the standalone Mirelight map.</summary>
public sealed class MirelightJourney : MonoBehaviour
{
    public Transform player;
    public Transform[] checkpoints;
    public Camera view;
    public float waterline = -2.8f;
    public Vector2 cameraLimits = new Vector2(8, 122);
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
            if (Vector2.Distance(player.position, checkpoints[i].position) < 2.2f)
            {
                checkpoint = i;
                safePosition = checkpoints[i].position;
            }
        }
        if (player.position.y < waterline || player.position.x < -8 || player.position.x > 136)
        {
            player.position = safePosition;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        var target = new Vector3(Mathf.Clamp(player.position.x + 3, cameraLimits.x, cameraLimits.y),
            Mathf.Clamp(player.position.y + 2, 5, 13), -10);
        view.transform.position = Vector3.SmoothDamp(view.transform.position, target, ref cameraVelocity, .22f);
    }
}
