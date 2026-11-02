using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WardenQuestPolish
{
    public static string Apply()
    {
        const string root="Assets/Prefab/NPC/Warden Journey/";
        string firePath=root+"Watchfire demonstration.prefab";var fire=PrefabUtility.LoadPrefabContents(firePath);
        var aid=fire.GetComponent<WardenVisualAid>();aid.fireFrames=AssetDatabase.LoadAllAssetsAtPath("Assets/Tiny Swords/Particle FX/Fire_02.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        foreach(var s in aid.symbols){s.sprite=aid.fireFrames[0];s.transform.localScale=Vector3.one*2;}
        PrefabUtility.SaveAsPrefabAsset(fire,firePath);PrefabUtility.UnloadPrefabContents(fire);
        string path=root+"Warden Final Trial.prefab";var final=PrefabUtility.LoadPrefabContents(path);var encounter=final.GetComponent<CaveEnemyEncounter>();
        string enemyPath=root+"Warden Training Sentinel.asset";var enemy=AssetDatabase.LoadAssetAtPath<EnemyData>(enemyPath);
        if(enemy==null){enemy=Object.Instantiate(encounter.enemy);enemy.name="Warden Training Sentinel";AssetDatabase.CreateAsset(enemy,enemyPath);}
        var so=new SerializedObject(enemy);so.FindProperty("<Health>k__BackingField").intValue=70;so.FindProperty("<AttackPower>k__BackingField").intValue=3;so.FindProperty("experienceReward").intValue=40;so.ApplyModifiedPropertiesWithoutUndo();
        encounter.enemy=enemy;var squad=new SerializedObject(encounter.battleEncounter);var list=squad.FindProperty("enemies");for(int i=0;i<list.arraySize;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=enemy;squad.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(final,path);PrefabUtility.UnloadPrefabContents(final);AssetDatabase.SaveAssets();return "Animated Tiny Swords watchfires and two low-damage Warden sentinels saved.";
    }
}
