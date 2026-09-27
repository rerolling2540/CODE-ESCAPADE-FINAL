using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Class & Level Configuration")]
    [SerializeField] private HeroClassData heroClassData;
    [SerializeField] private int currentLevel = 1;

    [Header("Component Reference")]
    [SerializeField] private CombatantView combatantView;

    private void Awake()
    {
        if (combatantView == null)
        {
            combatantView = GetComponent<CombatantView>();
        }
    }

    private void Start()
    {
        InitializePlayer();
    }

    [ContextMenu("Initialize Player")]
    public void InitializePlayer()
    {
        if (combatantView == null)
        {
            combatantView = GetComponent<CombatantView>();
        }

        if (heroClassData != null && combatantView != null)
        {
            combatantView.InitializeClass(heroClassData, currentLevel);
        }
    }

    public void LevelUp()
    {
        currentLevel++;
        InitializePlayer();
    }

    private void OnValidate()
    {
        // Automatically updates health bar in Play Mode whenever tweaking currentLevel in Inspector
        if (Application.isPlaying && combatantView != null && heroClassData != null)
        {
            InitializePlayer();
        }
    }
}