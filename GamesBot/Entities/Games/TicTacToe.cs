using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Games.Entities.Games.TicTacToe
{
    public enum GameResult
    {
        None,
        Draw,
        Player1,
        Player2
    }

    public class TicTacToeGame : IDisposable
    {
        private string id;
        private DiscordChannel chn;
        private DiscordMessage msg;
        private DiscordClient client;
        private DiscordUser player1;
        private DiscordUser player2;
        private Actor[] table;
        private Actor currentActor;
        private TaskCompletionSource<GameResult> tsc;
        private GameResult result;
        private int round = 1;

        public void Dispose()
        {
            chn = null;
            client = null;
            msg = null;
            player1 = player2 = null;
            table = null;
            currentActor = Actor.None;
            tsc = null;
            result = default;
            round = -1;
            GC.Collect();
        }

        static readonly int[][] Spots = new int[][]
        {
            new[]{ 0, 1, 2 },
            new[]{ 3, 4, 5 },
            new[]{ 6, 7, 8 },

            new[]{ 0, 3, 6 },
            new[]{ 1, 4, 7 },
            new[]{ 2, 5, 8 },

            new[]{ 0, 4, 8 },
            new[]{ 2, 4, 6 },
        };

        public TicTacToeGame(DiscordClient c, DiscordChannel channel, DiscordUser user1, DiscordUser user2)
        {
            id = Guid.NewGuid().ToString("N");
            client = c;
            chn = channel;
            player1 = user1;
            player2 = user2;
            table = Enumerable.Repeat(Actor.None, 9).ToArray();
            currentActor = Actor.Player1;
            tsc = new TaskCompletionSource<GameResult>();
        }

        public async Task<GameResult> StartAsync()
        {
            await UpdateAsync();
            client.ComponentInteractionCreated += this.OnInteractionInvoked;
            var val = (result = await tsc.Task);

            await msg.DeleteAsync();
            await UpdateAsync();
            return val;
        }

        bool CanInteract(DiscordUser other)
        {
            if (currentActor == Actor.Player2 && IsVersusBot)
                return true;

            if (currentActor == Actor.Player1 && player1 != other)
                return false;

            if (currentActor == Actor.Player2 && player2 != other)
                return false;

            return true;
        }

        static readonly Random Rnd = new Random(Environment.TickCount);

        int MakeGuess()
        {
            int position;

            while (true)
            {
                position = Rnd.Next(0, table.Length - 1);

                if (table[position] == Actor.None)
                    break;
            }

            return position;
        }

        bool IsCompleted()
        {
            bool completed = false;

            foreach (var spot in Spots)
            {
                completed = table[spot[0]] == currentActor
                    && table[spot[1]] == currentActor
                    && table[spot[2]] == currentActor;

                if (completed)
                    break;
            }

            if (completed)
                SetResult(currentActor == Actor.Player1 ? GameResult.Player1 : GameResult.Player2);

            return completed;
        }

        bool CanMove(bool finish = true)
        {
            var result = table.Any(x => x == Actor.None);

            if (!result && finish)
                SetResult(GameResult.Draw);

            return result;
        }

        void SetResult(GameResult result)
        {
            if (!tsc.Task.IsCompleted)
            {
                client.ComponentInteractionCreated -= OnInteractionInvoked;
                tsc.TrySetResult(result);
            }
        }

        async Task OnInteractionInvoked(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Message.Id != msg.Id)
                return;

            var itr = e.Interaction;
            await itr.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            if (!CanInteract(e.User))
                return;

            if (itr.Data.CustomId.IndexOf("ttt_action_") != -1)
            {
                round++;

                var option = int.Parse(itr.Data.CustomId.Replace("ttt_action_", string.Empty));
                table[option] = currentActor;
                //await UpdateAsync();

                if (IsGameEnded())
                    return;

                if (currentActor == Actor.Player1)
                    currentActor = Actor.Player2;
                else
                    currentActor = Actor.Player1;

                //if (!IsVersusBot)
                //    await UpdateAsync();

                if (currentActor == Actor.Player2 && IsVersusBot)
                {
                    var choice = MakeGuess();
                    table[choice] = Actor.Player2;

                    if (IsGameEnded())
                        return;

                    currentActor = Actor.Player1;
                    round++;
                }

                await UpdateAsync();
            }

            bool IsGameEnded()
            {
                if (IsCompleted())
                    return true;

                if (!CanMove())
                    return true;

                return false;
            }
        }

        DateTime startTime = DateTime.UtcNow;
        DateTime endTime;

        async Task UpdateAsync()
        {
            DiscordEmbedBuilder embed = null;

            if (result == GameResult.None)
            {
                embed = new DiscordEmbedBuilder()
                    .WithAuthor("JOGO DA VELHA", iconUrl: client.CurrentUser.AvatarUrl)
                    .WithFooter("Iniciado Em ")
                    .WithTimestamp(startTime)
                    .AddField("Rodadas", round.ToString(), true)
                    .AddField("Jogador Atual", currentActor == Actor.Player1
                        ? player1.Mention : player2.Mention, true)
                    .WithColor(DiscordColor.SpringGreen);
            }
            else
            {
                if (endTime == default)
                    endTime = DateTime.UtcNow;

                embed = new DiscordEmbedBuilder()
                    .WithAuthor("JOGO DA VELHA", iconUrl: client.CurrentUser.AvatarUrl)
                    .WithDescription("**FIM DE JOGO**")
                    .AddField("Rodadas", round.ToString(), true)
                    .WithColor(result switch
                    {
                        GameResult.Draw => new DiscordColor(255, 128, 0),
                        GameResult.Player1 => new DiscordColor(88, 101, 242),
                        GameResult.Player2 => new DiscordColor(237, 66, 69),
                        _ => DiscordColor.Blurple
                    })
                    .WithFooter("Encerrado Em")
                    .WithTimestamp(endTime)
                    .WithTimestamp(DateTime.Now);

                if (result == GameResult.Draw)
                    embed.AddField("Vencedor", "**EMPATE**");
                else
                    embed.AddField("Vencedor", currentActor == Actor.Player1
                        ? player1.Mention : player2.Mention);
            }

            var builder = new DiscordMessageBuilder
            {
                Content = $"\u200b\n{player1.Mention} :vs: {player2.Mention}",
                Embed = embed,
            };

            if (result != GameResult.None)
                msg = null;
            else
            {
                var rows = new List<DiscordActionRowComponent>();
                var buttons = new DiscordButtonComponent[3];
                var count = 0;

                for (int i = 0; i < table.Length; i++)
                {
                    var actor = table[i];
                    AppendRow(ref rows, ref count, ref buttons);
                    DSharpPlus.ButtonStyle style;

                    if (actor == Actor.None)
                        style = DSharpPlus.ButtonStyle.Secondary;
                    else
                    {
                        if (actor == Actor.Player1)
                            style = DSharpPlus.ButtonStyle.Primary;
                        else
                            style = DSharpPlus.ButtonStyle.Danger;
                    }

                    var label = actor == Actor.None ? (i + 1).ToString()
                        : (actor == Actor.Player1 ? "X" : "O");

                    var suffix = actor != Actor.None ? "_disabled" : string.Empty;
                    buttons[i % 3] = new DiscordButtonComponent(style, $"ttt_action_{i}{suffix}",
                        label, actor != Actor.None);

                    count++;
                }

                AppendRow(ref rows, ref count, ref buttons);
                builder.AddComponents(rows);
            }

            await Task.Delay(100);

            if (msg == null)
                msg = await builder.SendAsync(chn);
            else
                msg = await builder.ModifyAsync(msg);

            void AppendRow(ref List<DiscordActionRowComponent> rows, ref int count, ref DiscordButtonComponent[] buttons)
            {
                if (count == 3)
                {
                    rows.Add(new DiscordActionRowComponent(buttons));
                    buttons = new DiscordButtonComponent[3];
                    count = 0;
                }
            }
        }

        public bool IsVersusBot
            => this.player2.IsBot && this.player2.IsCurrent;

        public bool HasMovements()
            => this.table.Any(x => x == Actor.None);
    }

    public enum Actor
    {
        None,
        Player1,
        Player2
    }
}
