using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyCounterattackChecks
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Use Edit Mode for isolated counterattack checks.");
        int count=0;Action<bool,string> check=(v,m)=>{if(!v)throw new Exception(m);count++;};
        string before=JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active));
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var opening in new[]{EnemyOpeningAction.Shield,EnemyOpeningAction.DamageBuff})
            foreach(var style in new[]{EnemyAttackStyle.Melee,EnemyAttackStyle.Ranged})
            {
                var source=AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Cave Goblin.asset");
                var data=UnityEngine.Object.Instantiate(source);
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EnemyView.prefab"));
                SceneManager.MoveGameObjectToScene(go,preview);
                try
                {
                    data.openingAction=opening;data.attackStyle=style;data.appliesStatusEffect=true;
                    var enemy=go.GetComponent<EnemyView>();enemy.SetUp(data,"Scene3");
                    // Exercise combat logic without generating transient status UI or playing VFX.
                    typeof(CombatantView).GetField("statusEffectsUI",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,null);
                    var counter=new AttackHeroGA(enemy,isCounterattack:true);
                    var normal=new AttackHeroGA(enemy);
                    check(!enemy.OpeningActionResolved&&!normal.CanAttack,"Normal opening behaviour changed");
                    check(counter.CanAttack,"Wrong answer cannot retaliate before opening buff");
                    check(!enemy.OpeningActionResolved,"Counterattack skipped the opening buff permanently");
                    enemy.ResolveOpeningAction();
                    check(enemy.OpeningActionResolved&&counter.CanAttack&&normal.CanAttack,"Buff blocks subsequent attacks");
                    check(opening==EnemyOpeningAction.Shield?enemy.GetStatusEffectStacks(StatusEffectType.ARMOR)>0:enemy.AttackPower>data.AttackPower,"Opening buff did not apply");
                    enemy.ApplyBreakDamage(enemy.MaxBreakMeter);
                    check(!counter.CanAttack&&!normal.CanAttack,"Broken enemy can retaliate");
                    enemy.RestoreBreakMeter();
                    check(counter.CanAttack,"Recovered enemy cannot retaliate");
                    typeof(CombatantView).GetField("<CurrentHealth>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,0);
                    check(!counter.CanAttack&&!normal.CanAttack,"Dead enemy can retaliate");
                }
                finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(data);}
            }
            check(!new AttackHeroGA(null,true).CanAttack,"Missing attacker can retaliate");
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        check(before==JsonUtility.ToJson(SaveSlots.Read(SaveSlots.Active)),"Player save changed");
        return count+" counterattack checks passed: shield/damage openings, melee/ranged, status-enabled enemies, first-action retaliation, normal opening preservation, stagger and death. Player save unchanged.";
    }
}
