public abstract class Encounter
{
    public float Weight = 100f;
    public string Name => GetType().Name;
    public abstract string Text(Player player);
    public abstract Dice[] AvailableDice(Player player);
    public virtual bool Check(Player player) => false;
    public abstract void Act(Player player, DiceRoll roll);

    public virtual void BeforeEncounterStart(Player player) {}
    public virtual void AfterEncounterEnd(Player player) {}
}