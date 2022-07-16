public class Player
{
    public string Location = "start";
    public int Power = 6;

    public bool ShouldStartNewEncounter = true;
    public string NextExpectedActivity;

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
}