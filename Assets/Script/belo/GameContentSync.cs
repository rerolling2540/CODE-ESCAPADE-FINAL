using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>Public game content, separate from private player ownership. Never loads remote code/YAML.</summary>
public sealed class GameContentSync : MonoBehaviour
{
    [Serializable] public class CardRow
    {
        public string id, game_card_key, name, description, image_url, character_class, template_key, icon_template_key;
        public int mana, damage_type, base_damage, minimum_question_tier = 1, maximum_question_tier = 3, shop_price = 100;
        public float percent_damage, damage_multiplier = 1;
        public bool is_active = true, shop_available;
    }
    [Serializable] private class Rows { public CardRow[] items; }
    [Serializable] private class QuestionIdentity { public string id; public string[] choices; public int correct_index; }
    [Serializable] private class QuestionRows { public QuestionIdentity[] items; }
    private static GameContentSync instance;
    private static readonly Dictionary<string, CardRow> rows = new Dictionary<string, CardRow>();
    private static readonly Dictionary<string, CardData> runtime = new Dictionary<string, CardData>();
    private static readonly Dictionary<CardData, string> runtimeKeys = new Dictionary<CardData, string>();
    private static readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();
    private static readonly HashSet<string> loadingImages = new HashSet<string>();
    private string url, key;
    private bool refreshing;
    private string pendingCards;
    public static string QuestionJson { get; private set; }
    public static bool HasQuestionSnapshot => QuestionJson != null;
    public static int Version { get; private set; }
    public static event Action Changed;

    public static GameContentSync Configure(string projectUrl, string publishableKey)
    {
        if (!Application.isPlaying || string.IsNullOrWhiteSpace(projectUrl) || string.IsNullOrWhiteSpace(publishableKey)) return instance;
        if (instance == null)
        {
            instance = new GameObject("Game Content Sync").AddComponent<GameContentSync>();
            DontDestroyOnLoad(instance.gameObject);
        }
        string root = projectUrl.Trim().TrimEnd('/');
        instance.key = publishableKey.Trim();
        if (instance.url != root)
        {
            instance.url = root; instance.key = publishableKey.Trim();
            rows.Clear(); runtime.Clear(); runtimeKeys.Clear(); QuestionJson = null; instance.pendingCards = null;
            instance.ReadCache();
            instance.StartCoroutine(instance.RefreshNow());
        }
        return instance;
    }

