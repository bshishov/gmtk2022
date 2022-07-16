namespace Activities
{
    public class S2Greetings : Activity
    {
        public override string Text(Player player) =>
            "Greetings, luckiest of gods. You've just won your personal world to rule and collect cash from. There is just one problem; your 'fellow' gods awarded you with a world full of primitive people(sore losers, I know) This world has no roulettes, no casinos and no martinies! But you will shape to have not only martinis but also blackjack and hok.. Anyway, you will do it by throwing your eminent dice over and over again until things will just work out. Let's begin";

        public override void PlayerRoll(Player player, DiceRoll roll)
        {
            player.EndEncounter();
        }
    }
}