using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using Games.Entities;
using Games.Entities.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Games
{
    public class GamesBot
    {
        public static GamesBot Instance { get; private set; }
        public GamesBotConfiguration Config { get; private set; }
        public ConcurrentDictionary<int, DiscordClient> Shards { get; private set; }
        public IServiceProvider Services { get; private set; }

        public GamesBot(GamesBotConfiguration config)
        {
            Instance = this;

            this.Config = config;

            this.Services = new ServiceCollection()
                // TODO: More services soon
                .BuildServiceProvider(true);
        }

        async Task SetupAsync()
        {
            var dcfg = this.Config.GetAttachment<DiscordSettings>();
            var count = await Util.FetchShardsAsync(dcfg.Token);

            if (count <= 0)
                throw new InvalidOperationException("Cannot obtain shards from gateway.");

            await this.InitializeShardsAsync(dcfg, count);
            Util.StartAsyncTask(this.ScheduleUpdateShardCount, nameof(ScheduleUpdateShardCount));
        }

        async Task InitializeShardsAsync(DiscordSettings config, int count, int start = 0)
        {
            var shards = new Dictionary<int, DiscordClient>();

            for (int i = start; i < count; i++)
                shards[i] = this.CreateShard(config, i..count);

            this.Shards = new ConcurrentDictionary<int, DiscordClient>(shards);

            var tasks = new List<Task>();

            foreach (var shard in shards.Select(x => x.Value))
                tasks.Add(shard.ConnectAsync());

            await Task.WhenAll(tasks);
        }

        DiscordClient CreateShard(DiscordSettings config, Range range)
        {
            var client = new DiscordClient(config.Build(range));
            {
                var cnext = client.UseCommandsNext(this.Config.GetAttachment<CommandsSettings>().Build(this.Services));
                cnext.RegisterCommands(typeof(GamesBot).Assembly);
                cnext.CommandErrored += async (_, evt) =>
                {
                    await Task.Yield();

                    var ex = evt.Exception;

                    while (ex is AggregateException)
                        ex = ex.InnerException;

                    if (ex is CommandNotFoundException)
                        return;

                    if (ex.Message.Contains("overload", StringComparison.InvariantCultureIgnoreCase))
                        return;

                    Console.WriteLine("[Commands/Exception] {0}\n", ex);
                };
            }
            client.UseInteractivity(this.Config.GetAttachment<InteractivitySettings>().Build());
            return client;
        }

        async Task ScheduleUpdateShardCount()
        {
            var config = this.Config.GetAttachment<DiscordSettings>();
            var token = config.Token;

            while (true)
            {
                var count = await Util.FetchShardsAsync(token);

                if (count > this.Shards.Count)
                {
                    int i;

                    for (i = 0; i < this.Shards.Count; i++)
                        if (this.Shards.ContainsKey(i))
                            continue;

                    await this.InitializeShardsAsync(config, count, i);
                }
                else
                {
                    int i;

                    var shards = new List<DiscordClient>();

                    for (i = this.Shards.Count; i > 0; i--)
                    {
                        if (i > count)
                            break;

                        if (this.Shards.TryGetValue(i, out var s))
                            shards.Add(s);
                    }

                    _ = Task.WhenAll(shards.Select(x => x.DisconnectAsync()));
                }

                await Task.Delay(TimeSpan.FromMinutes(1.5d));
            }
        }

        public async Task RunAsync()
        {
            await this.SetupAsync();
            await Task.Delay(-1);
        }
    }
}
