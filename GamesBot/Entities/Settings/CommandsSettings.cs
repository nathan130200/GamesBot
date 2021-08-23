using System;
using System.Collections.Immutable;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace Games.Entities.Settings
{
    public class CommandsSettings
    {
        [JsonProperty("prefixes")]
        public ImmutableArray<string> Prefixes { get; private set; } = ImmutableArray.Create("g.", "g!");

        [JsonProperty("allow_dm")]
        public bool AllowDm { get; private set; } = false;

        [JsonProperty("use_dm_help")]
        public bool UseDmHelp { get; private set; } = true;

        [JsonProperty("case_sensitive")]
        public bool CaseSensitive { get; private set; } = false;

        [JsonProperty("enable_mention_prefix")]
        public bool EnableMentionPrefix { get; private set; } = false;

        public CommandsNextConfiguration Build(IServiceProvider services)
        {
            var config = new CommandsNextConfiguration
            {
                CaseSensitive = CaseSensitive,
                DmHelp = UseDmHelp,
                EnableDms = AllowDm,
                EnableMentionPrefix = EnableMentionPrefix,
                StringPrefixes = Prefixes
            };

            if (services != null)
                config.Services = services;

            //if (factory != null)
            //{
            //    try { config.Services = factory?.Invoke(param); }
            //    catch (Exception e)
            //    {
            //        Debug.WriteLine(e);
            //    }
            //}

            return config;
        }
    }
}
