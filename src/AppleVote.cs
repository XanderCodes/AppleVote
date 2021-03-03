using System;
using System.Net;
using System.Collections.Generic;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using System.Linq;
using System.IO;
using Rocket.Core;
using Newtonsoft.Json;

namespace AppleVote
{
    public class AppleVote : RocketPlugin<PluginConfig>
    {
        public static AppleVote Instance;
        public static PluginConfig Config;

        protected override void Load()
        {
            Logger.Log($"{Name} 1.0.0 has been loaded!", ConsoleColor.Cyan);
            Logger.Log($"Thanks for using my plugin! Contact me on Discord for support: 'AppleManYT#8750'!", ConsoleColor.Blue);

            Instance = this;

            Config = Configuration.Instance;

            if (Configuration.Instance.Rewards.Count == 0)
            {
                Logger.LogError("NO REWARDS HAVE BEEN SET UP. PLAYERS WILL NOT BE REWARDED FOR VOTING!");
            }
        }

        protected override void Unload()
        {
            Logger.Log($"{Name} has been unloaded! Sorry to see you go :(", ConsoleColor.Cyan);
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList(){
                    {"reward_command_help","Use this command to recieve your reward after voting!"},
                    {"reward_checking_servers","Checking TopServeurs.net for your vote..."},
                    {"incorrect_api_key","The API key in the plugin config is incorrect! Please let a server administrator know!"},
                    {"not_voted","You haven't voted for the server yet! Do '/vote'!"},
                    {"reward_already_claimed","You have already claimed your reward! You can vote again in {0} minutes!"},
                    {"vote_command_help","Opens the configured voting URL."},
                    {"vote_url_message","Vote for this server on TopServeurs.net!"},
                    {"rewards_not_configured","The server admin has not configured rewards yet, sorry!"},
                    {"reward_global_annoucement","Player {0} voted and recieved a reward! Do '/vote' to get a reward!"},
                    {"reward", "You've been rewarded {0}. Thanks for voting!"},
                    {"pseudo_command_help", "Tells you your public display name."},
                    {"pseudo_command_response", "Your display name is '{0}'"}
                };
            }
        }

        public static void SendMessage(IRocketPlayer caller, string message)
        {
            if (caller is ConsolePlayer)
            {
                Logger.Log(message, ConsoleColor.Cyan);
            }
            else
            {
                UnturnedPlayer unp = (UnturnedPlayer)caller;
                ChatManager.serverSendMessage(message, Color.white, null, unp.SteamPlayer(), EChatMode.SAY, Config.MessageIcon, Config.UseRichText);
            }
        }

        public class CommandTimerStart : IRocketCommand
        {
            public AllowedCaller AllowedCaller => AllowedCaller.Player;

            public string Name => "reward";

            public string Help => Instance.Translate("reward_command_help");

            public string Syntax => string.Empty;

            public List<string> Aliases => new List<string>();

            public List<string> Permissions => new List<string> { "applevote.reward" };

            public void Execute(IRocketPlayer caller, string[] command)
            {
                UnturnedPlayer unp = (UnturnedPlayer)caller;

                // "Checking server for vote..."
                SendMessage(unp, Instance.Translate("reward_checking_servers"));

                // Start up web client and send "check" GET request.
                WebClient wc = new WebClient();
                string result = null;
                bool voteSuccess = true;

                try
                {
                    result = wc.DownloadString(string.Format("https://api.top-serveurs.net/v1/votes/check?server_token={0}&playername={1}", Config.TopServeursAPIKey, unp.DisplayName));
                }
                catch (WebException e)
                {
                    // Handle response and find data
                    HttpWebResponse response = (HttpWebResponse)e.Response;
                    var content = response.GetResponseStream();
                    StreamReader reader = new StreamReader(content);
                    string text = reader.ReadToEnd();

                    // Check Request Failed
                    
                    voteSuccess = false;

                    CheckResponseError resp = JsonConvert.DeserializeObject<CheckResponseError>(text);
                    // Logger.Log($"CHECK FAIL: Code: {resp.code}, Success Bool: {resp.success}, Error: {resp.error}, Message: '{resp.message}'");

                    if (resp.error == "ServerNotFound")
                    {
                        // The API key in the plugin config is incorrect! Please let a server administrator know!
                        SendMessage(unp, Instance.Translate("incorrect_api_key"));
                    }
                    else if (resp.error == "NotFound")
                    {
                        // You have not voted for the server! Do '/vote'!
                        SendMessage(unp, Instance.Translate("not_voted"));
                    }
                }

                if (voteSuccess)
                {
                    // Check request successful, starting claim flow
                    CheckResponseSuccess checkResp = JsonConvert.DeserializeObject<CheckResponseSuccess>(result);

                    int minutes = checkResp.duration;

                    bool claimSuccess = true;

                    try
                    {
                        result = wc.DownloadString(string.Format("https://api.top-serveurs.net/v1/votes/claim-username?server_token={0}&playername={1}", Config.TopServeursAPIKey, unp.DisplayName));
                    }
                    catch (WebException e)
                    {
                        // Handle response and find data
                        HttpWebResponse response = (HttpWebResponse)e.Response;
                        var content = response.GetResponseStream();
                        StreamReader reader = new StreamReader(content);
                        string text = reader.ReadToEnd();

                        // Claim Request Failed

                        claimSuccess = false;

                        ClaimResponseError resp = JsonConvert.DeserializeObject<ClaimResponseError>(text);
                        // Logger.Log($"CLAIM FAIL: Code: {resp.code}, Success Bool: {resp.success}, Message: '{resp.message}'");

                        // The API key in the plugin config is incorrect! Please let a server administrator know!
                        SendMessage(unp, Instance.Translate("incorrect_api_key"));
                    }

                    if (claimSuccess)
                    {
                        // Successful Claim Request

                        ClaimResponseSuccess resp = JsonConvert.DeserializeObject<ClaimResponseSuccess>(result);
                        // Logger.Log($"CLAIM SUCCESS: Code: {resp.code}, Success Bool: {resp.success}, Claim Int: {resp.claimed}, Message: '{resp.message}'");

                        if (resp.claimed == 1)
                        {
                            GiveReward(unp, "service name");
                        }
                        else if (resp.claimed == 2)
                        {
                            // You have already claimed your reward! You can vote again in # minutes!
                            SendMessage(unp, string.Format(Instance.Translate("reward_already_claimed"), minutes));
                        }
                    }
                }
            }
        }

