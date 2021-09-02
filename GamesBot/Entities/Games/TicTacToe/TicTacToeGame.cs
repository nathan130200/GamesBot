using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Games.Entities.Games.TicTacToe
{
    public class TicTacToeGame : BaseGame<TicTacToeGameResult>
    {
        private string id;
        private DiscordChannel chn;
        private DiscordMessage msg;
        private DiscordClient client;
        private DiscordUser player1;
        private DiscordUser player2;
        private SpotOwner[] table;
        private SpotOwner currentActor;
        private TaskCompletionSource<TicTacToeGameResult> tsc;
        private TicTacToeGameResult result;
        private int round = 1;

        public override void Dispose()
        {
            chn = null;
            client = null;
            msg = null;
            player1 = player2 = null;
            table = null;
            currentActor = SpotOwner.None;
            tsc = null;
            result = default;
            round = -1;
            GC.Collect();
        }

        public TicTacToeGame(DiscordClient c, DiscordChannel channel, DiscordUser user1, DiscordUser user2) : base(BaseGameType.TicTacToe)
        {
            id = Guid.NewGuid().ToString("N");
            client = c;
            chn = channel;
            player1 = user1;
            player2 = user2;
            table = Enumerable.Repeat(SpotOwner.None, 9).ToArray();
            currentActor = SpotOwner.Player1;
            tsc = new TaskCompletionSource<TicTacToeGameResult>();
        }

        public override async Task<TicTacToeGameResult> GetResultAsync()
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
            if (currentActor == SpotOwner.Player2 && IsVersusBot)
                return true;

            if (currentActor == SpotOwner.Player1 && player1 != other)
                return false;

            if (currentActor == SpotOwner.Player2 && player2 != other)
                return false;

            return true;
        }

        static readonly Random Rnd = new Random(Environment.TickCount);
        Spot winGuess;

        int MakeGuess()
        {
            int position;

            while (true)
            {
                position = Rnd.Next(0, table.Length - 1);

                if (table[position] == SpotOwner.None)
                    break;
            }

            Debug.WriteLine("* default guess generator (found empty spot: " + position + ")");

            return position;
        }

        bool IsEmptySpot(Spot s)
        {
            return table[s[0]] == SpotOwner.None
                && table[s[1]] == SpotOwner.None
                && table[s[2]] == SpotOwner.None;
        }

        bool CanBotUseSpot(Spot s, out int result)
        {
            result = -1;

            if (s == null)
                return false;

            return s.Data.Any(x => table[x] == SpotOwner.None);
        }

        static IEnumerable<Spot> FindNeighbourSpots(int pos)
        {
            Debug.WriteLine("neighbour test: " + pos);

            var result = new List<Spot>();

            foreach (var spot in Spot.Values)
            {
                if (spot.Data.Any(x => x == pos))
                {
                    result.Add(spot);
                    continue;
                }
            }

            return result;
        }

        int lastPlayerGuess;

        int MakeSmartGuess()
        {
            var pos = -1;
            var step = 1;

            Debug.WriteLine("* test spot");

            foreach (var item in FindNeighbourSpots(lastPlayerGuess)
                .Where(x => x.Data.Count(i => table[i] == SpotOwner.None) != 0))
            {
                Debug.WriteLine("* found neighbour spot: {0} (type: {1})",
                    item, item.Type);

                var len = item.Data.Count(n => table[n] == SpotOwner.Player1);

                if (len > 1)
                {
                    if (len == 2)
                    {
                        Debug.WriteLine("** neighbour spot is valid to blocking the player");
                        return item.Data.FirstOrDefault(x => table[x] == SpotOwner.None);
                    }
                    else
                    {
                        Debug.WriteLine("** neighbour spot is valid but we need an guess first");

                        var rnd = new Random(Environment.TickCount);

                        while (true)
                        {
                            if (!item.Data.Any(x => table[x] == SpotOwner.None))
                                break;

                            pos = rnd.Next(0, item.Data.Length);
                            
                            if (table[pos] == SpotOwner.None)
                                return pos;
                        }

                        return MakeGuess();
                    }
                }
            }

        test:
            if (winGuess == null)
            {
                winGuess = Spot.Values.FirstOrDefault(x => IsEmptySpot(x));
                Debug.WriteLine("@ initial random guess spot");
                goto test;
            }
            else
            {
                Debug.WriteLine("* process spot");

                if (!CanBotUseSpot(winGuess, out pos))
                {
                    Debug.WriteLine("# cannot use spot");

                    if (step == 0)
                    {
                        step++;
                        winGuess = Spot.Values.FirstOrDefault(x => IsEmptySpot(x));
                        Debug.WriteLine("@ shuffle guess spot ");
                        goto test;
                    }
                }
            }

            if (pos == -1)
                return MakeGuess();

            return pos;
        }

        bool IsCompleted()
        {
            bool completed = false;

            foreach (var spot in Spot.Values)
            {
                completed = table[spot[0]] == currentActor
                    && table[spot[1]] == currentActor
                    && table[spot[2]] == currentActor;

                if (completed)
                    break;
            }

            if (completed)
                SetResult(currentActor == SpotOwner.Player1 ? TicTacToeGameResult.Player1 : TicTacToeGameResult.Player2);

            return completed;
        }

        bool CanMove(bool finish = true)
        {
            var result = table.Any(x => x == SpotOwner.None);

            if (!result && finish)
                SetResult(TicTacToeGameResult.Draw);

            return result;
        }

        void SetResult(TicTacToeGameResult result)
        {
            if (!tsc.Task.IsCompleted)
            {
                client.ComponentInteractionCreated -= OnInteractionInvoked;
                tsc.TrySetResult(result);
            }
        }

        async Task OnInteractionInvoked(DiscordClient sender, ComponentInteractionCreateEventArgs evt)
        {
            await Task.Yield();

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessGameAsync(evt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        async Task ProcessGameAsync(ComponentInteractionCreateEventArgs e)
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

                if (IsGameEnded())
                    return;

                if (currentActor == SpotOwner.Player1)
                    currentActor = SpotOwner.Player2;
                else
                    currentActor = SpotOwner.Player1;

                if (currentActor == SpotOwner.Player2 && IsVersusBot)
                {
                    lastPlayerGuess = option;

                    var choice = MakeSmartGuess();

                    if (choice == -1)
                        choice = MakeGuess();

                    table[choice] = SpotOwner.Player2;

                    if (IsGameEnded())
                        return;

                    currentActor = SpotOwner.Player1;
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

            if (result == TicTacToeGameResult.None)
            {
                embed = new DiscordEmbedBuilder()
                    .WithAuthor("JOGO DA VELHA", iconUrl: client.CurrentUser.AvatarUrl)
                    .WithFooter("Iniciado Em ")
                    .WithTimestamp(startTime)
                    .AddField("Rodadas", round.ToString(), true)
                    .AddField("Jogador Atual", currentActor == SpotOwner.Player1
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
                        TicTacToeGameResult.Draw => new DiscordColor(255, 128, 0),
                        TicTacToeGameResult.Player1 => new DiscordColor(88, 101, 242),
                        TicTacToeGameResult.Player2 => new DiscordColor(237, 66, 69),
                        _ => DiscordColor.Blurple
                    })
                    .WithFooter("Encerrado Em")
                    .WithTimestamp(endTime)
                    .WithTimestamp(DateTime.Now);

                if (result == TicTacToeGameResult.Draw)
                    embed.AddField("Vencedor", "**EMPATE**");
                else
                    embed.AddField("Vencedor", currentActor == SpotOwner.Player1
                        ? player1.Mention : player2.Mention);
            }

            var builder = new DiscordMessageBuilder
            {
                Content = $"\u200b\n{player1.Mention} :vs: {player2.Mention}",
                Embed = embed,
            };

            if (result != TicTacToeGameResult.None)
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

                    if (actor == SpotOwner.None)
                        style = DSharpPlus.ButtonStyle.Secondary;
                    else
                    {
                        if (actor == SpotOwner.Player1)
                            style = DSharpPlus.ButtonStyle.Primary;
                        else
                            style = DSharpPlus.ButtonStyle.Danger;
                    }

                    var label = actor == SpotOwner.None ? (i + 1).ToString()
                        : (actor == SpotOwner.Player1 ? "X" : "O");

                    var suffix = actor != SpotOwner.None ? "_disabled" : string.Empty;
                    buttons[i % 3] = new DiscordButtonComponent(style, $"ttt_action_{i}{suffix}",
                        label, actor != SpotOwner.None);

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
            => this.table.Any(x => x == SpotOwner.None);
    }
}
