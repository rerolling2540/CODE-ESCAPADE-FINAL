using UnityEngine;

public class ManualTargetSystem : Singleton<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;
    private bool isTargeting;
    private EnemyView hoveredEnemy;
    private EnemyView selectedEnemy;

    private void Update()
    {
        if (!isTargeting) return;

        EnemyView enemyUnderCursor = FindEnemyUnderCursor();
        if (enemyUnderCursor == hoveredEnemy) return;
        if (hoveredEnemy != null) hoveredEnemy.SetTargetHighlighted(false);
        hoveredEnemy = enemyUnderCursor;
        if (hoveredEnemy != null) hoveredEnemy.SetTargetHighlighted(true);
    }

    public void StartTargeting(Vector3 startPosition)
    {
        ClearTargetHighlight();
        isTargeting = true;
        if (arrowView != null)
        {
            arrowView.gameObject.SetActive(true);
            arrowView.SetupArrow(startPosition);
        }
    }

    public EnemyView EndTargeting(Vector3 endPosition)
    {
        isTargeting = false;
        if (arrowView != null) arrowView.gameObject.SetActive(false);

        selectedEnemy = FindEnemyUnderCursor() ?? hoveredEnemy;
        if (hoveredEnemy != null && hoveredEnemy != selectedEnemy)
            hoveredEnemy.SetTargetHighlighted(false);
        hoveredEnemy = null;

        if (selectedEnemy != null) selectedEnemy.SetTargetHighlighted(true);
        return selectedEnemy;
    }

    public void CancelTargeting()
    {
        isTargeting = false;
        if (arrowView != null) arrowView.gameObject.SetActive(false);
        ClearTargetHighlight();
    }

    public void ClearTargetHighlight()
    {
        isTargeting = false;
        if (hoveredEnemy != null) hoveredEnemy.SetTargetHighlighted(false);
        if (selectedEnemy != null && selectedEnemy != hoveredEnemy)
            selectedEnemy.SetTargetHighlighted(false);
        hoveredEnemy = null;
        selectedEnemy = null;
    }

    private EnemyView FindEnemyUnderCursor()
    {
        Camera camera = Camera.main;
        if (camera == null) return null;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        // Filter by EnemyView rather than relying only on the scene's layer mask;
        // some spawned enemy prefabs have their colliders on a child/default layer.
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            EnemyView enemy = hit.collider.GetComponentInParent<EnemyView>();
            if (enemy != null && enemy.CurrentHealth > 0) return enemy;
        }

        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, 100f);
        System.Array.Sort(hits2D, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D hit in hits2D)
        {
            if (hit.collider == null) continue;
            EnemyView enemy = hit.collider.GetComponentInParent<EnemyView>();
            if (enemy != null && enemy.CurrentHealth > 0) return enemy;
        }

        return null;
    }
}
