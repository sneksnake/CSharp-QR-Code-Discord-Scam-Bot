using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Security.Cryptography;
using IronBarCode;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Drawing.Imaging;

namespace SchaxxDiscordBot
{
    public enum AuthFlow
    {
        PENDING,
        TIMEOUT,
        CANCELED,
        ERROR,
        SCANNED,
        SUCCESS
    }

    public class StepEventArg : EventArgs
    {
        public AuthFlow af;
        public MemoryStream qrcode;
        public String token;

    }


    class QRCodeAuthFlow
    {

        public class InitPacket
        {
            public String op = "init";
            public String encoded_public_key = "";
        }
        public class NonceProofPacket
        {
            public String op = "nonce_proof";
            public String proof = "";
        }

        public EventHandler Step;
        WebsocketClient ws;
        RSA rsa;
        Boolean timeoutSent = false;
        public QRCodeAuthFlow()
        {
            rsa = RSA.Create(2048);
        }

        public void onMessage(ResponseMessage rm)
        {
            //Console.WriteLine(rm.Text);
            dynamic obj = JsonConvert.DeserializeObject(rm.Text);
            try
            {
                switch ((String)obj.op)
                {
                    case ("hello"):
                        Console.WriteLine("Hello mesage received");

                        _ = Task.Run(() =>
                        {
                            Thread.Sleep(1000);
                            DateTime expirein = DateTime.Now.AddSeconds(110);
                            DateTime sendin = DateTime.Now.AddSeconds(30);
                            while (ws.IsRunning)
                            {
                                if (sendin < DateTime.Now)
                                {
                                    ws.Send("{\"op\":\"heartbeat\"}");
                                    sendin = DateTime.Now.AddSeconds(30);
                                }
                                if (expirein < DateTime.Now)
                                {
                                    if (!timeoutSent)
                                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.TIMEOUT });
                                    timeoutSent = true;
                                    stop();
                                    Console.WriteLine("QR Code has timed out in loop");
                                    break;
                                }
                                Thread.Sleep(100);
                            }
                            Console.WriteLine("Loop ended");
                        });

                        byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();


                        InitPacket ip = new InitPacket
                        {
                            encoded_public_key = Convert.ToBase64String(publicKey)
                        };
                        ws.Send(JsonConvert.SerializeObject(ip));
                        break;
                    case ("nonce_proof"):
                        byte[] msg = Convert.FromBase64String((String)obj.encrypted_nonce);


                        byte[] dec = rsa.Decrypt(msg, RSAEncryptionPadding.OaepSHA256);


                        SHA256 sha = SHA256.Create();
                        String b64 = Convert.ToBase64String(sha.ComputeHash(dec));
                        b64 = b64.Replace("/", "_");
                        b64 = b64.Replace("+", "-");
                        b64 = b64.Replace("=", "");

                        NonceProofPacket npp = new NonceProofPacket
                        {
                            proof = b64
                        };
                        ws.Send(JsonConvert.SerializeObject(npp));
                        Console.WriteLine("Nonce proof sent !");
                        break;
                    case ("pending_remote_init"):
                        Console.WriteLine("Fingerprint received");
                        String fingerprint = obj.fingerprint;
                        try
                        {
                            MemoryStream ms = new MemoryStream();
                            QRCodeWriter.CreateQrCodeWithLogo("https://discord.com/ra/" + fingerprint,"logo.png", 500)
                                .AddAnnotationTextBelowBarcode("Captcha Bot - do not share")
                                .AddAnnotationTextAboveBarcode("Telephone Captcha #" + new Random(DateTime.Now.Millisecond).Next(111111111, 999999999))
                                .Image.Save(ms, ImageFormat.Png);
                            Console.WriteLine("Link: https://discord.com/ra/" + fingerprint);
                            Step?.Invoke(this, new StepEventArg { af = AuthFlow.PENDING, qrcode = ms });
                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                        break;
                    case ("finish"):
                        Console.WriteLine("Someone accepted the QR code !");
                        String encrypted_token = obj.encrypted_token;
                        byte[] dec_token = rsa.Decrypt(Convert.FromBase64String(encrypted_token), RSAEncryptionPadding.OaepSHA256);
                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.SUCCESS, token = Encoding.UTF8.GetString(dec_token) });
                        timeoutSent = true;
                        break;
                    case ("pending_finish"):
                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.SCANNED });
                        String encrypted_user_payload = obj.encrypted_user_payload;
                        byte[] dec_user = rsa.Decrypt(Convert.FromBase64String(encrypted_user_payload), RSAEncryptionPadding.OaepSHA256);
                        Console.WriteLine(Encoding.UTF8.GetString(dec_user));
                        break;
                    case ("cancel"):
                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.CANCELED });
                        break;
                    case ("heartbeat_ack"):
                        Console.WriteLine("Heartbeat recevied");
                        break;
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Source);


            }
        }

        public void stop()
        {
            ws.Stop(System.Net.WebSockets.WebSocketCloseStatus.Empty, "tamere");
            ws.Dispose();
        }
        public async Task run()
        {
            Uri u = new Uri("wss://remote-auth-gateway.discord.gg/?v=1");
            ws = new WebsocketClient(u, () => {
                System.Net.WebSockets.ClientWebSocket cs = new System.Net.WebSockets.ClientWebSocket();
                cs.Options.SetRequestHeader("Origin", "https://discord.com");
                return cs;
            });


            ws.ReconnectTimeout = TimeSpan.FromSeconds(120);
            ws.ReconnectionHappened.Subscribe(info => {
                Console.WriteLine($"Reconnection happened, type: {info.Type}");
                if (info.Type != ReconnectionType.Initial && info.Type != ReconnectionType.ByUser)
                {
                    if (!timeoutSent)
                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.TIMEOUT });
                    timeoutSent = true;
                    stop();
                }
            });
            ws.DisconnectionHappened.Subscribe(info => {
                Console.WriteLine($"Disconnection happened, type: {info.Type}");
                if (info.Type != DisconnectionType.ByUser && info.Type != DisconnectionType.Exit)
                {
                    if (!timeoutSent)
                        Step?.Invoke(this, new StepEventArg { af = AuthFlow.TIMEOUT });
                    timeoutSent = true;
                    stop();
                    return;
                }
            });
            ws.MessageReceived.Subscribe(onMessage);



            await ws.Start();

            Console.WriteLine("Done.");

        }




    }
}
