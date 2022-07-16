namespace Activities
{
    public class S3DiceyGod1 : Activity
    {
        public override string Text(Player player) =>
            "Graced by your presence, your faithful servants want to know what behavior is favourable in the eyes of their deity.";

        public override void PlayerRoll(Player player, DiceRoll roll)
        {
            player.EndEncounter();
        }
    }
}