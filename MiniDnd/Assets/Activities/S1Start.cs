namespace Activities
{
    public class S1Start : Activity
    {
        public override string Text(Player player) => "Embark on adventure?";
        public override void PlayerRoll(Player player, DiceRoll roll)
        {
            if (roll.Value <= 5)
                player.Goto<S2Greetings>();
            else
                player.Goto<S1StartWasAwkward>();
        }
    }
}