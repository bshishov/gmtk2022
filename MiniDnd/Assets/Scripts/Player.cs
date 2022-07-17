public class Player
{
    public string Location = "start";
    public int Power = 6;

    public bool ShouldStartNewEncounter = true;
    public string NextExpectedActivity;
    public DiceRoll LastDiceRoll;

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

    public void ShowText(string text)
    {
        throw new System.NotImplementedException();
    }
}