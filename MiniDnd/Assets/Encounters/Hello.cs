namespace Encounters
{
    public class Hello : Encounter
    {
        public override string Text(Player player) => "Первый";
        public override bool Check(Player player) => player.Power > 5;
        public override Dice[] AvailableDice(Player player) => new []{ Dice.Attack, Dice.Defend };
        public override void Act(Player player, DiceRoll roll)
        {
            if (roll.Value > 3)
                player.EndEncounter();
        }
    }
}