using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionSystem : Singleton<ActionSystem>
{
    private List<GameAction> reactions = null;
    public bool IsPerforming { get; private set; } = false;
    private static Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> preSubs = new();
    private static Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> postSubs = new();
    private static Dictionary<Type, Func<GameAction, IEnumerator>> performers = new();
    public void Perform(GameAction action, Action OnPerformFinished = null)
    {
        StartCoroutine(WaitAndPerform(action, OnPerformFinished));
    }

    private IEnumerator WaitAndPerform(GameAction action, Action OnPerformFinished)
    {
        while (IsPerforming)
            yield return null;

        IsPerforming = true;

        yield return Flow(action);

        IsPerforming = false;

        OnPerformFinished?.Invoke();
    }
    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }
    private IEnumerator Flow(GameAction action, Action OnFlowFinished = null)
    {
        reactions = action.PreReactions;
        PerformSubscribers(action, preSubs);
        yield return PerformReactions();

        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions();

        reactions = action.PostReactions;
        PerformSubscribers(action, postSubs);
        yield return PerformReactions();

        OnFlowFinished?.Invoke();
    }
    private IEnumerator PerformPerformer(GameAction action)
    {
        Type type = action.GetType();
        if (performers.ContainsKey(type))
        {
            yield return performers[type](action);
        }
    }
    private void PerformSubscribers(GameAction action, Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> subs)
    {
        Type type = action.GetType();
        if (subs.ContainsKey(type))
        {
            foreach (var sub in subs[type].Values.ToArray())
            {
                sub(action);
            }
        }
    }
    private IEnumerator PerformReactions()
    {
        foreach (var reaction in reactions)
        {
            yield return Flow(reaction);
        }
    }
    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if (performers.ContainsKey(type)) performers[type] = wrappedPerformer;
        else performers.Add(type, wrappedPerformer);
    }
    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if (performers.ContainsKey(type)) performers.Remove(type);
    }
    public static void SubscribedReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        if (reaction == null) return;
        var subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        if (!subs.TryGetValue(typeof(T), out var callbacks))
        {
            callbacks = new Dictionary<Delegate, Action<GameAction>>();
            subs.Add(typeof(T), callbacks);
        }
        // Retain the original delegate as the key, not a newly-created wrapper.
        if (!callbacks.ContainsKey(reaction))
            callbacks.Add(reaction, action => reaction((T)action));
    }

    public static void UnSubscribedReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        var subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        if (reaction != null && subs.TryGetValue(typeof(T), out var callbacks))
        {
            callbacks.Remove(reaction);
            if (callbacks.Count == 0) subs.Remove(typeof(T));
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistrations()
    {
        preSubs.Clear();
        postSubs.Clear();
        performers.Clear();
    }
}
