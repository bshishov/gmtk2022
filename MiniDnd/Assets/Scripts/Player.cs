public class Player
{
    public string Location = "start";
    public int Power = 6;

    public bool ShouldStartNewEncounter = true;

    public void EndEncounter()
    {
        ShouldStartNewEncounter = true;
    }
}