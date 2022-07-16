namespace Activities
{
    public class S1StartWasAwkward : Activity
    {
        public override string Text(Player player) => "Well that was awkward, maybe we can start now?";

        public override void PlayerRoll(Player player, DiceRoll roll)
        {
            player.Goto<S2Greetings>();
        }
    }
}