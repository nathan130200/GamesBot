using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Games.Modules
{
    [ModuleLifespan(ModuleLifespan.Singleton)]
    public class InformationModule : BaseCommandModule
    {
        [Command]
        public async Task Ping(CommandContext ctx)
        {
            var watch = Stopwatch.StartNew();
            await ctx.TriggerTypingAsync();
            watch.Stop();

            var avg = (watch.ElapsedMilliseconds + ctx.Client.Ping) / 2;

            var color = avg switch
            {
                <= 100 => DiscordColor.Green,
                > 100 and <= 150 => DiscordColor.Yellow,
                > 150 => DiscordColor.Red
            };

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                .WithAuthor("Games: Ping", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Rest", watch.ElapsedMilliseconds + "ms")
                .AddField("Gateway", ctx.Client.Ping + "ms")
                .AddField("Avg", avg + "ms")
                .WithColor(color));
        }
    }
}
