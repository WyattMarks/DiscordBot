using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Web;
using CliWrap;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using DiscordBot.Objects;
using System.Net.Http;
using Newtonsoft.Json;

namespace DiscordBot {
    public class AudioService {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly LoggingService _log;

        private readonly ConcurrentDictionary<ulong, CurrentAudioInformation> CurrentAudioClients = new ConcurrentDictionary<ulong, CurrentAudioInformation>();


        public AudioService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config, LoggingService log) {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
            _log = log;

            _discord.UserVoiceStateUpdated += UserVoiceChange;
            _discord.MessageReceived += CheckForAudioUpload;
        }

        private async Task CheckForAudioUpload(SocketMessage s) {
            var msg = s as SocketUserMessage;
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;
            if (msg.Attachments.Count <= 0) return;

            if (msg.Channel is SocketTextChannel textChannel && msg.Author is SocketGuildUser user) { //Is in a server (not PM)

                bool isAudio = false;
                foreach (var attachment in msg.Attachments) {
                    if (attachment.Filename.EndsWith(".mp3") && msg.Content.ToLower().Contains("upload")) {
                        isAudio = true;
                        if (!user.GuildPermissions.ManageGuild)
                            break;
                        HttpClient client = new HttpClient();
                        var response = await client.GetAsync(attachment.Url);
                        using (var fs = new FileStream($"sounds/{textChannel.Guild.Id.ToString()}/{attachment.Filename.ToLower()}", FileMode.Create)) {
                            await response.Content.CopyToAsync(fs);
                            fs.Close();
                        }
                    }
                }

                if (isAudio)
                    await msg.DeleteAsync();
            }
        }

        private async Task UserVoiceChange(SocketUser user, SocketVoiceState previous, SocketVoiceState current) {
            if (current.VoiceChannel != null && current.VoiceChannel != previous.VoiceChannel) {
                CurrentAudioInformation client;
                if (CurrentAudioClients.TryGetValue(current.VoiceChannel.Guild.Id, out client)) {
                    IGuild guild = current.VoiceChannel.Guild;
                    if (client.voiceChannel == current.VoiceChannel && string.IsNullOrEmpty(client.playing))
                        await SpeakAudioAsync(guild, await guild.GetDefaultChannelAsync(), current.VoiceChannel, $"Welcome, {user.Username}"); //Kinda annoying
                }
            }
        }

        private async Task CheckAudioClient(IGuild guild, IVoiceChannel target, IAudioClient client) {
            if (client.ConnectionState == ConnectionState.Connected) {
                return;
            } else {
                await LeaveAudio(guild);
                await JoinAudio(guild, target);
            }
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target) {
            if (target.Guild.Id != guild.Id) {
                return;
            }

            var audioClient = await target.ConnectAsync();

            if (CurrentAudioClients.TryAdd(guild.Id, new CurrentAudioInformation(new ConcurrentQueue<string>(), audioClient, null, null, null, (SocketVoiceChannel)target))) {
                await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Connected to voice on {guild.Name}."));
            }
        }

        public async Task LeaveAudio(IGuild guild) {
            CurrentAudioInformation client;
            if (CurrentAudioClients.TryRemove(guild.Id, out client)) {
                if (client.currentStream != null)
                    await client.currentStream.FlushAsync();
                await client.client.StopAsync();
                await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Disconnected from voice on {guild.Name}."));
            }
        }

        public async Task StopAudio(IGuild guild, IMessageChannel channel, IVoiceChannel target) {
            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {
                if (!string.IsNullOrEmpty(client.playing)) {
                    client.cancelTokenSource.Cancel();
                    client.playing = null;
                } else {
                    await channel.SendMessageAsync("Stop what? I'm not talking.");
                }
            } else {
                await channel.SendMessageAsync("I'm not even in the voice channel, what do you want me to stop?");
            }
        }

