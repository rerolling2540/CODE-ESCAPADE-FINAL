using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class QuestionSystem : Singleton<QuestionSystem>
{
    [SerializeField] private QuestionDatabase database;
    [SerializeField] private QuestionUI questionUI;

    [Header("Supabase question bank (use the project URL and publishable key)")]
    [SerializeField] private string supabaseProjectUrl;
    [SerializeField] private string supabasePublishableKey;

    [Serializable]
    private sealed class QuestionRow
    {
        public string id;
        public string language;
        public string topic;
        public string difficulty;
        public string question_type;
        public string prompt;
        public string code_snippet;
        public string[] choices;
        public int correct_index;
        public int min_level;
        public int max_level;
    }

    private readonly List<MultipleChoiceQuestionData> runtimeQuestions = new List<MultipleChoiceQuestionData>();
    private static List<MultipleChoiceQuestionData> bundledQuestions;
    private bool remoteLoadFinished;
    private string activeAttemptId;

    private void Start()
    {
        StartCoroutine(LoadRemoteQuestions());
    }

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<QuestionGA>(PerformQuestion);
    }

    private void OnDisable()
    {
        EndAttempt();
        ActionSystem.DetachPerformer<QuestionGA>();
    }

    private void EndAttempt()
    {
        QuestionPerformanceTracker.Abandon(activeAttemptId);
        activeAttemptId = null;
    }

    private IEnumerator PerformQuestion(QuestionGA action)
    {
        yield return BattleTutorialGuide.ExplainQuestion();
        if (questionUI == null || action.Question == null)
        {
            Debug.LogError("QuestionSystem cannot show this question because its QuestionUI or question data is missing.", this);
            QuestionUI.UnlockGameplayInput();
            action.OnAnswered?.Invoke(false);
            yield break;
        }

        bool isDone = false;
        bool isCorrect = false;
        string attemptId = QuestionPerformanceTracker.Begin(action.Question, "battle");
        activeAttemptId = attemptId;

        questionUI.SetupAndShow(
            action.Question,
            callback: (selectedIndex) =>
            {
                isCorrect = action.Question.CheckAnswer(selectedIndex);
                QuestionPerformanceTracker.Complete(attemptId, isCorrect);
                activeAttemptId = null;
                isDone = true;
            },
            onTimeOut: () =>
            {
                Debug.Log("Time ran out on Pop Quiz!");
                isCorrect = false;
                QuestionPerformanceTracker.TimeOut(attemptId);
                activeAttemptId = null;
                isDone = true;
            },
            playedCard: action.PlayedCard
        );

        // Pause the ActionSystem until answered or timed out
        while (!isDone && activeAttemptId == attemptId)
        {
            yield return null;
        }
        if (!isDone) yield break;

        // Send answer result back to whoever queued the action
        BattleAudio.Play(isCorrect ? BattleSound.Correct : BattleSound.Wrong);
        BattleTutorialGuide.NotifyAnswer(isCorrect);
        action.OnAnswered?.Invoke(isCorrect);
    }
    public void AskRandomQuestion(Action<bool> onAnswered)
    {
        AskRandomQuestion(null, onAnswered);
    }

    public void AskRandomQuestion(Card playedCard, Action<bool> onAnswered)
    {
        Debug.Log("QuestionSystem: AskRandomQuestion");
        QuestionUI.LockGameplayInput();
        StartCoroutine(AskWhenReady(playedCard, onAnswered));
    }

    private IEnumerator AskWhenReady(Card playedCard, Action<bool> onAnswered)
    {
        if (BattleTutorialGuide.IsTraining)
        {
            ActionSystem.Instance.Perform(new QuestionGA(BattleTutorialGuide.NextPracticeQuestion(), onAnswered, playedCard));
            yield break;
        }
        // Wait for the bank request so card questions use the published database bank.
        while (!remoteLoadFinished) yield return null;

        QuestionData selected = SelectForCurrentPlayer(GetQuestionPool(), playedCard);

        if (selected == null)
        {
            Debug.LogError("No questions are available. Check the Supabase question bank setup and the local QuestionDatabase.");
            QuestionUI.UnlockGameplayInput();
            onAnswered?.Invoke(false);
            yield break;
        }

        if (questionUI == null)
        {
            Debug.LogError("QuestionSystem has no QuestionUI assigned.", this);
            QuestionUI.UnlockGameplayInput();
            onAnswered?.Invoke(false);
            yield break;
        }

        ActionSystem.Instance.Perform(new QuestionGA(selected, onAnswered, playedCard));
    }

    public MultipleChoiceQuestionData[] GetQuestionsForCurrentPlayer()
    {
        string language = GetPlayerLanguage();
        int level = GetPlayerLevel();
        return GetQuestionPool().OfType<MultipleChoiceQuestionData>()
            .Where(q => QuestionCurriculum.Language(q) == language && q.minLevel <= level).ToArray();
    }

    private IEnumerable<QuestionData> GetQuestionPool()
    {
        if (runtimeQuestions.Count > 0) return runtimeQuestions;
        return database != null && database.questions != null ? database.questions : Array.Empty<QuestionData>();
    }

    public static QuestionData SelectForCurrentPlayer(IEnumerable<QuestionData> pool, Card playedCard = null, string avoidId = null)
    {
        string language = CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter", 0));
        var player = FindFirstObjectByType<PlayerStats>();
        if (player != null && player.HeroClass != null)
            language = player.HeroClass.LanguageClass == HeroLanguageClass.CSharp ? "csharp" :
                player.HeroClass.LanguageClass == HeroLanguageClass.Python ? "python" : "java";
        var card = playedCard?.CardData;
        var available = (pool ?? Array.Empty<QuestionData>()).Where(q => q != null).ToList();
        // Scene-authored legacy doors may only contain Java questions. Use the matching bundled language instead.
        if (!available.Any(q => QuestionCurriculum.Language(q) == language)) available = GetBundledQuestions().Cast<QuestionData>().ToList();
        return AdaptiveQuestionSelector.Select(available, language, QuestionPerformanceTracker.GetCurrentPlayerLevel(),
            QuestionPerformanceTracker.GetLearningHistory(language, SaveSlots.Active), UnityEngine.Random.value, avoidId,
            card != null ? card.minimumQuestionTier : 1, card != null ? card.maximumQuestionTier : 3);
    }

    private static List<MultipleChoiceQuestionData> GetBundledQuestions()
    {
        if (bundledQuestions != null && bundledQuestions.All(q => q != null)) return bundledQuestions;
        bundledQuestions = new List<MultipleChoiceQuestionData>();
        var asset = Resources.Load<TextAsset>("QuestionBanks/StarterQuestions");
        if (asset == null) return bundledQuestions;
        var rows = JsonUtility.FromJson<QuestionRows>("{\"items\":" + asset.text + "}").items;
        foreach (var row in rows ?? Array.Empty<QuestionRow>())
        {
            if (row.choices == null || row.choices.Length < 2 || row.correct_index < 0 || row.correct_index >= row.choices.Length) continue;
            var question = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
            question.name = question.questionId = row.id;
            question.language = row.language; question.topic = QuestionCurriculum.Topic(row.topic, row.id);
            question.difficulty = row.difficulty; question.minLevel = row.min_level; question.maxLevel = row.max_level;
            question.questionText = row.prompt; question.codeSnippet = row.code_snippet;
            question.options = row.choices; question.correctAnswerIndex = row.correct_index; question.timeLimitInSeconds = 25f;
            bundledQuestions.Add(question);
        }
        return bundledQuestions;
    }

    private IEnumerator LoadRemoteQuestions()
    {
        // Same authored seed questions as the SQL bank; supports a first offline launch in every language.
        var starterBank = Resources.Load<TextAsset>("QuestionBanks/StarterQuestions");
        if (starterBank != null) ApplyQuestionRows(starterBank.text);
        string root = string.IsNullOrWhiteSpace(supabaseProjectUrl) ? "" : supabaseProjectUrl.Trim().TrimEnd('/');
        string key = string.IsNullOrWhiteSpace(supabasePublishableKey) ? "" : supabasePublishableKey.Trim();
        string cachePath = GetQuestionCachePath(root);
        if (!string.IsNullOrEmpty(cachePath) && File.Exists(cachePath))
        {
            try
            {
                string cachedJson = File.ReadAllText(cachePath);
                int cachedCount = ApplyQuestionRows(cachedJson);
                if (cachedCount > 0)
                    Debug.Log("Loaded " + cachedCount + " cached Supabase question(s) for offline play.", this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not read the local question cache: " + exception.Message, this);
            }
        }
        // Cached questions (or the local QuestionDatabase fallback) can be used immediately.
        remoteLoadFinished = true;

        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(key))
        {
            if (runtimeQuestions.Count == 0)
                Debug.LogWarning("Supabase question bank is not configured. Card questions will use the local QuestionDatabase.", this);
            yield break;
        }

        string url = root + "/rest/v1/question_bank?select=id,language,topic,difficulty,question_type,prompt,code_snippet,choices,correct_index,min_level,max_level&published=eq.true&order=id.asc";
        while (true)
        {
            bool refreshed = false;
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 12;
                request.SetRequestHeader("apikey", key);
                request.SetRequestHeader("Accept", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = request.downloadHandler.text;
                    // An active popup must retain its ScriptableObject until its answer is resolved.
                    while (!string.IsNullOrEmpty(activeAttemptId)) yield return null;
                    int loadedCount = ApplyQuestionRows(responseJson);
                    if (loadedCount > 0)
                    {
                        try
                        {
                            string directory = Path.GetDirectoryName(cachePath);
                            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                            File.WriteAllText(cachePath, responseJson);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("Questions loaded, but the offline cache could not be saved: " + exception.Message, this);
                        }
                        Debug.Log("Loaded " + loadedCount + " Supabase question(s) and refreshed the offline cache.", this);
                        refreshed = true;
                    }
                    else if (string.Equals(responseJson.Trim(), "[]", StringComparison.Ordinal))
                    {
                        refreshed = true;
                    }
                    else
                    {
                        Debug.LogWarning("Supabase returned no usable question rows. Keeping the current cache and retrying.", this);
                    }
                }
                else
                {
                    Debug.LogWarning("Could not refresh Supabase questions. Cached/local questions remain available. " +
                        request.error + " " + request.downloadHandler.text, this);
                }
            }

            if (refreshed) break;
            // Keep trying in the background so the cache refreshes after connectivity returns.
            yield return new WaitForSecondsRealtime(60f);
        }
    }

    private int ApplyQuestionRows(string json)
    {
        try
        {
            QuestionRow[] rows = JsonUtility.FromJson<QuestionRows>("{\"items\":" + json + "}").items;
            if (rows == null || rows.Length == 0) return 0;

            var parsedQuestions = new List<MultipleChoiceQuestionData>();
            foreach (QuestionRow row in rows)
            {
                if (row == null || row.choices == null || row.choices.Length < 2 ||
                    row.correct_index < 0 || row.correct_index >= row.choices.Length) continue;
                var asset = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
                asset.name = row.id;
                asset.questionId = row.id;
                asset.topic = row.topic;
                asset.language = row.language;
                asset.difficulty = row.difficulty;
                asset.minLevel = row.min_level;
                asset.maxLevel = row.max_level;
                asset.questionText = row.prompt;
                asset.codeSnippet = row.code_snippet ?? "";
                asset.options = row.choices;
                asset.correctAnswerIndex = row.correct_index;
                asset.timeLimitInSeconds = 25f;
                parsedQuestions.Add(asset);
            }

            if (parsedQuestions.Count == 0) return 0;
            foreach (var oldQuestion in runtimeQuestions)
                if (oldQuestion != null) Destroy(oldQuestion);
            runtimeQuestions.Clear();
            runtimeQuestions.AddRange(parsedQuestions);
            return runtimeQuestions.Count;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not parse question-bank data: " + exception.Message, this);
            return 0;
        }
    }

    private static string GetQuestionCachePath(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Uri.TryCreate(root, UriKind.Absolute, out Uri uri)) return "";
        string projectKey = System.Text.RegularExpressions.Regex.Replace(uri.Host.ToLowerInvariant(), "[^a-z0-9]", "_");
        if (string.IsNullOrEmpty(projectKey)) return "";
        return Path.Combine(Application.persistentDataPath, "QuestionBank_" + projectKey + ".json");
    }

    [Serializable]
    private sealed class QuestionRows { public QuestionRow[] items; }

    private string GetPlayerLanguage()
    {
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null || player.HeroClass == null) return CharacterAlmanac.LanguageForCharacter(PlayerPrefs.GetInt("SelectedCharacter", 0));
        switch (player.HeroClass.LanguageClass)
        {
            case HeroLanguageClass.CSharp: return "csharp";
            case HeroLanguageClass.Python: return "python";
            default: return "java";
        }
    }

    private int GetPlayerLevel()
    {
        return QuestionPerformanceTracker.GetCurrentPlayerLevel();
    }

    private void OnDestroy()
    {
        EndAttempt();
        foreach (var question in runtimeQuestions)
            if (question != null) Destroy(question);
    }
}
