namespace Encounters
{
    public class Encounter2 : Encounter
    {
        public override string Text(Player player) => "Второй";
        public override bool Check(Player player) => player.Location == "start";
        public override Dice[] AvailableDice(Player player) => new []{ Dice.Attack, Dice.Defend };
        public override void Act(Player player, DiceRoll roll)
        {
            if (roll.Value > 3)
                player.EndEncounter();
        }
    }
}