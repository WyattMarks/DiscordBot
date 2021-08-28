using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections.Generic;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;

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


        public async Task<ValorantStatusResponse> GetValorantStatus(string region) {
            HttpClient client = new HttpClient();
            string responseBody = await client.GetStringAsync("https://api.henrikdev.xyz/valorant/v1/status/" + region);
            return JsonConvert.DeserializeObject<ValorantStatusResponse>(responseBody);
        }

        public async Task<ValorantProfileResponse> GetValorantProfile(string name, string tagline) {
            HttpClient client = new HttpClient();
            string responseBody = await client.GetStringAsync($"https://api.henrikdev.xyz/valorant/v1/account/{name}/{tagline}");
            return JsonConvert.DeserializeObject<ValorantProfileResponse>(responseBody);
        }

        public async Task<ValorantMMRResponse> GetValorantMMR(string name, string tagline, string region) {
            HttpClient client = new HttpClient();
            string responseBody = await client.GetStringAsync($"https://api.henrikdev.xyz/valorant/v1/mmr/{region}/{name}/{tagline}");
            return JsonConvert.DeserializeObject<ValorantMMRResponse>(responseBody);
        }

    }
}
