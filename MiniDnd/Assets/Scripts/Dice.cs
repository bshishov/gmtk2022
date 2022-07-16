public enum Dice
{
    Attack,
    Defend,
    Intelligence
}

public struct DiceRoll
{
    public Dice Dice;
    public int Value;

    public static DiceRoll Random(Dice dice, int modifier = 0)
    {
        return new DiceRoll
        {
            Dice = dice,
            Value = UnityEngine.Random.Range(1, 6) + modifier 
        };
    }

    public override string ToString()
    {
        return $"{Dice} {Value}";
    }
}
