using Discord.Commands;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using Discord;

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
        public async Task ValorantStatus(string region = "na") {
            ValorantStatusResponse response = await _gamesService.GetValorantStatus(region);
            if (response.data["maintenances"].Length == 0 && response.data["incidents"].Length == 0) {
                int status;
                int.TryParse(response.status, out status);
                if (status == 200) {
                    await ReplyAsync($"The {region.ToUpper()} region servers are experiencing no issues.");
                }
            } else {
                string reply = response.data["maintenances"].Length == 0 ? "" : "Maintenace:\n";
                foreach (string maintenance in response.data["maintenances"]) {
                    reply += $"    {maintenance}\n";
                }

                string reply2 = response.data["incidents"].Length == 0 ? "" : "Incidents:\n";
                foreach (string incident in response.data["incidents"]) {
                    reply2 += $"    {incident}\n";
                }

                await ReplyAsync($"{reply} \n{reply2}");
            }
        }

        [Command("valorant_profile")]
        [Summary("Get the information of a Valorant profile")]
        public async Task ValorantProfile(string profile, string tagline) {
            ValorantProfileResponse response = await _gamesService.GetValorantProfile(profile, tagline);

            int status;
            int.TryParse(response.status, out status);
            if (status == 200) {
                var builder = new EmbedBuilder() {
                    Color = new Color(114, 137, 218),
                };
                string list = "`";

                foreach (string key in response.data.Keys) {
                    if (key != "name" && key != "tag")
                        list += $"{key.ToUpper()}: {response.data[key]}\n";
                }

                builder.AddField(x => {
                    x.Name = $"{profile}#{tagline} Profile Information";
                    x.Value = list + "`";
                    x.IsInline = false;
                });
                await ReplyAsync("", false, builder.Build());
            } else if (status == 404) {
                await ReplyAsync($"Profile \"{profile}#{tagline}\" not found.");
            } else {
                await ReplyAsync($"Error: {status}: {response.message}");
            }
        }

        [Command("valorant_mmr"), Alias("mmr")]
        [Summary("Get the rank information of a Valorant profile")]
        public async Task ValorantMMR(string profile, string tagline, string region="na") {
            ValorantMMRResponse response = await _gamesService.GetValorantMMR(profile, tagline, region);

            int status;
            int.TryParse(response.status, out status);
            if (status == 200) {
                var builder = new EmbedBuilder() {
                    Color = new Color(114, 137, 218),
                };
                string list = "`";

                if (int.Parse(response.data["mmr_change_to_last_game"]) > 0) {
                    response.data["mmr_change_to_last_game"] = "+" + response.data["mmr_change_to_last_game"];
                }

                list += $"Rank: {response.data["currenttierpatched"]}     {response.data["ranking_in_tier"]}/100\n";
                list += $"Last Game Change: {response.data["mmr_change_to_last_game"]}\n";
                list += $"ELO: {response.data["elo"]}\n";

                builder.AddField(x => {
                    x.Name = $"{profile}#{tagline} Rank Information";
                    x.Value = list + "`";
                    x.IsInline = false;
                });
                await ReplyAsync("", false, builder.Build());
            } else if (status == 404) {
                await ReplyAsync($"Profile \"{profile}#{tagline}\" not found.");
            } else {
                await ReplyAsync($"Error: {status}: {response.message}");
            }
        }
    }
}