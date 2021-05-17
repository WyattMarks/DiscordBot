using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DiscordBot {
    public class LoggingService {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private ConcurrentQueue<LogMessage> logQueue;
        private bool writing = false;

        private string _logDirectory { get; }
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
        public LoggingService(DiscordSocketClient discord, CommandService commands) {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private async Task OnLogAsync(LogMessage msg) {
            if (!Directory.Exists(_logDirectory))    
                Directory.CreateDirectory(_logDirectory);

            logQueue = logQueue ?? new ConcurrentQueue<LogMessage>();

            if (writing) { //Added a queue so that we avoid the File being locked
                logQueue.Enqueue(msg);

                return;
            }

            string logText = $"{DateTime.Now.ToLocalTime().ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            try {
                File.AppendAllText(_logFile, logText + "\n");     
            } catch (Exception e) {
                Console.WriteLine("Error opening log file " + e.Message); //Should never happen, now.
            } finally {
                 await CheckQueue();
            }

            await Console.Out.WriteLineAsync(logText);       // Write the log text to the console
        }

        private async Task CheckQueue() {
            writing = false;
            LogMessage next;
            if (logQueue.TryDequeue(out next)) {
                await LogAsync(next);
            }
        }

        public Task LogAsync(LogMessage msg) {
            return OnLogAsync(msg);
        }
    }
}