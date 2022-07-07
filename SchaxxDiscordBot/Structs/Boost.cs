using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchaxxDiscordBot.Structs
{
    public class Boost
    {
        public long id;
        public long guildid;
        public Boolean ended;
        public Boolean canceled;
        public String cooldown;
    }
}
