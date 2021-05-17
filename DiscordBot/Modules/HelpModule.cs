using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules {

    [Name("Help")]
    public class HelpModule : ModuleBase<SocketCommandContext> {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;

        public HelpModule(CommandService service, IConfigurationRoot config) {
            _service = service;
            _config = config;
        }

        [Command("help"), Alias("h")]
        [Summary("Get the list of commands")]
        public async Task Help() {
            string prefix = _config["prefix"];
            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218),
                Description = "Here are my available commands"
            };

            List<string> listed = new List<string>();
            for (int i = 0; i < _service.Modules.Count(); i++) {
                var module = _service.Modules.ElementAt(i);

                string description = "";
                bool alternate = false;
                string withArgs = "";


                foreach (var cmd in module.Commands) {
                    
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess && !listed.Contains(cmd.Aliases.First())) {
                        listed.Add(cmd.Aliases.First());
                        if (cmd.Parameters.Count > 0) {
                            string args = "";
                            foreach (var parameter in cmd.Parameters) {
                                args += $"<{parameter.Name}> ";
                            }
                            withArgs += $"`{prefix}{cmd.Aliases.First()}  {args}`\n";
                        } else {
                            description += $" `{prefix}{cmd.Aliases.First(), -9}`";

                            if (alternate) {
                                description += "\n";
                            }

                            alternate = !alternate;
                        }
                    }
                }

                if (alternate)
                    description += "\n";
                description += withArgs;


                if (i == _service.Modules.Count() - 1)
                    description += $"\n\n Use `{prefix}help <command>` for more information\n";

                if (!string.IsNullOrWhiteSpace(description)) {
                    builder.AddField(x => {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help"), Alias("h")]
        [Summary("Get specific information on a command")]
        public async Task Help(string command) {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess) {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = _config["prefix"];
            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            if (result.Commands.Count == 1) {
                builder.Description = "";
            }

            foreach (var match in result.Commands) {
                var cmd = match.Command;

                builder.AddField(x => {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: `{string.Join(", ", cmd.Parameters.Select(p => p.Name))}`\n" +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}