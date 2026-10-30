using UnityEngine;

/// <summary>Shared safe-ground validation for saves and checkpoint recovery.</summary>
public static class SafeCheckpoint
{
    public static bool IsSafe(playerMovement player,Vector3 position)
    {
        var body=player.GetComponent<Collider2D>();if(body==null)return false;
        Vector2 offset=(Vector2)(body.bounds.center-player.transform.position);
        Vector2 size=body.bounds.size;
        foreach(var hit in Physics2D.OverlapBoxAll((Vector2)position+offset,size+new Vector2(2f,.6f),0))
        {
            if(hit==null||hit.transform.IsChildOf(player.transform))continue;
            if(hit.GetComponentInParent<Map4TrapDamageTrigger>()!=null||hit.GetComponentInParent<TrapContact2D>()!=null||hit.GetComponentInParent<CaveEnemyEncounter>()!=null)return false;
            string name=hit.name.ToLowerInvariant();if(hit.isTrigger&&(name.Contains("trap")||name.Contains("spike")||name.Contains("poison")))return false;
        }
        float foot=body.bounds.min.y-player.transform.position.y;
        foreach(var hit in Physics2D.RaycastAll((Vector2)position+new Vector2(0,foot+.15f),Vector2.down,.45f))
            if(hit.collider!=null&&!hit.collider.isTrigger&&!hit.transform.IsChildOf(player.transform)&&hit.normal.y>.5f)return true;
        return false;
    }
    public static bool TryFind(playerMovement player,Vector3 desired,out Vector3 safe)
    {
        safe=desired;if(IsSafe(player,desired))return true;
        var body=player.GetComponent<Collider2D>();if(body==null)return false;
        float foot=body.bounds.min.y-player.transform.position.y;
        for(int step=0;step<=40;step++)foreach(int direction in new[]{-1,1})
        {
            float x=desired.x+direction*step;
            foreach(var hit in Physics2D.RaycastAll(new Vector2(x,desired.y+4),Vector2.down,24))
            {
                if(hit.collider==null||hit.collider.isTrigger||hit.transform.IsChildOf(player.transform)||hit.normal.y<.5f)continue;
                var candidate=new Vector3(x,hit.point.y-foot+.06f,desired.z);
                if(!IsSafe(player,candidate))continue;
                // Reject walls and low ceilings around the revived body.
                bool blocked=false;
                foreach(var overlap in Physics2D.OverlapBoxAll((Vector2)candidate+(Vector2)(body.bounds.center-player.transform.position),body.bounds.size*.9f,0))
                    if(overlap!=null&&!overlap.isTrigger&&!overlap.transform.IsChildOf(player.transform)){blocked=true;break;}
                if(!blocked){safe=candidate;return true;}
            }
        }
        return false;
    }
}
