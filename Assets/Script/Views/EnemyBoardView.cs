using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemyBoardView : MonoBehaviour
{
    [SerializeField] private List<Transform> slots;
    [SerializeField, Min(0.5f)] private float additionalEnemySpacing = 1.5f;
    public List<EnemyView> EnemyViews {get; private set; } = new();
    public void AddEnemy(EnemyData enemyData)
    {
        if (enemyData == null) return;

        if (slots != null && EnemyViews.Count < slots.Count && slots[EnemyViews.Count] != null)
        {
            Transform slot = slots[EnemyViews.Count];
            EnemyView enemyView = EnemyViewCreator.Instance.CreateEnemyView(enemyData, slot.position, slot.rotation);
            enemyView.transform.SetParent(slot, true);
            EnemyViews.Add(enemyView);
            return;
        }

        // Scenes may define only one slot. Place additional squad members beside
        // the first slot so encounter assets can still spawn full enemy groups.
        Transform anchor = slots != null && slots.Count > 0 ? slots[0] : transform;
        Vector3 position = anchor.position + Vector3.left * additionalEnemySpacing * (EnemyViews.Count - (slots?.Count ?? 0) + 1);
        EnemyView additionalEnemy = EnemyViewCreator.Instance.CreateEnemyView(enemyData, position, anchor.rotation);
        additionalEnemy.transform.SetParent(transform, true);
        EnemyViews.Add(additionalEnemy);
    }
    public IEnumerator RemoveEnemy(EnemyView enemyView)
    {
        if (enemyView == null || !EnemyViews.Contains(enemyView)) yield break;
        EnemyViews.Remove(enemyView);
        Tween tween = enemyView.transform.DOScale(Vector3.zero, 0.25f);
        yield return tween.WaitForCompletion();
        Destroy(enemyView.gameObject);
    }
}
