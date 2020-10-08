using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace MonkeyBot
{
    public class App
    {
        [JsonProperty("pixiv-username")]
        public string UserName { get; set; }

        [JsonProperty("pixiv-password")]
        public string Password { get; set; }

        [JsonProperty("discord-token")]
        public string DiscordToken { get; set; }
    }
}
