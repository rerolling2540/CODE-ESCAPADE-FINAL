using UnityEngine;

/// <summary>Fixed map progression, independent of player level and shared asset mutation.</summary>
public static class EnemyMapBalance
{
    static readonly int[] Health={100,180,300,460},Experience={45,85,140,210},Gold={15,30,50,75};
    public readonly struct Stats
    {
        public readonly int health,experience,gold;
        public Stats(int health,int experience,int gold){this.health=health;this.experience=experience;this.gold=gold;}
    }
    public static int MapIndex(string scene)
    {
        scene=MapSceneNames.Resolve(scene);
        return scene=="Whispering Hollow"?0:scene=="Shadowroot Depths"?1:scene=="Frostbound Passage"?2:scene=="Forest of Dawn"?3:-1;
    }
    public static Stats Resolve(EnemyData enemy,string scene)
    {
        if(enemy==null)return new Stats(1,0,0);
        int map=MapIndex(scene);
        if(map<0)return new Stats(Mathf.Max(1,enemy.Health),Mathf.Max(0,enemy.experienceReward),Mathf.Max(0,enemy.goldReward));
        if(enemy.progressionRank==EnemyProgressionRank.Tutorial)return new Stats(Mathf.Max(1,enemy.Health),Mathf.Max(0,enemy.experienceReward),5);
        // The final learning encounter has two low-damage sentinels, rather than a lethal boss.
        if(enemy.progressionRank==EnemyProgressionRank.LearningTrial)return new Stats(190,140,50);
        float weight=Mathf.Clamp(enemy.progressionWeight,.75f,1.5f);
        float hp=enemy.progressionRank==EnemyProgressionRank.Boss?3.5f:enemy.progressionRank==EnemyProgressionRank.Elite?1.8f:1f;
        float reward=enemy.progressionRank==EnemyProgressionRank.Boss?3f:enemy.progressionRank==EnemyProgressionRank.Elite?1.75f:1f;
        int health=Health[map],xp=Experience[map],gold=Gold[map];
        return new Stats(Mathf.RoundToInt(health*hp*weight),Mathf.RoundToInt(xp*reward*weight),Mathf.RoundToInt(gold*reward*weight));
    }
}
