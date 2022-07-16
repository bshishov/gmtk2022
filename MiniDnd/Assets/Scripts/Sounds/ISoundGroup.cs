namespace TSUtils.Sounds
{
    public interface ISoundGroup
    {
        int GetMaxConcurrentSounds();

        string GetId();
    }
}