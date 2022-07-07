using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchaxxDiscordBot.Structs
{
    public class Billing
    {
        public long id;
        public int type;
        public String country;

        public String email;

        public String brand;
        public int last4;
        public int expires_month;
        public int expires_year;
        
        public String toString()
        {
            if (this.type == 2)
                return "<:paypal:994015239228100628> " + this.country + " | " + this.email;
            else
                return "💳 " + this.country + " " + this.brand + " | `************" + this.last4 + "` (" + this.expires_month + "/" + this.expires_year + ")";
        }
    }
}
