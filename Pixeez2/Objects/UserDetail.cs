﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pixeez2.Objects
{
 
    public class UserDetail : IHasUser
    {

        [JsonProperty("profile")]
        public Profile Profile { get; set; }

        [JsonProperty("profile_publicity")]
        public ProfilePublicity ProfilePublicity { get; set; }

        [JsonProperty("workspace")]
        public Workspace Workspace { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

    }

    public class ProfilePublicity
    {
        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("birth_day")]
        public string BirthDay { get; set; }

        [JsonProperty("birth_year")]
        public string BirthYear { get; set; }

        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("pawoo")]
        public bool Pawoo { get; set; }

    }
}
