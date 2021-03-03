using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AppleVote
{
    public partial class PluginConfig : IRocketPluginConfiguration
    {
        public static PluginConfig Instance;

        public bool UseRichText;
        public string MessageIcon;

        public List<Reward> Rewards;
        public bool GlobalAnnouncement;
        public string VotePageURL;
        public string TopServeursAPIKey;

        public class Reward
        {
            public Reward() { }

            internal Reward(string type, string value, short chance)
            {
                Type = type;
                Value = value;
                Chance = chance;
            }

            [XmlAttribute]
            public string Type;
            [XmlAttribute]
            public string Value;
            [XmlAttribute]
            public short Chance;
        }

        public void LoadDefaults()
        {
            Instance = this;

            Rewards = new List<Reward>()
            {
                new Reward("item", "235,236,237,238,253,1369,1371,1371,297,298,298,298,15,15,15,15,15", 40),
                new Reward("xp", "1400", 50),
                new Reward("group", "VIP", 10)
            };

            GlobalAnnouncement = true;
            VotePageURL = "https://top-serveurs.net/unturned";
            TopServeursAPIKey = "enter your API key here";

            UseRichText = true;
            MessageIcon = "https://i.imgur.com/uuOb7CS.png";
        }
    }
}
