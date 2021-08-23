using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Games.Entities.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Games.Entities
{
    public class GamesBotConfiguration
    {
        static readonly IReadOnlyDictionary<string, Type> types;
        static readonly string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Config.json");

        static GamesBotConfiguration()
        {
            types = new Dictionary<string, Type>
            {
                ["discord"] = typeof(DiscordSettings),
                ["commands"] = typeof(CommandsSettings),
                ["interactivity"] = typeof(InteractivitySettings),
            };
        }

        static string GetAttachmentKey(Type t)
        {
            foreach (var (key, type) in types)
                if (type == t)
                    return key;

            return t.Name.ToLowerInvariant();
        }

        private Dictionary<string, object> attachments;

        public static GamesBotConfiguration GetOrCreateDefault()
        {
            var cfg = new GamesBotConfiguration();
            cfg.Load();
            return cfg;
        }

        public GamesBotConfiguration()
        {
            this.attachments = new Dictionary<string, object>();
        }

        public TAttachment GetAttachment<TAttachment>(string key = default) where TAttachment : class
        {
            key ??= GetAttachmentKey(typeof(TAttachment));

            if (this.attachments.TryGetValue(key, out var raw))
            {
                if (raw is JObject obj)
                    return obj.ToObject<TAttachment>();

                return raw as TAttachment;
            }

            return default;
        }

        public void Load()
        {
            var update = false;

            if (!File.Exists(fileName))
            {
                this.attachments = types.ToDictionary(x => x.Key,
                    x => Activator.CreateInstance(x.Value));

                update = true;
            }
            else
            {
                this.attachments = JObject.Parse(File.ReadAllText(fileName))
                    .ToObject<Dictionary<string, object>>();

                var result = types.Where(x => !this.attachments.ContainsKey(x.Key));

                if (result.Any())
                    update = true;

                foreach (var kvp in result)
                    this.attachments[kvp.Key] = Activator.CreateInstance(kvp.Value);
            }

            if (update)
                this.Save();
        }

        public void Save()
        {
            try
            {
                string json = JObject.FromObject(this.attachments).ToString(Formatting.Indented);
                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
