using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;


    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    
    public Button[] choiceButtons;

    public bool isDialogueActive;

    private DialogueSO currentDialogue;
    private int dialogueIndex;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else 
            Destroy(gameObject);

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        foreach (var button in choiceButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    public void StartDialogue(DialogueSO dialogueSO)
    {
        currentDialogue = dialogueSO;
        dialogueIndex = 0;
        isDialogueActive = true;
        ShowDialogue();
    }

    public void AdvanceDialogue()
    {
        if (dialogueIndex < currentDialogue.lines.Length)
            ShowDialogue();
            else 
            ShowChoices();

    }


    private void ShowDialogue()
    {
        DialogueLine line = currentDialogue.lines[dialogueIndex];

        portrait.sprite = line.speaker.portrait;
        actorName.text = line.speaker.actorName;

        dialogueText.text = line.text;
         
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        dialogueIndex++;
    }

    private void ShowChoices()
    {   
        ClearChoiceButtons();

        if (currentDialogue.options.Length > 0)
        {
           for (int i = 0; i < currentDialogue.options.Length; i++)
            {
               var option = currentDialogue.options[i];
               choiceButtons[i].GetComponentInChildren<TMP_Text>().text = option.optionText;
               choiceButtons[i].gameObject.SetActive(true);

               choiceButtons[i].onClick.AddListener(() => ChooseOption(option.nextDialogue));
            }          
        }
        else
        {
           choiceButtons[0].GetComponentInChildren<TMP_Text>().text = "End Dialogue";
           choiceButtons[0].onClick.AddListener (EndDialogue);
           choiceButtons[0].gameObject.SetActive(true); 
        }
    }


    private void ChooseOption(DialogueSO dialogueSO)
    {
       if (dialogueSO == null)
       {
           EndDialogue();
       }
       else
       {   ClearChoiceButtons();
           StartDialogue(dialogueSO);
       }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;   
        dialogueIndex = 0;
        ClearChoiceButtons();
        
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
       
    
    private void ClearChoiceButtons()
    {
        foreach (var button in choiceButtons)
        {
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }
    }

}
