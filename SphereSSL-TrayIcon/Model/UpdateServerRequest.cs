using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SphereSSL_TrayIcon.Model
{
    public class UpdateServerRequest
    {
        [JsonPropertyName("serverUrl")]
        public string ServerUrl { get; set; }

        [JsonPropertyName("serverPort")]
        public int ServerPort { get; set; }
    }
}
