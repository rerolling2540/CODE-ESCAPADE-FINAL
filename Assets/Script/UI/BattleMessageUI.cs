using System.Collections;
using TMPro;
using UnityEngine;

public class BattleMessageUI : MonoBehaviour
{
    public static BattleMessageUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Hide panel on start
        panel.SetActive(false);
    }

    public void Show(string title, string description)
    {
        // Stop previous message if one is already showing
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(title, description));
    }

    private IEnumerator ShowRoutine(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;

        panel.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        panel.SetActive(false);

        currentRoutine = null;
    }
    private void Update()
{
    // Temporary test
    if (Input.GetKeyDown(KeyCode.T))
    {
        Show(
            "✔ Correct!",
            "Fireball activated!"
        );
    }
}
}