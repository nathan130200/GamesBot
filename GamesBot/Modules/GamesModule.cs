using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Games.Entities.Games.TicTacToe;

namespace Games.Modules
{
    [ModuleLifespan(ModuleLifespan.Singleton)]
    public class GamesModule : BaseCommandModule
    {
        [Command, Aliases("ttt")]
        public async Task TicTacToe(CommandContext ctx, [RemainingText] DiscordUser user = default)
        {
            if (user == null)
                user = ctx.Client.CurrentUser;
            else
            {
                if (user.IsBot && !user.IsCurrent)
                {
                    await ctx.RespondAsync($"{ctx.User.Mention} :x: Outros bots não podem jogar!");
                    return;
                }
            }

            using (var ttt = new TicTacToeGame(ctx.Client, ctx.Channel, ctx.User, user))
                await ttt.StartAsync();
        }
    }
}
