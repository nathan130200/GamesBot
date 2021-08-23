using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;

namespace Games
{
    internal static class Util
    {
        static volatile int GameId;
        static readonly HttpClient Http = new();

        public static async Task<int> FetchShardsAsync(string token)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/gateway/bot");
            req.Headers.TryAddWithoutValidation("Authorization", $"Bot {token}");

            string json = "{}";
            using var res = await Http.SendAsync(req);

            if (res.IsSuccessStatusCode)
                json = await res.Content.ReadAsStringAsync();

            var raw = JObject.Parse(json);

            if (raw.ContainsKey("shards"))
                return raw.Value<int>("shards");

            return 0;
        }

        static volatile int AsyncTaskId = 1;

        public static void StartAsyncTask(Func<Task> factory, string name = default)
        {
            name ??= $"AsyncTask/#{AsyncTaskId++}";

            Task.Run(async () =>
            {
                try
                {
                    if (factory != null)
                    {
                        var task = factory();
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[{0}]: {1}\n", name, ex);
                }
            });
        }

        public static DiscordEmoji Emoji(string name)
        {
            var obj = (Dictionary<string, string>)typeof(DiscordEmoji).GetTypeInfo()
                .GetProperty("UnicodeEmojis", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

            if (obj.TryGetValue(name, out var unicode))
                return DiscordEmoji.FromUnicode(unicode);

            throw new KeyNotFoundException(nameof(name));
        }

        public static string NextGameId()
            => $"{GameId++:x4}";
    }
}
