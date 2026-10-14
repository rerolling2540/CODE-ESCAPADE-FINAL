using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the enemy squad spawned for a battle encounter.
/// The overworld encounter can remain a single enemy while this asset supplies
/// multiple enemies when that encounter transitions into battle.
/// </summary>
[CreateAssetMenu(fileName = "New Encounter", menuName = "Data/Encounter")]
public class EncounterData : ScriptableObject
{
    [SerializeField] private List<EnemyData> enemies = new List<EnemyData>();

    public List<EnemyData> CreateEnemySquad()
    {
        return enemies == null
            ? new List<EnemyData>()
            : enemies.FindAll(enemy => enemy != null);
    }
}
