using Discord.Commands;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;

namespace DiscordBot.Modules {
    [Name("Games")]
    public class GameModule : ModuleBase<SocketCommandContext> {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;
        private readonly GamesService _gamesService;

        public GameModule(CommandService service, IConfigurationRoot config, GamesService gamesService) {
            _service = service;
            _config = config;
            _gamesService = gamesService;
        }

        [Command("valorant_status"), Alias("vs")]
        [Summary("Get the status of the Valorant servers")]
        public async Task Multiply(string region="na") {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.henrikdev.xyz/valorant/v1/status/" + region);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            await ReplyAsync($"The product of .");
        }
    }
}