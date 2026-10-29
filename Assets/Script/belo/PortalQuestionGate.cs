using System;
using UnityEngine;

/// <summary>Uses the shared coding challenge before allowing entry through this portal.</summary>
public sealed class PortalQuestionGate : MonoBehaviour
{
    [SerializeField] private MultipleChoiceQuestionData[] puzzleQuestions;
    [SerializeField, HideInInspector] private string portalId;
    private bool unlocked;
    private bool asking;
    public bool IsUnlocked => unlocked;
    private string SaveId => gameObject.scene.path + "/portal-question/" + portalId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid() ||
            string.IsNullOrEmpty(gameObject.scene.path) ||
            UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
        var id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this);
        if (id.targetObjectId == 0 || portalId == id.ToString()) return;
        portalId = id.ToString();
        UnityEditor.EditorUtility.SetDirty(this);
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
    }
#endif

    private void Start()
    {
        unlocked = !string.IsNullOrEmpty(portalId) &&
            SaveSlots.Read(SaveSlots.Active)?.solvedPortalQuestions?.Contains(SaveId) == true;
    }

    public void RequestEntry(Action enter)
    {
        if (!isActiveAndEnabled || asking || DoorJavaQuestionPanel.IsOpen) return;
        if (unlocked) { enter?.Invoke(); return; }
        var questions = QuestionSystem.Instance != null
            ? QuestionSystem.Instance.GetQuestionsForCurrentPlayer() : null;
        if (questions == null || !Array.Exists(questions, q => q != null)) questions = puzzleQuestions;
        if (questions == null || !Array.Exists(questions, q => q != null)) return;
        asking = true;
        DoorJavaQuestionPanel.Show(questions, () => Accept(enter), () => { if (this != null) asking = false; }, forPortal: true);
    }

    private void Accept(Action enter)
    {
        asking = false;
        if (this == null || !isActiveAndEnabled) return;
        unlocked = true;
        if (!string.IsNullOrEmpty(portalId))
        {
            var saved = SaveSlots.Read(SaveSlots.Active) ??
                new SaveSlots.Record { character = PlayerPrefs.GetInt("SelectedCharacter", 0) };
            if (saved.solvedPortalQuestions == null)
                saved.solvedPortalQuestions = new System.Collections.Generic.List<string>();
            if (!saved.solvedPortalQuestions.Contains(SaveId)) saved.solvedPortalQuestions.Add(SaveId);
            SaveSlots.Write(saved);
        }
        enter?.Invoke();
    }
}