    void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; StartCoroutine(RefreshPeriodically()); }
    void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; }
    void OnDestroy() { if (instance == this) instance = null; }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Battle Scene" && pendingCards != null) { ApplyCards(pendingCards); pendingCards = null; }
    }
    IEnumerator RefreshPeriodically()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(60);
            if (!string.IsNullOrEmpty(url)) yield return RefreshNow();
        }
    }
    public IEnumerator RefreshNow()
    {
        if (refreshing) { while (refreshing) yield return null; yield break; }
        if (string.IsNullOrWhiteSpace(url)) yield break;
        refreshing = true;
        try
        {
            string cards = null;
            yield return FetchPages("cards", "id,game_card_key,name,description,mana,image_url,damage_type,base_damage,percent_damage,damage_multiplier,character_class,template_key,icon_template_key,is_active,minimum_question_tier,maximum_question_tier,shop_available,shop_price", "game_card_key.asc", "", text => cards = text);
            if (cards != null && ValidateCards(cards))
            {
                WriteCache("cards", cards);
                if (SceneManager.GetActiveScene().name == "Battle Scene") pendingCards = cards;
                else ApplyCards(cards);
            }
            string questions = null;
            yield return FetchPages("question_bank", "id,language,topic,difficulty,question_type,prompt,code_snippet,choices,correct_index,min_level,max_level,explanation", "id.asc", "&published=eq.true", text => questions = text);
            if (questions != null && ValidateQuestions(questions))
            {
                WriteCache("questions", questions);
                if (QuestionJson != questions) { QuestionJson = questions; Changed?.Invoke(); }
            }
        }
        finally { refreshing = false; }
    }

    IEnumerator FetchPages(string table, string columns, string order, string filter, Action<string> completed)
    {
        var pages = new List<string>();
        const int size = 200;
        for (int offset = 0; ; offset += size)
        {
            using (var request = UnityWebRequest.Get(url + "/rest/v1/" + table + "?select=" + columns + "&order=" + order + filter + "&limit=" + size + "&offset=" + offset))
            {
                request.timeout = 12;
                request.SetRequestHeader("apikey", key);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { completed(null); yield break; }
                string json = request.downloadHandler.text.Trim();
                int count;
                try { count = JsonUtility.FromJson<QuestionRows>("{\"items\":" + json + "}")?.items?.Length ?? -1; }
                catch (Exception) { count = -1; }
                if (count < 0 || !json.StartsWith("[") || !json.EndsWith("]")) { completed(null); yield break; }
                if (count > 0) pages.Add(json.Substring(1, json.Length - 2));
                if (count < size) break;
            }
        }
        completed("[" + string.Join(",", pages) + "]");
    }
    public static IEnumerator ReadEditorPages(string query, Action<UnityWebRequest> headers, Action<string, string> completed)
    {
        var pages = new List<string>();
        for (int offset = 0; ; offset += 200)
        {
            using (var request = UnityWebRequest.Get(query + "&limit=200&offset=" + offset))
            {
                request.timeout = 12; headers(request);
                yield return SupabaseSession.Send(request);
                if (request.result != UnityWebRequest.Result.Success)
                { completed(null, "Could not load content: HTTP " + request.responseCode + ". " + (request.downloadHandler?.text ?? request.error)); yield break; }
                string json = request.downloadHandler.text.Trim();
                int count = -1;
                try { count = JsonUtility.FromJson<QuestionRows>("{\"items\":" + json + "}")?.items?.Length ?? -1; } catch (Exception) { }
                if (count < 0 || !json.StartsWith("[") || !json.EndsWith("]"))
                { completed(null, "The server returned an invalid content list."); yield break; }
                if (count > 0) pages.Add(json.Substring(1, json.Length - 2));
                if (count < 200) break;
            }
        }
        completed("[" + string.Join(",", pages) + "]", null);
    }
    public static bool ValidateCards(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<Rows>("{\"items\":" + json + "}")?.items;
            return data != null && data.All(r => r != null && !string.IsNullOrWhiteSpace(r.game_card_key) &&
                !string.IsNullOrWhiteSpace(r.name) && r.mana >= 0 && r.base_damage >= 0 && r.damage_type >= 0 && r.damage_type <= 3 &&
                r.percent_damage >= 0 && !float.IsInfinity(r.percent_damage) && r.damage_multiplier >= 0 && !float.IsInfinity(r.damage_multiplier) && r.minimum_question_tier >= 1 && r.maximum_question_tier <= 3 &&
                r.maximum_question_tier >= r.minimum_question_tier) && data.Select(r => r.game_card_key).Distinct().Count() == data.Length;
        }
        catch (Exception) { return false; }
    }
    public static bool ValidateQuestions(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<QuestionRows>("{\"items\":" + json + "}")?.items;
            return data != null && data.All(r => r != null && !string.IsNullOrEmpty(r.id) && r.choices != null &&
                r.choices.Length >= 2 && r.correct_index >= 0 && r.correct_index < r.choices.Length);
        }
        catch (Exception) { return false; }
    }
    void ApplyCards(string json)
    {
        if (JsonUtility.ToJson(new Rows { items = rows.Values.OrderBy(r => r.game_card_key).ToArray() }) ==
            JsonUtility.ToJson(new Rows { items = JsonUtility.FromJson<Rows>("{\"items\":" + json + "}").items.OrderBy(r => r.game_card_key).ToArray() })) return;
        rows.Clear();
        foreach (var row in JsonUtility.FromJson<Rows>("{\"items\":" + json + "}").items) rows[row.game_card_key] = row;
        // Keep old clones alive for existing hands/popups; newly resolved cards use the new version.
        runtime.Clear(); Version++; Changed?.Invoke();
    }
    string CachePath(string kind) => Path.Combine(Application.persistentDataPath, "Content_" + new Uri(url).Host + "_" + kind + ".json");
    void ReadCache()
    {
        try
        {
            if (File.Exists(CachePath("cards"))) { string json = File.ReadAllText(CachePath("cards")); if (ValidateCards(json)) ApplyCards(json); }
            string questionFile = CachePath("questions");
            if (!File.Exists(questionFile)) questionFile = Path.Combine(Application.persistentDataPath,
                "QuestionBank_" + System.Text.RegularExpressions.Regex.Replace(new Uri(url).Host.ToLowerInvariant(), "[^a-z0-9]", "_") + ".json");
            if (File.Exists(questionFile)) { string json = File.ReadAllText(questionFile); if (ValidateQuestions(json)) QuestionJson = json; }
        }
        catch (Exception e) { Debug.LogWarning("Content cache could not be read: " + e.Message); }
    }
    void WriteCache(string kind, string json)
    {
        try
        {
            string file = CachePath(kind), temporary = file + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(file)) File.Replace(temporary, file, null);
            else File.Move(temporary, file);
        }
        catch (Exception e) { Debug.LogWarning("Content works online but its offline copy could not be saved: " + e.Message); }
    }
    public static string RuntimeKey(CardData card) => card != null && runtimeKeys.TryGetValue(card, out string id) ? id : null;
    public static CardData Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (runtime.TryGetValue(id, out var card) && card != null) return card;
        var catalog = GameplayCardCatalog.Load();
        var bundled = catalog?.cards.Find(e => e != null && e.id == id)?.card;
        if (!Application.isPlaying || !rows.TryGetValue(id, out var row)) return bundled;
        string templateKey = string.IsNullOrEmpty(row.template_key) ? id : row.template_key;
        var template = catalog?.cards.Find(e => e != null && e.id == templateKey)?.card;
        if (template == null) return bundled;
        card = Instantiate(template);
        var icon = catalog.cards.Find(e => e != null && e.id == row.icon_template_key)?.card?.Image ?? template.Image;
        card.ApplyCatalogValues(row, icon);
        runtime[id] = card; runtimeKeys[card] = id;
        if (!string.IsNullOrEmpty(row.image_url) && instance != null)
            instance.StartCoroutine(instance.LoadImage(row.image_url, card));
        return card;
    }
    public static int HeroFor(string id)
    {
        if (rows.TryGetValue(id ?? "", out var row))
            return row.character_class == "warrior" ? 0 : row.character_class == "archer" ? 1 : row.character_class == "mage" ? 2 : -1;
        return GameplayCardCatalog.Load()?.cards.Find(e => e != null && e.id == id)?.hero ?? -1;
    }
    public static bool CanObtain(string id) => !rows.TryGetValue(id ?? "", out var row) || row.is_active;
    public static CardRow Metadata(string id) => rows.TryGetValue(id ?? "", out var row) ? row : null;
    public static IEnumerable<CardRow> ShopCards => rows.Values.Where(r => r.is_active && r.shop_available && r.shop_price > 0 && Find(r.game_card_key) != null);

    IEnumerator LoadImage(string address, CardData card)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https") yield break;
        while (loadingImages.Contains(address)) yield return null;
        if (card == null) yield break;
        if (images.TryGetValue(address, out var sprite)) { card.SetCatalogIcon(sprite); yield break; }
        loadingImages.Add(address);
        try
        {
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(address))).Replace("-", "");
            string file = Path.Combine(Application.persistentDataPath, "CardIcon_" + hash + ".png");
            byte[] bytes = null;
            try { if (File.Exists(file)) bytes = File.ReadAllBytes(file); } catch (IOException) { }
            if (bytes == null)
            {
                using (var request = UnityWebRequest.Get(address))
                {
                    request.timeout = 12;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        if (request.downloadedBytes > 4 * 1024 * 1024) { request.Abort(); yield break; }
                        yield return null;
                    }
                    if (request.result != UnityWebRequest.Result.Success) yield break;
                    bytes = request.downloadHandler.data;
                }
            }
            if (bytes.Length > 4 * 1024 * 1024) yield break;
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(bytes) || texture.width > 2048 || texture.height > 2048) { Destroy(texture); yield break; }
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            images[address] = sprite;
            try { File.WriteAllBytes(file, bytes); } catch (IOException) { }
            string id = RuntimeKey(card);
            if (card != null && id != null && rows.TryGetValue(id, out var row) && row.image_url == address) card.SetCatalogIcon(sprite);
        }
        finally { loadingImages.Remove(address); }
    }
}
