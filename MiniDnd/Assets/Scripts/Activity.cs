using System;

public abstract class Activity
{
    public float Weight = 100f;
    public string Name => GetType().Name;
    public abstract string Text(Player player);
    public virtual Dice[] AvailableDice(Player player) => Array.Empty<Dice>();
    public virtual bool CanBeRolled(Player player) => false;
    public abstract void PlayerRoll(Player player, DiceRoll roll);
    public virtual void BeforeStart(Player player) {}
    public virtual void AfterEnd(Player player) {}
}