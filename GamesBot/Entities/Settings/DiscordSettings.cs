using System;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Games.Entities.Settings
{
    public class DiscordSettings
    {
        [JsonProperty("token"), JsonRequired]
        public string Token { get; private set; } = "changeme";

        [JsonProperty("http_timeout")]
        public TimeSpan HttpTimeout { get; private set; } = TimeSpan.FromSeconds(45d);

        [JsonProperty("use_member_cache")]
        public bool UseMemberCache { get; private set; } = true;

        [JsonProperty("use_verbosity")]
        public bool UseVerbosity { get; private set; } = false;

        [JsonProperty("enable_reconnect")]
        public bool EnableReconnect { get; private set; } = true;

        [JsonProperty("use_relative_ratelimit")]
        public bool UseRelativeRatelimit { get; private set; } = true;

        [JsonProperty("intents")]
        public DiscordIntents Intents { get; private set; } = DiscordIntents.AllUnprivileged;

        public DiscordConfiguration Build(Range range)
        {
            return new DiscordConfiguration()
            {
                Token = Token,
                AutoReconnect = EnableReconnect,
                AlwaysCacheMembers = UseMemberCache,
                HttpTimeout = HttpTimeout,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                Intents = Intents,
                MinimumLogLevel = this.UseVerbosity ? LogLevel.Debug : LogLevel.Warning,
                LogTimestampFormat = "HH:mm:ss.fff",
                TokenType = TokenType.Bot,
                UseRelativeRatelimit = UseRelativeRatelimit,
                ShardId = range.Start.Value,
                ShardCount = range.End.Value
            };
        }
    }
}
