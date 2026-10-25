using System;
using System.Collections;
using System.Collections.Generic;
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
        public string question_type;
        public string prompt;
        public string code_snippet;
        public string[] choices;
        public int correct_index;
        public int min_level;
        public int max_level;
    }

    private readonly List<QuestionRow> runtimeQuestionRows = new List<QuestionRow>();
    private readonly List<MultipleChoiceQuestionData> runtimeQuestions = new List<MultipleChoiceQuestionData>();
    private bool remoteLoadFinished;

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
        ActionSystem.DetachPerformer<QuestionGA>();
    }

    private IEnumerator PerformQuestion(QuestionGA action)
    {
        yield return BattleTutorialGuide.ExplainQuestion();
        bool isDone = false;
        bool isCorrect = false;

        questionUI.SetupAndShow(
            action.Question,
            callback: (selectedIndex) =>
            {
                isCorrect = action.Question.CheckAnswer(selectedIndex);
                isDone = true;
            },
            onTimeOut: () =>
            {
                Debug.Log("Time ran out on Pop Quiz!");
                isCorrect = false;
                isDone = true;
            },
            playedCard: action.PlayedCard
        );

        // Pause the ActionSystem until answered or timed out
        while (!isDone)
        {
            yield return null;
        }

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

        int level = GetPlayerLevel();
        string language = GetPlayerLanguage();
        MultipleChoiceQuestionData[] eligible = GetRemoteQuestions(language, level);
        QuestionData selected = eligible.Length > 0
            ? eligible[UnityEngine.Random.Range(0, eligible.Length)]
            : database != null ? database.GetRandomQuestion() : null;

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
        return GetRemoteQuestions(GetPlayerLanguage(), GetPlayerLevel());
    }

    private MultipleChoiceQuestionData[] GetRemoteQuestions(string language, int level)
    {
        var matches = new List<MultipleChoiceQuestionData>();
        for (int i = 0; i < runtimeQuestions.Count; i++)
        {
            var question = runtimeQuestions[i];
            if (question == null) continue;
            QuestionRow row = runtimeQuestionRows[i];
            if (row.language == language && row.min_level <= level && (row.max_level == 0 || row.max_level >= level))
                matches.Add(question);
        }
        return matches.ToArray();
    }

    private IEnumerator LoadRemoteQuestions()
    {
        string root = string.IsNullOrWhiteSpace(supabaseProjectUrl) ? "" : supabaseProjectUrl.Trim().TrimEnd('/');
        string key = string.IsNullOrWhiteSpace(supabasePublishableKey) ? "" : supabasePublishableKey.Trim();
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Supabase question bank is not configured. Card questions will use the local QuestionDatabase.", this);
            remoteLoadFinished = true;
            yield break;
        }

        string url = root + "/rest/v1/question_bank?select=id,language,question_type,prompt,code_snippet,choices,correct_index,min_level,max_level&published=eq.true&order=id.asc";
        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", key);
            request.SetRequestHeader("Accept", "application/json");
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Could not load Supabase questions: " + request.error + " " + request.downloadHandler.text, this);
                remoteLoadFinished = true;
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;
                QuestionRow[] rows = JsonUtility.FromJson<QuestionRows>("{\"items\":" + json + "}").items;
                if (rows != null)
                {
                    foreach (QuestionRow row in rows)
                    {
                        if (row == null || row.choices == null || row.choices.Length < 2 ||
                            row.correct_index < 0 || row.correct_index >= row.choices.Length) continue;
                        var asset = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
                        asset.name = row.id;
                        asset.questionText = row.prompt;
                        asset.codeSnippet = row.code_snippet ?? "";
                        asset.options = row.choices;
                        asset.correctAnswerIndex = row.correct_index;
                        asset.timeLimitInSeconds = 25f;
                        runtimeQuestions.Add(asset);
                        runtimeQuestionRows.Add(row);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not parse Supabase question data: " + exception.Message, this);
            }
        }
        remoteLoadFinished = true;
        Debug.Log("Loaded " + runtimeQuestions.Count + " Supabase question(s).", this);
    }

    [Serializable]
    private sealed class QuestionRows { public QuestionRow[] items; }

    private string GetPlayerLanguage()
    {
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null || player.HeroClass == null) return "java";
        switch (player.HeroClass.LanguageClass)
        {
            case HeroLanguageClass.CSharp: return "csharp";
            case HeroLanguageClass.Python: return "python";
            default: return "java";
        }
    }

    private int GetPlayerLevel()
    {
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        return player == null ? 1 : player.CurrentLevel;
    }

    private void OnDestroy()
    {
        foreach (var question in runtimeQuestions)
            if (question != null) Destroy(question);
    }
}
