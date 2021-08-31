using System.Threading.Tasks;
using System;
using System.IO;
using Discord;
using Discord.Commands;
using System.Speech.Synthesis;


namespace DiscordBot.Modules {
    [Name("Voice")]
    public class AudioModule : ModuleBase<SocketCommandContext> {


        private readonly AudioService _service;

        public AudioModule(AudioService service) {
            _service = service;
        }


        [Command("join", RunMode = RunMode.Async)]
        [Summary("Join your voice channel")]
        public async Task JoinCmd() {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        [Command("leave", RunMode = RunMode.Async)]
        [Summary("Leave your voice channel")]
        public async Task LeaveCmd() {
            await _service.LeaveAudio(Context.Guild);
        }

        [Command("stop", RunMode = RunMode.Async), Alias("skip")]
        [Summary("Stop the current audio playback")]
        public async Task StopCmd() {
            await _service.StopAudio(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel);
        }

        [Command("stopall", RunMode = RunMode.Async)]
        [Summary("Stop audio playback and clear the queue")]
        public async Task StopAllCmd() {
            await _service.StopAllAudio(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel);
        }

        [Command("play", RunMode = RunMode.Async), Alias("p"), Priority(1)]
        [Summary("Play a sound")]
        public async Task Play(double volume, [Remainder] string sound) {
            await _service.SendAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, sound, Math.Max(0, Math.Min(1, volume)));
        }

        [Command("play", RunMode = RunMode.Async), Alias("p"), Priority(0)]
        [Summary("Play a sound")]
        public async Task Play([Remainder] string sound) {
            await _service.SendAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, sound);
        }


        [Command("youtube", RunMode = RunMode.Async), Alias("yt", "song"), Priority(1)]
        [Summary("Play a video with volume")]
        public async Task Youtube(double volume, [Remainder] string video) {
            await _service.SendYTAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, video, Math.Max(0, Math.Min(1, volume)));
        }

        [Command("youtube", RunMode = RunMode.Async), Alias("yt", "song"), Priority(0)]
        [Summary("Play a video")]
        public async Task Youtube([Remainder] string video) {
            await _service.SendYTAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, video);
        }


        [Command("playlist_create")]
        [Summary("Create a playlist with a given song / video")]
        public async Task CreatePlaylist(string playlist, [Remainder] string song) {
            await _service.CreatePlaylist(Context.Guild, Context.Channel, playlist, song);
        }

        [Command("playlist_add")]
        [Summary("Add a song to a playlist")]
        public async Task AddToPlaylist(string playlist, [Remainder] string song) {
            await _service.AddPlaylist(Context.Guild, Context.Channel, playlist, song);
        }

        [Command("playlist")]
        [Summary("Play a saved playlist")]
        public async Task PlayPlaylist(string playlist) {
            await _service.PlayPlaylist(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, playlist);
        }

        [Command("play"), Alias("p")]
        [Summary("List the sounds")] 
        public async Task Play() {
            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218),
            };
            string list = "`";
            string name = "Available Sounds";

            if (!Directory.Exists($"sounds/{Context.Guild.Id.ToString()}")) {
                Directory.CreateDirectory($"sounds/{Context.Guild.Id.ToString()}");
            }

            int alternate = 1;

            int longestSound = 0;
            foreach (string file in Directory.GetFiles("sounds")) {
                var fileName = $"{file.Replace(".mp3", "").Replace("sounds\\", "")}";
                if (name.Length > longestSound)
                    longestSound = fileName.Length;
            }

            foreach (string file in Directory.GetFiles($"sounds/{Context.Guild.Id.ToString()}")) { 
                var fileName = $"{file.Replace(".mp3", "").Replace($"sounds/{Context.Guild.Id}\\", "")}";
                if (name.Length > longestSound)
                    longestSound = fileName.Length;
            }

            longestSound += 1;

            foreach (string file in Directory.GetFiles("sounds")) {
                list += $"{file.Replace(".mp3", "").Replace("sounds\\", "")}".PadRight(longestSound, ' ');

                if (alternate % 3 == 0) {
                    list += "\n";
                }

                if (list.Length >= 1024 - longestSound) {
                    builder.AddField(x => {
                        x.Name = name;
                        x.Value = list + "`";
                        x.IsInline = false;
                    });

                    name = "Continued... wow you've got a lot of sounds";
                    list = "`";
                    alternate = 0;
                }
                alternate += 1;
            }

            foreach (string file in Directory.GetFiles($"sounds/{Context.Guild.Id.ToString()}")) {
                list += $"{file.Replace(".mp3", "").Replace($"sounds/{Context.Guild.Id}\\", "")}".PadRight(longestSound, ' ');

                if (alternate % 3 == 0) {
                    list += "\n";
                }

                if (list.Length >= 1024 - longestSound) {
                    builder.AddField(x => {
                        x.Name = name;
                        x.Value = list + "`";
                        x.IsInline = false;
                    });

                    name = "Continued... wow you've got a lot of sounds";
                    list = "`";
                    alternate = 0;
                }

                alternate += 1;
            }

            builder.AddField(x => {
                x.Name = name;
                x.Value = list + "`";
                x.IsInline = false;
            });
            await ReplyAsync("", false, builder.Build());
        }

        [Command("speak", RunMode = RunMode.Async), Priority(3), Alias("s")]
        [Summary("Speak words with volume and speed")]
        public async Task Speak(double volume, double speed, [Remainder] string speech) {
            await _service.SpeakAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, speech, Math.Max(0, Math.Min(1, volume)), Math.Max(0, speed));
        }

        [Command("speak", RunMode = RunMode.Async), Priority(2), Alias("s")]
        [Summary("Speak words with volume")]
        public async Task Speak(double volume, [Remainder] string speech) {
            await _service.SpeakAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, speech, Math.Max(0, Math.Min(1, volume)));
        }

        [Command("speak", RunMode = RunMode.Async), Priority(1), Alias("s")]
        [Summary("Speak words")]
        public async Task Speak([Remainder] string speech) {
            await _service.SpeakAudioAsync(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, speech);
        }

        [Command("speak", RunMode = RunMode.Async), Priority(0), Alias("s")]
        [Summary("List Voices")]
        public async Task Speak() {
            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218),
            };
            string list = "`";
            foreach (var voice in new SpeechSynthesizer().GetInstalledVoices()) {
                list += voice.VoiceInfo.Name.Replace("Microsoft ", "").Replace(" Desktop", "") + "\n";
            }

            builder.AddField(x => {
                x.Name = "Available Voices";
                x.Value = list + "`";
                x.IsInline = false;
            });
            await ReplyAsync("", false, builder.Build());
        }
    }
}