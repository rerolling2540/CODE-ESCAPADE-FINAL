using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class ForestRuleTileLevelSetup
{
    private const string Root = "Assets/2D Pixel Art Platformer Biome - American Forest";
    private const string Output = Root + "/Generated Level";
    private const string AutoBuildKey = "ForestRuleTileLevelSetup.AutoBuildCompleted";

    [InitializeOnLoadMethod]
    private static void ScheduleFirstBuild()
    {
        if (EditorPrefs.GetBool(AutoBuildKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(AutoBuildKey, false)) return;
            EditorPrefs.SetBool(AutoBuildKey, true);
            Build();
        };
    }

    [MenuItem("Tools/Level Setup/Build American Forest Rule Tile Level")]
    public static void Build()
    {
        EnsureFolder(Output);
        var groundTiles = AssetDatabase.FindAssets("t:Tile", new[] { Root + "/Tilemap" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Tile>)
            .Where(t => t != null && t.name.StartsWith("TileGround"))
            .OrderBy(t => t.name, System.StringComparer.Ordinal)
            .ToList();
        if (groundTiles.Count == 0 || groundTiles[0].sprite == null)
            throw new System.Exception("No ground tiles with sprites were found in the American Forest Tilemap folder.");

        var sprites = groundTiles.Select(t => t.sprite).Distinct().ToList();
        var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        ruleTile.name = "Forest Ground Rule Tile";
        ruleTile.m_DefaultSprite = sprites[0];
        var directions = new[] {
            new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0),
            new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0)
        };
        // Create a rule for every cardinal connectivity mask. The RuleTile
        // chooses a matching mask first, then varies the matching terrain art.
        for (var mask = 15; mask >= 0; mask--)
        {
            var rule = new RuleTile.TilingRule {
                m_Sprites = sprites.ToArray(),
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed
            };
            for (var i = 0; i < directions.Length; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                rule.m_NeighborPositions.Add(directions[i]);
                rule.m_Neighbors.Add(RuleTile.TilingRule.Neighbor.This);
            }
            ruleTile.m_TilingRules.Add(rule);
        }
        AssetDatabase.CreateAsset(ruleTile, Output + "/Forest Ground Rule Tile.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var grid = new GameObject("Grid", typeof(Grid));
        var ground = new GameObject("Ground and Platforms", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        ground.transform.SetParent(grid.transform, false);
        ground.GetComponent<TilemapRenderer>().sortingOrder = 0;
        var map = ground.GetComponent<Tilemap>();

        // Main traversable floor with a stepped underside to make its collision volume solid.
        Fill(map, ruleTile, -20, 0, 20, 0);
        Fill(map, ruleTile, -20, -1, 20, -1);
        Fill(map, ruleTile, -20, -2, 20, -2);
        // Three raised platforms with intentional gaps for jumps.
        Fill(map, ruleTile, -12, 4, -7, 4);
        Fill(map, ruleTile, -3, 7, 3, 7);
        Fill(map, ruleTile, 9, 4, 14, 4);
        Fill(map, ruleTile, 18, 8, 23, 8);
        map.CompressBounds();

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 4, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8;
        camera.backgroundColor = new Color(0.12f, 0.18f, 0.22f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        var spawn = new GameObject("Player Spawn");
        spawn.transform.position = new Vector3(-17, 1, 0);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Output + "/Forest Rule Tile Level.unity");
        AssetDatabase.SaveAssets();
        Selection.activeObject = ruleTile;
        Debug.Log("Built Forest Rule Tile Level and Forest Ground Rule Tile.");
    }

    private static void Fill(Tilemap map, TileBase tile, int x0, int y, int x1, int y1)
    {
        for (var x = x0; x <= x1; x++)
            for (var row = y; row <= y1; row++) map.SetTile(new Vector3Int(x, row, 0), tile);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
