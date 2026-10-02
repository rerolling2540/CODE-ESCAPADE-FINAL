using UnityEngine;
using UnityEngine.InputSystem;

public class Npc_Talk : MonoBehaviour
{
    private Animator anim;
    public Animator interactAnim;
    public DialogueSO dialogueSO;


    private void Awake()    
    {
    anim = GetComponentInChildren<Animator>();      
    }

    private void OnEnable()
    {
        anim.Play("Idle");
       
        
        interactAnim.Play("Open");
    }

    private void OnDisable()
    {
        interactAnim.Play("Close");
       
    }

    private void Update()
    {
        bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        
        interactPressed |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        
        interactPressed |= Gamepad.current != null &&
                           (Gamepad.current.buttonNorth.wasPressedThisFrame ||
                            Gamepad.current.buttonEast.wasPressedThisFrame);

        if (interactPressed)
        {
            if(DialogueManager.Instance.isDialogueActive)
               DialogueManager.Instance.AdvanceDialogue();
            else             
               DialogueManager.Instance.StartDialogue(dialogueSO);
        }
    }
}
