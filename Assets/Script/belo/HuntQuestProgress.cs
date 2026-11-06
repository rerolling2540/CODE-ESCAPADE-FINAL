using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Combat contracts use permanent unique encounter victories, not a map's respawning visit counters.</summary>
public static class HuntQuestProgress
{
    public const int Stages=3, ReturnGiverOffset=3;
    public sealed class Contract
    {
        public readonly string title,story,target,advice;
        public readonly string[] encounterIds;
        public readonly int required,xp,gold,scraps,tokens;
        public Contract(string title,string story,string target,string advice,string[] ids,int required,int xp,int gold,int scraps=0,int tokens=0)
        {this.title=title;this.story=story;this.target=target;this.advice=advice;encounterIds=ids;this.required=required;this.xp=xp;this.gold=gold;this.scraps=scraps;this.tokens=tokens;}
    }
    public static string Name(int giver)=>giver==0?"Eleonore":"Joanna";
    public static string Scene(int giver)=>giver==0?"Whispering Hollow":"Shadowroot Depths";
    public static string Key(int giver,int stage)=>"hunt-v1-"+giver+"-"+stage;
    static readonly Contract[][] contracts={
        new[]{
            new Contract("A Safer Hollow","Travelers cannot reach the hollow while its passages are overrun. Help me clear a path.","Defeat 3 different cave encounters","Explore the cave beneath Whispering Hollow. Skeletons, goblins, mushrooms and flying eyes count. Training enemies do not count.",new[]{"Cave Goblin 02","Cave Mushroom 03","Cave Flying Eye 04","Cave Skeleton 05","Cave Goblin 06","Cave Mushroom 07","Cave Flying Eye 08","Cave Skeleton 09","Cave Goblin 10","Cave Mushroom 11","Cave Flying Eye 12"},3,100,100,2),
            new Contract("The Executioner's Watch","The lesser creatures are only the beginning. An undead executioner still holds the deeper passage.","Defeat the Undead Executioner Puppet","Search the deeper cave in Whispering Hollow. Read the enemy's attack countdown, keep enough health, and review difficult topics before trying again.",new[]{"Boss - Undead Executioner Puppet"},1,180,175,3),
            new Contract("Banish the Bringer","One final threat remains at the far end of the hollow. Defeat the Bringer of Death so travelers can return.","Defeat the Bringer of Death","Search the far-right depths of Whispering Hollow. Spend gold at the merchant or refine a suitable card with the blacksmith if your deck needs help.",new[]{"boss"},1,250,250,0,1)
        },
        new[]{
            new Contract("Shadows on the Trail","Demons have spread through the next hollow. Clear the patrols before we confront their leader.","Defeat 4 different demon or blood-monster encounters","Explore Shadowroot Depths. Any four different Demon or Blood Monster encounters count; bats and the Flying Demon are separate targets.",Enumerable.Range(1,16).Select(i=>"Scene3 "+(i%2==1?"Demon":"Blood Monster")+" "+i.ToString("00")).ToArray(),4,150,150,3),
            new Contract("Quiet the Wings","The patrols have weakened, but bats still watch the lower passages. Make those routes safer.","Defeat 3 different bat encounters","Look through the lower passages of Shadowroot Depths. Defeat three of its five bat encounters. Repeating the same encounter does not add another target.",Enumerable.Range(1,5).Select(i=>"Scene3 Bat "+i.ToString("00")).ToArray(),3,200,200,4),
            new Contract("The Flying Tyrant","The Flying Demon still commands the skies above this hollow. End its watch and complete our pact.","Defeat the Flying Demon","Search the upper-right route of Shadowroot Depths. Review conditions and loops with Rowan, then prepare your deck for the boss.",new[]{"Scene3 Flying Demon 01"},1,300,300,0,2)
        }
    };
    public static Contract Get(int giver,int stage)=>contracts[giver][stage];
    public static int Stage(SaveSlots.Record save,int giver)
    {int stage=0;while(stage<Stages&&save?.huntQuestsClaimed?.Contains(Key(giver,stage))==true)stage++;return stage;}
    public static bool Accepted(SaveSlots.Record save,int giver)=>Stage(save,giver)<Stages&&save?.huntQuestsAccepted?.Contains(Key(giver,Stage(save,giver)))==true;
    public static int Count(SaveSlots.Record save,int giver,int stage)
    {
        var c=Get(giver,stage);var victories=new HashSet<string>(save?.defeatedEncounters??new List<string>(),StringComparer.Ordinal);
        // Whispering Hollow's authored Bringer was renamed to "boss". Keep old saves
        // eligible without requiring a rematch against an encounter already removed.
        if(victories.Contains("Scene 2 - Bringer Of Death 01"))victories.Add("boss");
        return Math.Min(c.required,c.encounterIds.Distinct().Count(victories.Contains));
    }
    public static bool Ready(SaveSlots.Record save,int giver)=>Accepted(save,giver)&&Count(save,giver,Stage(save,giver))>=Get(giver,Stage(save,giver)).required;
    public static bool ApplyAccept(SaveSlots.Record save,int giver,int expected)
    {
        if(save==null||expected>=Stages||Stage(save,giver)!=expected||Accepted(save,giver))return false;
        if(save.huntQuestsAccepted==null)save.huntQuestsAccepted=new List<string>();
        save.huntQuestsAccepted.Add(Key(giver,expected));return true;
    }
    public static void Accept(int giver,int expected)
    {var save=SaveSlots.Read(SaveSlots.Active);if(ApplyAccept(save,giver,expected))SaveSlots.Write(save);}
    public static bool ApplyReward(SaveSlots.Record save,int giver,int expected)
    {
        if(expected>=Stages||Stage(save,giver)!=expected||!Ready(save,giver))return false;
        if(save.huntQuestsClaimed==null)save.huntQuestsClaimed=new List<string>();
        save.huntQuestsClaimed.Add(Key(giver,expected));PlayerProgression.ApplyQuestExperience(save,Get(giver,expected).xp);return true;
    }
    public static bool Claim(int giver,int expected)
    {
        var save=SaveSlots.Read(SaveSlots.Active);
        if(expected>=Stages||Stage(save,giver)!=expected||!Ready(save,giver))return false;
        var inventory=UnityEngine.Object.FindFirstObjectByType<CharacterInventory>();if(inventory==null||!inventory.IsCurrentSlot)return false;
        var c=Get(giver,expected);
        // Inventory has a separate durable receipt; a retry after an interrupted save cannot pay twice.
        inventory.GrantHuntReward(Key(giver,expected),c.gold,c.scraps,c.tokens);
        if(!ApplyReward(save,giver,expected))return false;SaveSlots.Write(save);return true;
    }
    public static string Reward(Contract c)=>c.xp+" XP, "+c.gold+" gold"+(c.scraps>0?", "+c.scraps+" low-logic scraps":"")+(c.tokens>0?", "+c.tokens+" library token"+(c.tokens==1?"":"s"):"");
    public static string Journal(SaveSlots.Record save,int giver)
    {
        int stage=Stage(save,giver);if(stage>=Stages)return "<b>"+Name(giver).ToUpperInvariant()+"'S CONTRACTS</b>\nAll three contracts completed.";
        var c=Get(giver,stage);
        return "<b>"+Name(giver).ToUpperInvariant()+"'S CONTRACT  •  "+(stage+1)+" / 3</b>\n"+c.title+"\n\n"+(Ready(save,giver)?"<color=#24552A><b>REWARD READY</b></color>\nReturn to "+Name(giver)+".":c.target+" ("+Count(save,giver,stage)+" / "+c.required+")\n"+c.advice)+"\nLocation: "+Scene(giver)+"\n\n<b>Reward</b>\n"+Reward(c);
    }
}
