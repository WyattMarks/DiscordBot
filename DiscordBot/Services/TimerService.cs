using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections.Generic;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace DiscordBot {
    public class TimerService {
        private Dictionary<string, Timer> timers;
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public TimerService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config) {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
            timers = new Dictionary<string, Timer>();
        }


        public async Task Stop(string key, ISocketMessageChannel channel) {
            Timer t;

            if (timers.TryGetValue(key, out t)) {
                t.Change(Timeout.Infinite, Timeout.Infinite);
                t.Dispose();
                timers.Remove(key);
                await channel.SendMessageAsync($"{key}  Timer stopped!");
                return;
            } else {
                await channel.SendMessageAsync($"{key}  You don't have a timer!");
            }
        }

        public async Task Start(string key, ISocketMessageChannel channel, TimeSpan due, TimeSpan repeat) {
            Timer t = new Timer(async _ => {
                await channel.SendMessageAsync($"{key}  Timer up!");
            },
            null,
            due,
            repeat);
            if (timers.ContainsKey(key))
                await Stop(key, channel);
            
            if (repeat != TimeSpan.Zero)
                await channel.SendMessageAsync($"{key}  Timer set for {due.ToString("g")}, and then every {repeat.ToString("g")}!");
            else
                await channel.SendMessageAsync($"{key}  Timer set for {due.ToString("g")}!");
            timers.Add(key, t);
        }

    }
}
