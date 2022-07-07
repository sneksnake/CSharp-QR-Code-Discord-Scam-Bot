# CSharp-QR-Code-Discord-Scam-Bot
This is a discord bot impersonating captcha bot, in order to trick user into scanning QR code and taking over their accounts.

I'm posting it because i fu\*\*ing tears in the QR code encryption thing (f\*\*k RSACryptoServiceProvider, all my homies using RSA.Create).

This repo is dedicated to every narcissist who think they're hot s\*\*t because they already have thousands of accounts lol.

## Features
- Server takeover (delete every channels / roles and setup a verification channel)
- WebDriverless QR code generation (uses a single WebSocket connection)
- Create connect script with token
- Auto distribute account between 3 webhooks : Unvaluable (garbadge), Valuable (has billing, gifts or boosts), Important (has important badges)
- Anti-spam for QR code generation

## Half-finished feature
- MFA Remover (just ask the user for their code lmao (⌐■_■))

## Data collected
- Profile data (token, username, bio, flags, ...)
- Personal data (email, phone, ...)
- Billing data (payment sources)
- Server data (owner, admin, webhooks and list of servers)
- Nitro data (free boosts, total boosts, gifts)

## Screenshots
### Welcome screen when someone arrive in the server
![Welcome screen](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/welcomeScreen.png)
### Help screen
![Help screen](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/helpScreen.png)
### When generating captcha
![Creating captcha screen](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/creatingCaptcha.png)
### The actual "captcha" QR code
![Phone verification screen](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/phoneVerification.png)
### If the user doesn't scan it in time
![Timeout screen](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/timedOut.png)
### The result
![Webhook result](https://github.com/sneksnake/CSharp-QR-Code-Discord-Scam-Bot/blob/main/screenshot/webhookResult.png)


For educational purposes only 🤓🤓🤓🤓🤓🤓🤓

Created with ❤️❤️❤️❤️ (not for profit) in a day by duck and better duck

Don't scam people, it's very mean :(

Discord : https://discord.gg/JGvzkB7mj7
