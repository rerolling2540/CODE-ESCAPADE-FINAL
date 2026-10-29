using UnityEngine;
public sealed class BattleRetreatButton : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    void Start(){button=GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(()=>{var flow=FindFirstObjectByType<EncounterBattleReturn>();if(flow!=null)flow.ReturnToLobby();});}
    void Update(){if(button!=null)button.interactable=!GameControlsGuide.GameplayOverlayHidden&&!QuestionUI.IsQuestionActive&&!EncounterBattleReturn.BattleEnded&&ActionSystem.Instance!=null&&!ActionSystem.Instance.IsPerforming;}
}
