using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class QuestionUI : MonoBehaviour
{
    [Header("General Question UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text codeSnippetText; // For code blocks / Fill-in-the-blank
    [SerializeField] private Slider timerSlider;

    [Header("4 Choice Buttons (A-D)")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TMP_Text[] optionTexts = new TMP_Text[4];


    private Action<int> onOptionSelected;
    private Coroutine timerCoroutine;
    private bool isAnswered;

    public void SetupAndShow(QuestionData data, Action<int> callback, Action onTimeOut)
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        isAnswered = false;
        onOptionSelected = callback;

        // Set Main Question Text
        questionText.text = data.questionText;


        // Populate options & snippet based on type
        if (data is MultipleChoiceQuestionData mcData)
        {
            codeSnippetText.gameObject.SetActive(!string.IsNullOrEmpty(mcData.codeSnippet));
            codeSnippetText.text = mcData.codeSnippet;

            Debug.Log("OptionTexts Length: " + optionTexts.Length);
            Debug.Log("Options Length: " + mcData.options.Length);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                Debug.Log($"i = {i}");

                Debug.Log("optionTexts[i] = " + optionTexts[i]);
                Debug.Log("mcData.options[i] = " + mcData.options[i]);

                optionTexts[i].text = mcData.options[i];
            }
        }
        else if (data is FillInBlankQuestionData fibData)
        {
            codeSnippetText.gameObject.SetActive(true);
            codeSnippetText.text = fibData.codeWithBlank;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionTexts[i].text = fibData.optionBlanks[i];
            }
        }

        // Setup Button Listeners
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Closure copy
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

        // Start Timer
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine(data.timeLimitInSeconds, onTimeOut));
    }

    private void OnButtonClicked(int index)
    {
        if (isAnswered) return;
        isAnswered = true;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        gameObject.SetActive(false);
        onOptionSelected?.Invoke(index);
    }

    private IEnumerator TimerRoutine(float duration, Action onTimeOut)
    {
        float remaining = duration;
        timerSlider.maxValue = duration;
        timerSlider.value = duration;

        while (remaining > 0)
        {
            remaining -= Time.deltaTime;
            timerSlider.value = remaining;
            yield return null;
        }

        if (!isAnswered)
        {
            isAnswered = true;
            gameObject.SetActive(false);
            onTimeOut?.Invoke();
        }
    }
}