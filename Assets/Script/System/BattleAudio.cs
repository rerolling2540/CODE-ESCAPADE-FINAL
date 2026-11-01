using System.Collections.Generic;
using UnityEngine;

public class BattleAudio : MonoBehaviour
{
    private static BattleAudio instance;
    private BattleSfxLibrary library;
    private readonly AudioSource[] voices = new AudioSource[8];
    private readonly Dictionary<BattleSound, float> lastPlayed = new Dictionary<BattleSound, float>();
    private int nextVoice;

    public static void EnsureInstance()
    {
        if (instance == null) new GameObject("Battle Sound Effects").AddComponent<BattleAudio>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        library = Resources.Load<BattleSfxLibrary>("BattleSfxLibrary");
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
            GameAudio.Route(voices[i], false);
        }
        if (library != null)
            foreach (var cue in library.cues)
                if (cue.clips != null)
                    foreach (var clip in cue.clips)
                        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
    }

    void OnEnable()
    {
        ActionSystem.SubscribedReaction<DrawCardsGA>(OnDraw, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<EnemyTurnGA>(OnEnemyTurn, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<AddStatusEffectGA>(OnStatus, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<HealGA>(OnHeal, ReactionTiming.PRE);
        ActionSystem.SubscribedReaction<KillEnemyGA>(OnEnemyDefeat, ReactionTiming.PRE);
    }

    void OnDisable()
    {
        ActionSystem.UnSubscribedReaction<DrawCardsGA>(OnDraw, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(OnEnemyTurn, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<AddStatusEffectGA>(OnStatus, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<HealGA>(OnHeal, ReactionTiming.PRE);
        ActionSystem.UnSubscribedReaction<KillEnemyGA>(OnEnemyDefeat, ReactionTiming.PRE);
    }

    void OnDestroy() { if (instance == this) instance = null; }
    void OnDraw(DrawCardsGA action) { if (action.Amount > 0) Play(BattleSound.CardDraw); }
    void OnEnemyTurn(EnemyTurnGA action) { Play(BattleSound.TurnEnd); }
    void OnEnemyDefeat(KillEnemyGA action) { if (action.EnemyView != null) Play(BattleSound.EnemyDefeat); }
    void OnStatus(AddStatusEffectGA action)
    {
        if (action.StackCount <= 0 || action.Targets == null || !action.Targets.Exists(t => t != null && t.CurrentHealth > 0)) return;
        BattleSound sound = action.StatusEffectType switch
        {
            StatusEffectType.ARMOR => BattleSound.Armor,
            StatusEffectType.BURN => BattleSound.FireCast,
            StatusEffectType.POISON => BattleSound.MagicCast,
            StatusEffectType.BLEED => BattleSound.Hit,
            StatusEffectType.FROST => BattleSound.MagicCast,
            StatusEffectType.NATURE => BattleSound.Heal,
            _ => BattleSound.MagicCast
        };
        Play(sound);
    }
    void OnHeal(HealGA action)
    {
        if (action.Amount > 0 && action.Targets != null && action.Targets.Exists(t => t != null && t.CurrentHealth > 0 && t.CurrentHealth < t.MaxHealth))
            Play(BattleSound.Heal);
    }

    public static void Play(BattleSound sound)
    {
        if (instance == null || instance.library == null) return;
        float cooldown = sound == BattleSound.CardHover ? 0.12f : 0.06f;
        if (instance.lastPlayed.TryGetValue(sound, out float previous) && Time.unscaledTime - previous < cooldown) return;
        var cue = instance.library.cues.Find(c => c.sound == sound);
        if (cue == null || cue.clips == null || cue.clips.Length == 0) return;
        var clip = cue.clips[Random.Range(0, cue.clips.Length)];
        if (clip == null) return;
        instance.lastPlayed[sound] = Time.unscaledTime;
        if (sound == BattleSound.Victory || sound == BattleSound.Defeat)
        {
            // Let the result finish even when the return-to-map scene loads.
            var result = new GameObject("Battle Result Sound");
            DontDestroyOnLoad(result);
            var playback = result.AddComponent<AudioSource>();
            playback.playOnAwake = false;
            playback.spatialBlend = 0f;
            GameAudio.Route(playback, false);
            playback.clip = clip;
            playback.volume = cue.volume;
            playback.Play();
            Destroy(result, clip.length + 0.2f);
            return;
        }
        AudioSource source = null;
        foreach (var voice in instance.voices) if (!voice.isPlaying) { source = voice; break; }
        if (source == null) source = instance.voices[instance.nextVoice++ % instance.voices.Length];
        source.Stop();
        source.clip = clip;
        source.volume = cue.volume;
        source.Play();
    }

    public static bool IsFireCard(CardData card)
    {
        if (card == null) return false;
        string name = card.name.ToLowerInvariant();
        return name.Contains("fire") || name.Contains("burn") || name.Contains("ignite");
    }

    public static void HeroAttack(CardData card)
    {
        if (card == null) return;
        bool damage = card.ManualTargetEffect is DealDamageEffect || card.ManualTargetEffect is StatusComboDamageEffect ||
            (card.OtherEffects != null && card.OtherEffects.Exists(e => e != null &&
                (e.Effect is DealDamageEffect || e.Effect is StatusComboDamageEffect)));
        if (!damage) return;
        Play(IsFireCard(card) ? BattleSound.FireCast : SelectedHero.IsMage ? BattleSound.MagicCast :
            SelectedHero.IsArcher ? BattleSound.ArrowRelease : BattleSound.SwordSwing);
    }

    public static void Impact(DealDamageGA action, CombatantView target, int previousHealth)
    {
        if (target.CurrentHealth == previousHealth) { Play(BattleSound.Block); return; }
        bool magic = action.Caster is EnemyView enemy ? enemy.IsRanged : action.Caster is HeroView && SelectedHero.IsMage;
        Play(IsFireCard(action.CardSource) ? BattleSound.FireHit : magic ? BattleSound.MagicHit : BattleSound.Hit);
    }
}
