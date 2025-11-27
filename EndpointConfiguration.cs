using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UniversalConnectionTester
{
    public class EndpointConfiguration
    {
        [JsonPropertyName("endpoints")]
        public List<EndpointDefinition> Endpoints { get; set; } = new();
    }

    public class EndpointDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; } = string.Empty;

        [JsonPropertyName("connectionType")]
        public ConnectionType ConnectionType { get; set; }
    }

    public enum ConnectionType
    {
        Oracle,
        Mssql,
        Http,
        Https,
        Ping
    }
}
