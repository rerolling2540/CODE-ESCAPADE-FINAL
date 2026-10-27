using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Place the forge beside the opening area's elf without replacing authored scene edits.</summary>
public static class NPCWorldServices
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Scene 2") return;
        EnsureBlacksmith(scene);
    }

    public static BlacksmithNPC EnsureBlacksmith(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var existing = root.GetComponentInChildren<BlacksmithNPC>(true);
            if (existing != null) return existing;
        }
        var prefab = Resources.Load<GameObject>("NPC/Blacksmith");
        if (prefab == null) return null;
        Transform marker = null, elf = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Blacksmith Spawn") marker = child;
                var talk = child.GetComponent<Npc_Talk>();
                if (talk != null && talk.offersElfQuest) elf = child;
            }
        if (marker == null && elf == null) return null;
        Physics2D.SyncTransforms();
        Vector3 position = marker != null ? marker.position : elf.position;
        bool found = marker != null;
        if (marker == null)
            foreach (float offset in new[] { 7f, -7f, 9f, -9f })
            {
                var origin = elf.position + new Vector3(offset, 3f, 0);
                foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 10f))
                {
                    if (hit.collider == null || hit.collider.isTrigger || hit.collider.gameObject.scene != scene || hit.normal.y < .8f || !hit.collider.CompareTag("Ground")) continue;
                    position = new Vector3(origin.x, hit.point.y + .025f, elf.position.z);
                    found = true; break;
                }
                if (found) break;
            }
        if (!found) { Debug.LogWarning("No walkable floor beside the elf for the blacksmith. Place a Blacksmith Spawn marker."); return null; }
        var npc = Object.Instantiate(prefab, position, Quaternion.identity);
        npc.name = "Blacksmith";
        SceneManager.MoveGameObjectToScene(npc, scene);
        return npc.GetComponent<BlacksmithNPC>();
    }
}
