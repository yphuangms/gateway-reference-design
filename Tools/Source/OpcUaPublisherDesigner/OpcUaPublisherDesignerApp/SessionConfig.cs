using Newtonsoft.Json;
using System.Collections.Generic;

namespace PublisherDesignerApp
{
    public class SessionInfo
    {
        [JsonProperty(PropertyName = "SessionName")]
        public string sessionName { get; set; }

        [JsonProperty(PropertyName = "ProfilePath")]
        public string profilePath { get; set; }

        [JsonProperty(PropertyName = "SourceType")]
        public string sourceType { get; set; }
    };

    public class SessionConfig
    {
        [JsonIgnore]
        public bool IsDirty = false;

        [JsonProperty(PropertyName = "SourceSessions")]
        public List<SessionInfo> sessions = new List<SessionInfo>();
    }

}