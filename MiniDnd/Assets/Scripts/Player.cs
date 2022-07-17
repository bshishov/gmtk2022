using System;
using System.Collections.Generic;
using Konklav;

public class Player : IContext
{
    public string Location = "start";
    public int Power = 6;
    public bool ShouldStartNewEncounter = true;
    public string NextExpectedActivity;
    public DiceRoll LastDiceRoll;
    public HashSet<string> VisitedHashSet = new HashSet<string>();
    public string CurrentActivity;
    private readonly Action<string> _showTextCallback;

    public Player(Action<string> showTextCallback)
    {
        _showTextCallback = showTextCallback;
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

    public void ShowText(string text)
    {
        _showTextCallback?.Invoke(text);
    }

    public void Debug(string message)
    {
        UnityEngine.Debug.Log($"[{CurrentActivity}] {message}");   
    }
}