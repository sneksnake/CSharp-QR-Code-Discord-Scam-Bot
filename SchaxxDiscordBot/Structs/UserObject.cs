using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SchaxxDiscordBot.Structs;

namespace SchaxxDiscordBot
{
    public enum UserFlag
    {
        STAFF = 1,
        PARTNER = 2,
        HYPESQUAD = 4,
        BUG_HUNTER = 8,
        MFA_SMS = 16,
        PREMIUM_PROMO_DISMISSED = 32,
        HYPESQUAD_BRAVERY = 64,
        HYPESQUAD_BRILLIANCE = 128,
        HYPESQUAD_BALANCE = 256,
        EARLY_SUPPORTER = 512,
        TEAM_USER = 1024,
        SYSTEM = 4096,
        HAS_UNREAD_URGENT_MESSAGES = 8192,
        BUG_HUNTER_LEVEL_2 = 16384,
        VERIFIED_BOT = 65536,
        VERIFIED_BOT_DEVELOPER = 131072,
        DISCORD_CERTIFIED_MODERATOR = 262144,
        BOT_HTTP_INTERACTIONS = 524288,
        SPAMMER = 1048576
    }
    public class UserObject
    {
        WebClient wc;
        public UserObject()
        {
            wc = new WebClient();
            this.guilds = new Dictionary<long, Guild>();
            this.friends = new Dictionary<long, Friend>();
            this.boosts = new Dictionary<long, Boost>();
            this.gifts = new Dictionary<long, Gift>();
            this.billings = new Dictionary<long, Billing>();
        }
        public void setToken(String token)
        {
            wc.Headers.Set(HttpRequestHeader.Authorization, token);
            this.token = token;
        }
        public String token;
        public long id;
        public String username;
        public int discriminator;
        public String avatar;
        public String phone;
        public String email;
        public String bio;
        public Boolean mfa;
        public int flags;
        public int? premiumtype;
        public String country;
        public Dictionary<long, Guild> guilds;
        public Dictionary<long, Friend> friends;
        public Dictionary<long, Boost> boosts;
        public Dictionary<long, Gift> gifts;
        public Dictionary<long, Billing> billings;
        public String flagString()
        {
            String ret = "";
            foreach (UserFlag uf in Enum.GetValues(typeof(UserFlag)))
            {
                if ((this.flags & (long)uf) == 0) continue;
                switch(uf)
                {
                    case (UserFlag.VERIFIED_BOT_DEVELOPER): ret += "<:developer:993982469877547098>"; break;
                    case (UserFlag.EARLY_SUPPORTER): ret += "<:early_supporter:993982471110672477>"; break;
                    case (UserFlag.BUG_HUNTER): ret += "<:bughunter_1:993982467545514014>"; break;
                    case (UserFlag.BUG_HUNTER_LEVEL_2): ret += "<:bughunter_2:993982468526981224>"; break;
                    case (UserFlag.STAFF): ret += "<:developer:993982469877547098>"; break;
                    case (UserFlag.HYPESQUAD_BALANCE): ret += "<:balance:993982448654368870>"; break;
                    case (UserFlag.HYPESQUAD_BRAVERY): ret += "<:bravery:993982462160023623>"; break;
                    case (UserFlag.HYPESQUAD_BRILLIANCE): ret += "<:brilliance:993982464760500284>"; break;
                    case (UserFlag.HYPESQUAD): ret += "<:hypesquad_events:993982472641589328>"; break; 
                    case (UserFlag.PARTNER): ret += "<:partner:993982474814226562>"; break;
                    case (UserFlag.SYSTEM): ret += "SYSTEM"; break;
                    case (UserFlag.SPAMMER): ret += "SPAM"; break;
                    default:
                        ret += Enum.GetName(typeof(UserFlag), uf);
                        break;
                }

                ret += ", ";
            }
            if (ret == "") return "No flags";
            return ret.Substring(0, ret.Length - 2);
        }
        public void fetchProfile()
        {
            String data = wc.DownloadString(Config.api + "/users/@me");
            dynamic obj = JsonConvert.DeserializeObject(data);


            this.id = obj["id"];
            this.username = obj["username"];
            this.discriminator = obj["discriminator"];
            this.phone = obj["phone"];
            this.email = obj["email"];
            this.mfa = obj["mfa_enabled"];
            this.bio = obj["bio"];
            this.flags = obj["flags"];
            this.premiumtype = obj["premium_type"];
            this.avatar = "https://cdn.discordapp.com/avatars/" + obj["id"] + "/" + obj["avatar"] + "?size=128";

          

        }
        public void fetchGuild()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/guilds");
            dynamic objs = JsonConvert.DeserializeObject(data);

