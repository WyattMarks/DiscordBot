using Discord.Audio;
using System.Collections.Concurrent;
using System.Threading;
using Discord.WebSocket;
using System;

namespace DiscordBot.Objects {
    class CurrentAudioInformation {
        public ConcurrentQueue<string> queue;
        public IAudioClient client;
        public AudioOutStream currentStream;
        public CancellationTokenSource cancelTokenSource;
        public SocketVoiceChannel voiceChannel;
        public string playing = null;
        public string paused = "";
        public DateTime startTime;
        public DateTime pauseTime;
        public CurrentAudioInformation(ConcurrentQueue<string> queue, IAudioClient client, AudioOutStream currentStream, CancellationTokenSource cancelTokenSource, string playing, SocketVoiceChannel channel) {
            this.queue = queue;
            this.client = client;
            this.currentStream = currentStream;
            this.cancelTokenSource = cancelTokenSource;
            this.playing = playing;
            voiceChannel = channel;
        }
    }
}
