using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot {
    public class ValorantStatusResponse {
        public string status;
        public string region;
        public Dictionary<string, string[]> data;
    }
}
