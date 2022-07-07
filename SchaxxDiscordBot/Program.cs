using System;
using System.Threading;

namespace SchaxxDiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting program !");

            SQLInstance.open();

            DiscordBot db = new DiscordBot();
            db.Start();

            Thread.Sleep(-1);


            
        }
    }
}
