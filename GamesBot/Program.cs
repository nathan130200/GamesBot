using System;
using System.Threading.Tasks;
using Games.Entities;

namespace Games
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "GAMES: DISCORD BOT";

            try
            {
                var bot = new GamesBot(GamesBotConfiguration.GetOrCreateDefault());
                await bot.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Task.Delay(5000);
                Environment.Exit(1);
            }
        }
    }
}
