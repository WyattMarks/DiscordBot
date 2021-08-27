﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections.Generic;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace DiscordBot {
    public class GamesService {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public GamesService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config) {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
        }


        public async Task GetJSONResponse(string uri) {

        }

    }
}
