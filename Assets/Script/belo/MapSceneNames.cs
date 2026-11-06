using System.Collections.Generic;

/// <summary>Compatibility for scene references stored before the exploration maps were renamed.</summary>
public static class MapSceneNames
{
    /// <summary>Display-only compatibility for dialogue assets written before map renaming.</summary>
    public static string DisplayText(string text)
    {
        if(string.IsNullOrEmpty(text))return text;
        return System.Text.RegularExpressions.Regex.Replace(text,@"\b(?:scene|map)\s*([2-5])\b",match=>
            new[]{"Whispering Hollow","Shadowroot Depths","Frostbound Passage","Forest of Dawn"}[int.Parse(match.Groups[1].Value)-2],
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    public static string Resolve(string scene)
    {
        switch (scene)
        {
            case "Scene 2": return "Whispering Hollow";
            case "Scene3": return "Shadowroot Depths";
            case "Scene4":
            case "map 4": return "Frostbound Passage";
            case "map 5": return "Forest of Dawn";
            default: return scene;
        }
    }

    public static string ResolveProgressKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        foreach (string old in new[] { "Scene 2", "Scene3", "Scene4", "map 4", "map 5" })
        {
            if (key.StartsWith(old + "/", System.StringComparison.Ordinal))
                return Resolve(old) + key.Substring(old.Length);
            string path = "Assets/Scenes/" + old + ".unity/";
            if (key.StartsWith(path, System.StringComparison.Ordinal))
                return "Assets/Scenes/" + Resolve(old) + ".unity/" + key.Substring(path.Length);
        }
        return key;
    }

    public static void NormalizeSave(SaveSlots.Record save)
    {
        if (save == null) return;
        save.scene = Resolve(save.scene);
        save.checkpointScene = Resolve(save.checkpointScene);
        if (save.visitedScenes != null)
        {
            var visited = new List<string>();
            foreach (string scene in save.visitedScenes)
            {
                string current = Resolve(scene);
                if (!visited.Contains(current)) visited.Add(current);
            }
            save.visitedScenes = visited;
        }
        NormalizeKeys(save.solvedPortalQuestions);
        NormalizeKeys(save.solvedTrapSwitches);
    }

    private static void NormalizeKeys(List<string> keys)
    {
        if (keys == null) return;
        for (int i = 0; i < keys.Count; i++) keys[i] = ResolveProgressKey(keys[i]);
    }
}