        public class CommandVote : IRocketCommand
        {
            public AllowedCaller AllowedCaller => AllowedCaller.Player;

            public string Name => "vote";

            public string Help => Instance.Translate("vote_command_help");

            public string Syntax => "";

            public List<string> Aliases => new List<string>();

            public List<string> Permissions => new List<string>() { "applevote.vote" };

            public void Execute(IRocketPlayer caller, string[] command)
            {
                UnturnedPlayer player = (UnturnedPlayer)caller;
                string url = Config.VotePageURL;

                player.Player.sendBrowserRequest(Instance.Translate("vote_url_message", Provider.serverName), url);
            }
        }

        public class CommandPseudo : IRocketCommand
        {
            public AllowedCaller AllowedCaller => AllowedCaller.Player;

            public string Name => "pseudo";

            public string Help => Instance.Translate("pseudo_command_help");

            public string Syntax => "";

            public List<string> Aliases => new List<string>();

            public List<string> Permissions => new List<string>() { "pseudo" };

            public void Execute(IRocketPlayer caller, string[] command)
            {
                UnturnedPlayer unp = (UnturnedPlayer)caller;
                SendMessage(unp, string.Format(Instance.Translate("pseudo_command_response"), unp.DisplayName));
            }
        }

        public static void GiveReward(UnturnedPlayer player, string serviceName)
        {
            int sum = Config.Rewards.Sum(p => p.Chance);
            string selectedElement = null;
            string value = null;

            System.Random r = new System.Random();

            int i = 0, diceRoll = r.Next(0, sum);

            foreach (var reward in Config.Rewards)
            {
                if (diceRoll > i && diceRoll <= i + reward.Chance)
                {
                    selectedElement = reward.Type;
                    value = reward.Value;
                    break;
                }
                i = i + reward.Chance;
            }

            if (selectedElement == null || value == null)
            {
                SendMessage(player, Instance.Translate(string.Format("rewards_not_configured")));
                return;
            }

            // Rewards
            if (selectedElement == "item" || selectedElement == "i")
            {
                List<string> items = value.Split(',').ToList();
                foreach (string item in items)
                {
                    ushort itemID = ushort.Parse(item);

                    player.Inventory.tryAddItem(new Item(itemID, true), true);
                }

                SendMessage(player, Instance.Translate("reward", "some items"));
            }
            else if (selectedElement == "xp" || selectedElement == "exp")
            {
                player.Experience += uint.Parse(value);

                SendMessage(player, Instance.Translate("reward", value + " xp"));
            }
            else if (selectedElement == "group" || selectedElement == "permission")
            {
                R.Permissions.AddPlayerToGroup(value, player);
                R.Permissions.Reload();

                SendMessage(player, Instance.Translate("reward", value + " Permission Group"));
            }

            // Optional global announcement
            if (Config.GlobalAnnouncement)
            {
                foreach (SteamPlayer sP in Provider.clients)
                {
                    var p = sP.playerID.steamID;
                    if (p != player.CSteamID)
                    {
                        SendMessage(player, Instance.Translate("reward_global_annoucement", player.DisplayName));
                    }
                }
            }
        }
    }

    public class CheckResponseSuccess
    {
        public int code { get; set; }
        public bool success { get; set; }
        public int duration { get; set; }
        public string message { get; set; }
    }

    public class CheckResponseError
    {
        public int code { get; set; }
        public bool success { get; set; }
        public string error { get; set; }
        public string message { get; set; }
    }

    public class ClaimResponseSuccess
    {
        public int code { get; set; }
        public bool success { get; set; }
        public int claimed { get; set; }
        public string message { get; set; }
    }

    public class ClaimResponseError
    {
        public int code { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
    }
}