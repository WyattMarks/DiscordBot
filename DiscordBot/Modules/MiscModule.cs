using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using Google.Cloud.Translation.V2;
using Genbox.Wikipedia;
using Genbox.Wikipedia.Objects;
using System.Text.RegularExpressions;

namespace DiscordBot.Modules {
    [Name("Miscellaneous")]
    public class MiscModule : ModuleBase<SocketCommandContext> {
        private readonly TimerService _service;
        public MiscModule(TimerService service) {
            _service = service;
        }

        [Command("timer")]
        [Summary("Start or stop a timer")]
        public async Task TimerCmd(params string[] input) {
            if (input[0] == "start") {

                TimeSpan repeat = TimeSpan.Zero;
                if (input.Length >= 3 && !string.IsNullOrEmpty(input[2])) {
                    repeat = TimeSpan.Parse(input[2]);
                }

                TimeSpan due = new TimeSpan(0, 5, 0);
                if (!string.IsNullOrEmpty(input[1])) {
                    due = TimeSpan.Parse(input[1]);
                }

                await _service.Start(Context.User.Mention, Context.Channel, due, repeat);
            } else if (input[0] == "stop") {
                await _service.Stop(Context.User.Mention, Context.Channel);
            } else {
                await ReplyAsync("Usage: !timer start/stop h:mm:ss");
            }
        }


        [Command("say")]
        [Summary("Make the bot say something")]
        public Task Say([Remainder] string text) {
            return ReplyAsync(text);
        }


        [Command("ping")]
        [Summary("Pong")]
        public Task Ping()
            => ReplyAsync("Pong " + Context.User.Mention);

        [Command("info")]
        [Summary("Get information on a user")]
        public async Task Info([Remainder] SocketGuildUser user) {
            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218)
            };

            string description = "";
            description += $"Discord Name: {user.ToString()}\n";
            description += $"Created: {user.CreatedAt.LocalDateTime} CDT\n";
            if (user.JoinedAt.HasValue)
                description += $"Joined {Context.Guild.Name}: {user.JoinedAt.Value.LocalDateTime} CDT\n";
            if (user.PremiumSince.HasValue)
                description += $"Nitro Since: {user.PremiumSince.Value.LocalDateTime} CDT\n";
            else
                description += $"No Nitro\n";
            if (user.Activity != null)
                description += $"Status: {user.Activity.Name}\n";
            builder.ImageUrl = user.GetAvatarUrl();

            builder.AddField(x => {
                x.Name = user.Nickname ?? user.Username;
                x.Value = description;
                x.IsInline = false;
            });
            await ReplyAsync("", false, builder.Build());
        }

        [Command("translate"), Alias("t", "gt")]
        [Summary("Translate detected language to English")]
        public async Task Translate(params string[] input) {
            

            TranslationClient client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromFile("service-account-file.json"));

            bool translateTo = false;

            foreach (var l in client.ListLanguages(LanguageCodes.English)) {
                if (l.Code == input[0] || l.Name.ToLower() == input[0].ToLower()) {
                    translateTo = true;
                    input[0] = l.Code;
                }
            }

            TranslationResult result;

            if (translateTo) {
                var language = input[0];
                input[0] = "";
                result = client.TranslateText(string.Join(" ", input), language);
            } else {
                result = client.TranslateText(string.Join(" ", input), LanguageCodes.English);
            }

            await ReplyAsync(result.TranslatedText);
        }

        [Command("translate"), Alias("t", "gt")]
        [Summary("Translate text into a language")] //Just to make the help section of this command easier. Technically the command def above handles both scenarios
        public async Task Translate(string language, [Remainder] params string[] input) {
            string[] text = new string[input.Length + 1];
            text[0] = language;
            for (int i = 0; i < input.Length; i++) {
                text[i+1] = input[i];
            }
            await Translate(text);
        }


        [Command("setnick"), Priority(1)]
        [Summary("Change your nickname to the specified text")]
        [RequireUserPermission(GuildPermission.ChangeNickname)]
        public Task Nick([Remainder] string name)
            => Nick(Context.User as SocketGuildUser, name);

        [Command("setnick"), Priority(0)]
        [Summary("Change another user's nickname to the specified text")]
        [RequireUserPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(SocketGuildUser user, [Remainder] string name) {
         
            await user.ModifyAsync(person => person.Nickname = name);
            await ReplyAsync($"{user.Mention} I changed your name to **{name}**");
        }

        [Command("wikipedia"), Alias("wiki")]
        [Summary("Get a wikipedia summary")]
        public async Task Wiki([Remainder] string text) {
            WikipediaClient client = new WikipediaClient();
            client.Limit = 1;

            QueryResult results = client.Search(text);

            var builder = new EmbedBuilder() {
                Color = new Color(114, 137, 218),
            };


            foreach (Search s in results.Search) {

                PageResult pages = client.GetPage(s.Title);

                string value = "\n";
                string name = s.Title;

                foreach (Page page in pages.Pages) {
                    value = page.Extract;
                }

                if (value.Length > 1017 - s.Url.AbsoluteUri.Length) {
                    value = value.Remove(1017 - s.Url.AbsoluteUri.Length);
                    value += "...\n\n";
                } else {
                    value += "\n\n";
                }
                
                value += s.Url.AbsoluteUri;

                builder.AddField(x => {
                    x.Name = name;
                    x.IsInline = false;
                    x.Value = value;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}