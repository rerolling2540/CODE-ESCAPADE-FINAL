using UnityEngine;

/// <summary>Saved progression shared by exploration, battle and character information.</summary>
public static class PlayerProgression
{
    public static int RequiredExperience(int level)=>100+50*(Mathf.Max(1,level)-1);
    public static int Level
    {
        get{var saved=SaveSlots.Read(SaveSlots.Active);if(saved==null)return 5;Ensure(saved);return saved.level;}
    }
    public static int Experience=>Mathf.Max(0,SaveSlots.Read(SaveSlots.Active)?.experience??0);
    public static HeroClassData Class=>Resources.Load<CharacterInfoData>("UI/CharacterInfo")?.battleClass;
    private static void Ensure(SaveSlots.Record saved)
    {
        if(saved.level>=5)return;
        saved.level=5;
        SaveSlots.Write(saved);
    }
    public static int AwardEncounter(string encounterId,int amount)
    {
        var saved=SaveSlots.Read(SaveSlots.Active);
        if(saved==null||string.IsNullOrEmpty(encounterId))return 0;
        Ensure(saved);
        if(saved.defeatedEncounters==null)saved.defeatedEncounters=new System.Collections.Generic.List<string>();
        if(saved.defeatedEncounters.Contains(encounterId))return 0;
        saved.defeatedEncounters.Add(encounterId);
        ApplyExperience(saved,Mathf.Max(0,amount));SaveSlots.Write(saved);return Mathf.Max(0,amount);
    }
    public static void GainExperience(int amount)
    {
        var saved=SaveSlots.Read(SaveSlots.Active);if(saved==null||amount<=0)return;
        Ensure(saved);ApplyExperience(saved,amount);SaveSlots.Write(saved);
    }
    private static void ApplyExperience(SaveSlots.Record saved,int amount)
    {
        saved.experience=(int)System.Math.Min(int.MaxValue,(long)Mathf.Max(0,saved.experience)+amount);
        int before=saved.level;
        while(saved.level<100&&saved.experience>=RequiredExperience(saved.level))
        {saved.experience-=RequiredExperience(saved.level);saved.level++;}
        if(saved.level>=100)saved.experience=0;
        var heroClass=Class;
        if(saved.level!=before&&heroClass!=null)
        {
            int maximum=heroClass.GetHealthForLevel(saved.level);
            int previous=saved.maxHealth>0?saved.maxHealth:heroClass.GetHealthForLevel(before);
            saved.currentHealth=Mathf.Clamp(saved.currentHealth+maximum-previous,0,maximum);saved.maxHealth=maximum;
        }
        if(saved.level>before&&Application.isPlaying)LevelUpCelebration.Request();
    }
}
