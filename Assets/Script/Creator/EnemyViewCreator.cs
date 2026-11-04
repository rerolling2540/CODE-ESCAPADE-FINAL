using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class EnemyViewCreator : Singleton<EnemyViewCreator>
{
   [SerializeField] private EnemyView enemyViewPrefab;
   public EnemyView CreateEnemyView(EnemyData enemyData, Vector3 position, Quaternion rotation,string sourceScene=null)
   {
        EnemyView enemyView = Instantiate(enemyViewPrefab, position, rotation);
        enemyView.SetUp(enemyData,sourceScene);
        return enemyView;
   } 
}