        public async Task StopAllAudio(IGuild guild, IMessageChannel c, IVoiceChannel target) {
            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client))
                client.queue = new ConcurrentQueue<string>();
            await StopAudio(guild, c, target);
        }

        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, IVoiceChannel target, string path, double volume = 1.0) {
            volume *= 0.5;

            if (!Directory.Exists($"sounds/{guild.Id.ToString()}")) {
                Directory.CreateDirectory($"sounds/{guild.Id.ToString()}");
            }

            if (!File.Exists($"sounds/{path}.mp3") && !File.Exists($"sounds/{guild.Id.ToString()}/{path}.mp3")) {
                await channel.SendMessageAsync("Sound does not exist.");
                return;
            }


            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {
                await CheckAudioClient(guild, target, client.client);
                await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Starting playback of {path} in {guild.Name}"));

                if (!string.IsNullOrEmpty(client.playing)) {
                    client.queue.Enqueue(path);
                    await channel.SendMessageAsync($"Added {path} to the queue.");
                    return;
                }

                client.playing = path;

                if (client.currentStream == null) {
                    client.currentStream = client.client.CreatePCMStream(AudioApplication.Mixed, 98304, 200);
                }


                string hardpath = File.Exists($"sounds/{path}.mp3") ? $"\"sounds/{path}.mp3\"" : $"\"sounds/{guild.Id.ToString()}/{path}.mp3\"";

                var memoryStream = new MemoryStream();

                await Cli.Wrap("ffmpeg")
                    .WithArguments($" -hide_banner -loglevel panic -i {hardpath} -filter:a \"volume = {volume}\" -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                    .ExecuteAsync();

                try {
                    client.cancelTokenSource = new CancellationTokenSource();
                    await client.currentStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length, client.cancelTokenSource.Token);
                } catch (OperationCanceledException e) {
                    await channel.SendMessageAsync($"Stopped {path}");
                    client.paused = path;
                } finally {
                    await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Finished playing {path}."));
                    await CheckQueue(guild, channel, target);
                }
            } else {
                await JoinAudio(guild, target);
                await SendAudioAsync(guild, channel, target, path, volume);
            }
        }

        public async Task SpeakAudioAsync(IGuild guild, IMessageChannel channel, IVoiceChannel target, string speech, double volume = 1.0, double speed = 1.0) {
            volume *= 0.5;


            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {
                await CheckAudioClient(guild, target, client.client);
                await _log.LogAsync(new LogMessage(LogSeverity.Info, "Audio", $"Starting to speak \"{speech}\" in {guild.Name}"));

                if (!string.IsNullOrEmpty(client.playing)) {
                    client.queue.Enqueue("SPEAK" + speech); //SPEAK prefix just so we can differentiate between audio files and text to speech
                    await channel.SendMessageAsync($"Added \"{speech}\" to the queue.");
                    return;
                }

                client.playing = "SPEAK" + speech;

                if (client.currentStream == null) {
                    client.currentStream = client.client.CreatePCMStream(AudioApplication.Mixed, 98304, 200);
                }

                var speechStream = new MemoryStream();

                var synthesizer = new SpeechSynthesizer();
                var synthFormat = new SpeechAudioFormatInfo(EncodingFormat.Pcm, (int)(98304.0 / (speed)), 16, 1, 16000, 2, null);

                string[] words = speech.Split(' ');
                foreach (var voice in synthesizer.GetInstalledVoices()) {
                    if (voice.VoiceInfo.Name.ToUpper().Equals($"MICROSOFT {words[0].ToUpper()} DESKTOP") || voice.VoiceInfo.Name.ToUpper().Equals($"MICROSOFT {words[0].ToUpper()}")) {
                        synthesizer.SelectVoice(voice.VoiceInfo.Name);
                        words[0] = "";
                        speech = string.Join(" ", words);
                    }
                }

                synthesizer.SetOutputToAudioStream(speechStream, synthFormat);

                synthesizer.Speak(speech);
                speechStream.Position = 0;

                try {
                    client.cancelTokenSource = new CancellationTokenSource();
                    await client.currentStream.WriteAsync(speechStream.ToArray(), 0, (int)speechStream.Length, client.cancelTokenSource.Token);
                } catch (OperationCanceledException e) {
                    await channel.SendMessageAsync($"Stopped \"{speech}\"");
                } finally {
                    await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Finished speaking \"{speech}\"."));
                    await CheckQueue(guild, channel, target);
                }
            } else {
                await JoinAudio(guild, target);
                await SpeakAudioAsync(guild, channel, target, speech, volume);
            }
        }

        public async Task<PlaylistEntry> SearchYoutube(YoutubeClient youtube, string search, bool retry = false) {
            string youtubeID = GetYouTubeVideoIdFromUrl(search);

            if (youtubeID == null || youtubeID.Length == 0) {

                try {
                    var videos = await youtube.Search.GetVideosAsync(search);
                    return new PlaylistEntry(videos[0].Id.ToString(), videos[0].Title);
                } catch (YoutubeExplode.Exceptions.YoutubeExplodeException e) {
                    //INCOMING: Very hacky and stupid way of fixing this. For some reason popular music artists sometimes give error ("Cannot extract video author") and don't play.
                    //Therefore we just search for a reupload once to see if we can find a working vid
                    if (retry) {
                        return null;
                    } else {
                        return await SearchYoutube(youtube, search + " lyrics", true);
                    }
                }
            } else {
                var vid = await youtube.Videos.GetAsync(youtubeID);
                return new PlaylistEntry(vid.Id.ToString(), vid.Title);
            }
        }

        public async Task SendYTAudioAsync(IGuild guild, IMessageChannel channel, IVoiceChannel target, string path, double volume = 1.0) {

            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {
                await CheckAudioClient(guild, target, client.client);

                var youtube = new YoutubeClient();

                if (!string.IsNullOrEmpty(client.playing)) {
                    client.queue.Enqueue("YOUTUBE" + path);
                    await channel.SendMessageAsync($"Added {path} to the queue.");
                    return;
                }

                client.playing = path;

                var entry = await SearchYoutube(youtube, path);
                if (entry == null) {
                    await channel.SendMessageAsync($"Error searching for song");
                    await CheckQueue(guild, channel, target);
                    return;
                }
                string youtubeID = entry.youtubeID;

                await _log.LogAsync(new LogMessage(LogSeverity.Info, "Audio", $"Starting playback of \"{path}\" in {guild.Name}"));

                if (client.currentStream == null) {
                    client.currentStream = client.client.CreatePCMStream(AudioApplication.Mixed, 98304, 200);
                }



                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(youtubeID);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                var memoryStream = new MemoryStream();
                var video = await youtube.Videos.Streams.GetAsync(streamInfo);

                await Cli.Wrap("ffmpeg")
                    .WithArguments($"-hide_banner -loglevel panic -i pipe:0 -filter:a \"volume = {volume}\" -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardInputPipe(PipeSource.FromStream(video))
                    .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                    .ExecuteAsync();
                try {
                    client.cancelTokenSource = new CancellationTokenSource();
                    await client.currentStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length, client.cancelTokenSource.Token);
                } catch (OperationCanceledException e) {
                    await channel.SendMessageAsync($"Stopped {path}");
                } finally {
                    await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Finished playing {path}."));
                    await CheckQueue(guild, channel, target);
                }

            } else {
                await JoinAudio(guild, target);
                await SendYTAudioAsync(guild, channel, target, path, volume);
            }


        }

        private async Task CheckQueue(IGuild guild, IMessageChannel channel, IVoiceChannel target) {
            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {
                string nextPlayback;
                client.playing = null;
                if (client.queue.TryDequeue(out nextPlayback)) {
                    await _log.LogAsync(new LogMessage(LogSeverity.Verbose, "Audio", $"Took {nextPlayback} off queue"));

                    if (nextPlayback.StartsWith("SPEAK")) {
                        nextPlayback = nextPlayback.Substring(5);
                        await SpeakAudioAsync(guild, channel, target, nextPlayback);
                    } else if (nextPlayback.StartsWith("YOUTUBE")) {
                        await SendYTAudioAsync(guild, channel, target, nextPlayback.Substring(7));
                    } else {
                        await SendAudioAsync(guild, channel, target, nextPlayback);
                    }
                }

                if (client.currentStream != null)
                    await client.currentStream.FlushAsync();
            }
        }

        private bool Contains(object[] array, object value) {
            foreach (var arrObj in array) {
                if (arrObj.Equals(value))
                    return true;
            }
            return false;
        }
        public string GetYouTubeVideoIdFromUrl(string url) {
            Uri uri = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) {
                try {
                    uri = new UriBuilder("http", url).Uri;
                } catch {
                    // invalid url
                    return null;
                }
            }

            string host = uri.Host;
            string[] youTubeHosts = { "www.youtube.com", "youtube.com", "youtu.be", "www.youtu.be" };

            if (!Contains(youTubeHosts, host))
                return "";

            var query = HttpUtility.ParseQueryString(uri.Query);

            if (Contains(query.AllKeys, "v")) {
                return Regex.Match(query["v"], @"^[a-zA-Z0-9_-]{11}$").Value;
            } else if (Contains(query.AllKeys, "u")) {
                // some urls have something like "u=/watch?v=AAAAAAAAA16"
                return Regex.Match(query["u"], @"/watch\?v=([a-zA-Z0-9_-]{11})").Groups[1].Value;
            } else {
                // remove a trailing forward space
                var last = uri.Segments[uri.Segments.Length - 1].Replace("/", "");
                if (Regex.IsMatch(last, @"^v=[a-zA-Z0-9_-]{11}$"))
                    return last.Replace("v=", "");

                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                    return "";

                return Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
            }
        }

        public async Task CreatePlaylist(IGuild guild, IMessageChannel channel, string name, string song) {
            if (!Directory.Exists($"sounds/{guild.Id.ToString()}")) {
                Directory.CreateDirectory($"sounds/{guild.Id.ToString()}");
            }

            if (!Directory.Exists($"sounds/{guild.Id.ToString()}/playlists")) {
                Directory.CreateDirectory($"sounds/{guild.Id.ToString()}/playlists");
            }

            if (File.Exists($"sounds/{guild.Id.ToString()}/playlists/{name}.json")) {
                await channel.SendMessageAsync("Playlist already exists.. did you mean playlist_add?");
                return;
            }


            YoutubeClient youtube = new YoutubeClient();
            var entry = await SearchYoutube(youtube, song);
            if (entry == null) {
                await channel.SendMessageAsync("Error finding that song... try a different wording?");
                return;
            }

            await channel.SendMessageAsync($"Created and added '{entry.title}' to the playlist");

            PlaylistEntry[] array = { entry };
            await SavePlaylist(guild, name, array);


        }

        public async Task SavePlaylist(IGuild guild, string name, PlaylistEntry[] playlist) {
            File.WriteAllText($"sounds/{guild.Id.ToString()}/playlists/{name}.json", JsonConvert.SerializeObject(playlist));
        }

        public async Task<PlaylistEntry[]> LoadPlaylist(string guild, string name) {
            return JsonConvert.DeserializeObject<PlaylistEntry[]>(File.ReadAllText($"sounds/{guild}/playlists/{name}.json"));
        }

        public async Task AddPlaylist(IGuild guild, IMessageChannel channel, string name, string song) {
            if (!Directory.Exists($"sounds/{guild.Id.ToString()}") || !Directory.Exists($"sounds/{guild.Id.ToString()}/playlists") || !File.Exists($"sounds/{guild.Id.ToString()}/playlists/{name}.json")) {
                await channel.SendMessageAsync("Playlist doesn't exist... did you mean playlist_create?");
                return;
            }

            YoutubeClient youtube = new YoutubeClient();
            var entry = await SearchYoutube(youtube, song);
            if (entry == null) {
                await channel.SendMessageAsync("Error finding that song... try a different wording?");
                return;
            }


            PlaylistEntry[] playlist = await LoadPlaylist(guild.Id.ToString(), name);

            PlaylistEntry[] newPlaylist = new PlaylistEntry[playlist.Length + 1];
            for (int i = 0; i < playlist.Length; i++) {
                if (playlist[i].youtubeID == entry.youtubeID) {
                    await channel.SendMessageAsync("That's already in the playlist dude.");
                    return;
                }
                newPlaylist[i] = playlist[i];
            }
            newPlaylist[playlist.Length] = entry;

            await channel.SendMessageAsync($"Added '{entry.title}' to the playlist");

            await SavePlaylist(guild, name, newPlaylist);
        }

        public async Task PlayPlaylist(IGuild guild, IMessageChannel channel, IVoiceChannel target, string name) {
            if (!Directory.Exists($"sounds/{guild.Id.ToString()}") || !Directory.Exists($"sounds/{guild.Id.ToString()}/playlists") || !File.Exists($"sounds/{guild.Id.ToString()}/playlists/{name}.json")) {
                await channel.SendMessageAsync("Playlist doesn't exist...");
                return;
            }

            CurrentAudioInformation client;
            if (CurrentAudioClients.TryGetValue(guild.Id, out client)) {

                PlaylistEntry[] playlist = await LoadPlaylist(guild.Id.ToString(), name);
                for (int i = 0; i < playlist.Length; i++) {
                    client.queue.Enqueue("YOUTUBEhttps://youtube.com/watch?v=" + playlist[i].youtubeID); //Is this cheating? No, because it's only me and I say so.
                }
                await CheckQueue(guild, channel, target);
            } else {
                await JoinAudio(guild, target);
                await PlayPlaylist(guild, channel, target, name);
            }
        }
    }
}