using DSharpPlus;
using DSharpPlus.Entities;

namespace Games.Entities.Games.Hangman
{
    public class HangmanGame : BaseGame<HangmanGameResult>
    {
        static readonly string[] Words = new[]
        {
            "vaca", "galinha", "passaro",
            "jacaré", "capivara", "gato",
            "cachorro", "papagaio", "peixe"
        };

        private DiscordClient client;
        private DiscordChannel chn;
        private DiscordUser player;

        public HangmanGame(DiscordClient c, DiscordChannel cc, DiscordUser p) : base(BaseGameType.Hangman)
        {
            client = c;
            chn = cc;
            player = p;
        }
    }

    public enum HangmanGameResult
    {
        None,
        Win,
        Loss
    }
}
