using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class NPCSpriteAnimation : MonoBehaviour
{
    public Sprite[] frames;
    [Min(1)] public float framesPerSecond = 7f;
    private SpriteRenderer visual;
    void Awake() { visual = GetComponent<SpriteRenderer>(); }
    void Update()
    {
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length];
    }
}