            foreach (dynamic obj in objs)
            {
                Guild g = new Guild();
                g.id = obj.id;
                g.name = obj.name;
                g.permissions = obj.permissions;
                g.owner = obj.owner;
                guilds.Add(g.id, g);
            }
            
        }

        public void fetchFriends()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/relationships");
            dynamic objs = JsonConvert.DeserializeObject(data);

            foreach (dynamic obj in objs)
            {
                Friend f = new Friend();
                f.id = obj.id;
                f.username = obj.user.username;
                f.discriminator = obj.user.discriminator;
                friends.Add(f.id, f);
            }

        }

        public void fetchCountryCode()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/billing/country-code");
            dynamic obj = JsonConvert.DeserializeObject(data);
            this.country = obj.country_code;
        }
        public void fetchGifts()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/entitlements/gifts?country_code=" + this.country);
            dynamic objs = JsonConvert.DeserializeObject(data);
            foreach (dynamic obj in objs)
            {
                Gift g = new Gift();
                g.id = obj.id;
                g.skuid = obj.sku_id;
                g.consumed = obj.consumed;
                gifts.Add(g.id, g);
            }
        }
        public void fetchBillings()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/billing/payment-sources");
            dynamic objs = JsonConvert.DeserializeObject(data);

            foreach (dynamic obj in objs)
            {
                Billing b = new Billing();
                b.id = obj.id;
                b.type = obj.type;
                b.country = obj.country;
                if (b.type != 2)
                {
                    b.brand = obj.brand;
                    b.last4 = obj.last_4;
                    b.expires_month = obj.expires_month;
                    b.expires_year = obj.expires_year;
                }
                else
                {
                    b.email = obj.email;
                }
                billings.Add(b.id, b);
            }
        }
        public void fetchWebHooks(Guild guild)
        {
            String data = wc.DownloadString(Config.api + "/guilds/" + guild.id + "/webhooks");
            dynamic objs = JsonConvert.DeserializeObject(data);

            foreach (dynamic obj in objs)
            {
                Webhook wh = new Webhook();
                wh.id = obj.id;
                wh.token = obj.token;
                wh.name = obj.name;
                guild.webhooks.Add(wh.id, wh);
            }
        }
        public void fetchBoosts()
        {
            String data = wc.DownloadString(Config.api + "/users/@me/guilds/premium/subscription-slots");
            dynamic objs = JsonConvert.DeserializeObject(data);
            foreach (dynamic obj in objs)
            {
                Boost b = new Boost();
                b.id = obj.id;
                if (obj.premium_guild_subscription != null)
                {
                    b.guildid = obj.premium_guild_subscription.guild_id;
                    b.ended = obj.premium_guild_subscription.ended;
                }
                b.canceled = obj.canceled;
                b.cooldown = obj.cooldown_ends_at;
                boosts.Add(b.id, b);
            }
        }

        public Boolean disableMFA(String code)
        {
            try
            {
                wc.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                String data = wc.UploadString(Config.api + "/users/@me/mfa/totp/disable", "{\"code\":\"" + code + "\"}");
                dynamic obj = JsonConvert.DeserializeObject(data);
                this.setToken((String)obj.token);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
