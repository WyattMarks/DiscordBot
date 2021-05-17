using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace DiscordBot.Modules {
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext> {

        private readonly ModeratorService _service;
        public ModeratorModule(ModeratorService service) {
            _service = service;
        }


        [Command("kick")]
        [Summary("Kick the specified user.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Kick([Remainder] SocketGuildUser user) {
            await ReplyAsync($"Goodbye {user.Mention} :wave:");
            await user.KickAsync();
        }

        [Command("ban")]
        [Summary("Ban the specified user.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban([Remainder] SocketGuildUser user) {
            await ReplyAsync($"Goodbye {user.Mention} :wave:");
            await user.BanAsync();
        }

        [Command("ban")]
        [Summary("Ban the specified user and deletes their messages for x days.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban(SocketGuildUser user, int purgeDays) {
            await ReplyAsync($"Goodbye {user.Mention} :wave:");
            await user.BanAsync(purgeDays);
        }

        [Command("ban")]
        [Summary("Ban the specified user for reason.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban(SocketGuildUser user, [Remainder] string text) {
            await ReplyAsync($"Goodbye {user.Mention} :wave:");
            await user.BanAsync(0, text);
        }

        [Command("ban")]
        [Summary("Ban the specified user for reason and deletes their messages for x days.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban(SocketGuildUser user, int purgeDays, [Remainder] string text) {
            await ReplyAsync($"Goodbye {user.Mention} :wave:");
            await user.BanAsync(purgeDays, text);
        }

        [Command("remove")]
        [Summary("Remove the X most recent messages in this channel")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Remove(int x) {
            var messages = await Context.Channel.GetMessagesAsync(limit: x).FlattenAsync();
            _service.removing = true;

            foreach (var msg in messages) {
                if (msg.Author != null) {
                    await msg.DeleteAsync();
                    await Task.Delay(950);
                }
                if (!_service.removing)
                    break;
            }

        }

        [Command("remove")]
        [Summary("Cancel removing")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Remove(string cancel) {
            if (cancel.Equals("cancel")) {
                if (!_service.removing)
                    await ReplyAsync("I'm not removing anything");
                _service.removing = false;
            } else {
                await ReplyAsync("Usage: ~remove X or ~remove cancel");
            }
        }
    }
}