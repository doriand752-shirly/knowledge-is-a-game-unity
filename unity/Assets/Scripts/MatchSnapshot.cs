using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kiag
{
    [Serializable]
    public class MatchSnapshot
    {
        public string roomCode;
        public string phase;
        public int roundIndex;
        public int totalRounds;
        public long phaseEndsAt;
        public long serverNow;
        public string joinUrl;
        public string hostLine;
        public string hostMood;
        public int playerCount;
        public string matchMode;
        public bool powerPlaysEnabled;
        public string themeOptionsJson;
        public string selectedThemeId;
        public string selectedThemeLabel;
        public string lastRevealJson;
        public string challengeJson;
        public string throwVfxJson;
        public string scrollFact;
        public string podiumJson;
        public QuestionSnap question;
        public List<PlayerSnap> players = new List<PlayerSnap>();

        public static MatchSnapshot FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MatchSnapshot>(json);
        }

        public static MatchSnapshot FromObject(object data)
        {
            var token = JToken.FromObject(data);
            return token.ToObject<MatchSnapshot>();
        }
    }

    [Serializable]
    public class QuestionSnap
    {
        public string id;
        public string prompt;
        public string choicesJson;
        public string category;
        public int points;
        public int timeLimitSec;
    }

    [Serializable]
    public class PlayerSnap
    {
        public string sessionId;
        public string name;
        public bool isHost;
        public int score;
        public int slot;
        public string color;
        public string anim;
        public int lockedAnswerIndex = -1;
        public int pyramidStep;
        public string selfieDataUrl;
        public string characterId;
        public string hazard;
        public string themeVote;
    }

    [Serializable]
    public class ChoiceSnap
    {
        public string id;
        public string label;
    }
}
