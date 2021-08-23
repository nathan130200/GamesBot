using System;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Newtonsoft.Json;

namespace Games.Entities.Settings
{
    public class InteractivitySettings
    {
        [JsonProperty("button_behavior")]
        public ButtonPaginationBehavior ButtonBehavior { get; private set; } = ButtonPaginationBehavior.DeleteMessage;

        [JsonProperty("pagination_behaviour")]
        public PaginationBehaviour PaginationBehaviour { get; private set; }

        [JsonProperty("pagination_deletion")]
        public PaginationDeletion PaginationDeletion { get; private set; }

        [JsonProperty("poll_behaviour")]
        public PollBehaviour PollBehaviour { get; private set; }

        [JsonProperty("response_behaviour")]
        public InteractionResponseBehavior ResponseBehavior { get; private set; }

        [JsonProperty("timeout")]
        public TimeSpan Timeout { get; private set; }

        public InteractivityConfiguration Build()
        {
            return new InteractivityConfiguration
            {
                AckPaginationButtons = true,
                ButtonBehavior = ButtonBehavior,
                PaginationBehaviour = PaginationBehaviour,
                PaginationDeletion = PaginationDeletion,
                PollBehaviour = PollBehaviour,
                ResponseBehavior = ResponseBehavior,
                Timeout = Timeout
            };
        }
    }
}
