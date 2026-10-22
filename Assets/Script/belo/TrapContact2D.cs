using UnityEngine;

/// <summary>Forwards dynamically generated tile hazard overlaps to the Map 4 recovery system.</summary>
public sealed class TrapContact2D : MonoBehaviour
{
    private FallTrapCheckpointSystem owner;

    public void Initialize(FallTrapCheckpointSystem system)
    {
        owner = system;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.HandleTrapContact(other);
    }
}
