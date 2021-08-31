using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot {
    public class PlaylistEntry {
        public string youtubeID;
        public string title;
        public PlaylistEntry(string id, string tit) {
            youtubeID = id;
            title = tit;
        }
    }
}
