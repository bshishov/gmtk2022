using System;
using System.Collections.Generic;
using Konklav;
using UnityEngine;


public class Interaction
{
    public readonly Action Action;
    public readonly float Delay;

    public Interaction(Action action, float delay)
    {
        Action = action;
        Delay = delay;
    }
}


public class Player : IContext
{
    public string Location = "start";
    public int Power = 6;
    public bool ShouldStartNewEncounter = true;
    public string NextExpectedActivity;
    public DiceRoll LastDiceRoll;
    public HashSet<string> VisitedHashSet = new HashSet<string>();
    public HashSet<string> Tags = new HashSet<string>();
    public string CurrentActivity;
    private readonly Action<string> _showTextCallback;
    private readonly Action<string> _showImageCallback;
    public readonly List<Interaction> InteractionQueue = new List<Interaction>();

    public Player(Action<string> showTextCallback, Action<string> showImageCallback)
    {
        _showTextCallback = showTextCallback;
        _showImageCallback = showImageCallback;
    }

    public void EndEncounter()
    {
        
        ShouldStartNewEncounter = true;
    }

    public void Goto<T>() 
        where T: Activity
    {
        ShouldStartNewEncounter = true;
        NextExpectedActivity = typeof(T).Name;
    }
    
    public void Goto(string activityName)
    {
        ShouldStartNewEncounter = true;
        NextExpectedActivity = activityName;
    }

    public bool Visited(string activityName)
    {
        return VisitedHashSet.Contains(activityName);
    }

    public void End()
    {
        ShouldStartNewEncounter = true;
    }

    public int LastDiceRollValue => LastDiceRoll.Value;

    public void Schedule(Action action, float delay = 0f)
    {
        InteractionQueue.Add(new Interaction(action, delay));    
    }

    public void ScheduleWait(float time)
    {
        InteractionQueue.Add(new Interaction(() => UnityEngine.Debug.Log($"[{CurrentActivity}]Waiting for {time}"), time));
    }

    public void ShowText(string text)
    {
        if (_showTextCallback != null)
        {
            Schedule(() => _showTextCallback(text));
            ScheduleWait(4f + text.Length * 0.05f);
        }
    }

    public void Debug(string message)
    {
        UnityEngine.Debug.Log($"[{CurrentActivity}] {message}");   
    }

    public void ShowImage(string imageName)
    {
        if (_showImageCallback != null)
        {
            Schedule(() => _showImageCallback.Invoke(imageName));
        }
    }

    public void SetTag(string tag)
    {
        Tags.Add(tag.ToLowerInvariant());
    }

    public bool HasTag(string tag)
    {
        return Tags.Contains(tag.ToLowerInvariant());
    }

    public void Quit()
    {
        Schedule(Application.Quit);
    }

    public void Wait(float seconds)
    {
        ScheduleWait(seconds);
    }
}