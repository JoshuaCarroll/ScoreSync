using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreSync
{
    class ScoreboardOCRData
    {
        public string GameClock 
        { 
            get
            {
                return gameClock;
            }
            set
            {
                gameClock = value.ToString();
            }
        }
        public string Period
        {
            get
            {
                return period;
            }
            set
            {
                period = checkNumericValue(value);
            }
        }
        public string Quarter
        {
            get
            {
                return period;
            }
            set
            {
                period = checkNumericValue(value);
            }
        }
        public string ShotClock
        {
            get
            {
                return shotClock;
            }
            set
            {
                if (value == null)
                {
                    shotClock = "";
                }
                else
                {
                    shotClock = value.Trim();
                }
            }
        }
        public string PlayClock
        {
            get
            {
                return shotClock;
            }
            set
            {
                if (value == null)
                {
                    shotClock = "";
                }
                else
                {
                    shotClock = value.Trim();
                }
            }
        }
        public string ScoreAway
        {
            get
            {
                return scoreAway;
            }
            set
            {
                scoreAway = checkNumericValue(value);
            }
        }
        public string ScoreHome
        {
            get
            {
                return scoreHome;
            }
            set
            {
                scoreHome = checkNumericValue(value);
            }
        }
        public string FoulsAway
        {
            get
            {
                return foulsAway;
            }
            set
            {
                foulsAway = checkNumericValue(value);
            }
        }
        public string FoulsHome
        {
            get
            {
                return foulsHome;
            }
            set
            {
                foulsHome = checkNumericValue(value);
            }
        }
        public string TimeoutsAway
        {
            get
            {
                return timeoutsAway;
            }
            set
            {
                timeoutsAway = checkNumericValue(value);
            }
        }
        public string TimeoutsHome
        {
            get
            {
                return timeoutsHome;
            }
            set
            {
                timeoutsHome = checkNumericValue(value);
            }
        }
        public string Downs
        {
            get
            {
                return down;
            }
            set
            {
                down = checkNumericValue(value);
            }
        }
        public string Yards
        {
            get
            {
                return toGo;
            }
            set
            {
                toGo = checkNumericValue(value);
            }
        }
        public string LOS
        {
            get
            {
                return ballOn;
            }
            set
            {
                ballOn = checkNumericValue(value);
            }
        }
        public string Possession
        {
            get
            {
                return possession;
            }
            set
            {
                possession = value;
            }
        }
        public string PossessionAway
        {
            get
            {
                if (Possession == "V")
                {
                    return "1";
                }
                else
                {
                    return "";
                }
            }
        }
        public string PossessionHome
        {
            get
            {
                if (Possession == "H")
                {
                    return "1";
                }
                else
                {
                    return "";
                }
            }
        }

        private string period = "0";
        private string scoreAway = "0";
        private string scoreHome = "0";
        private string foulsAway = "0";
        private string foulsHome = "0";
        private string timeoutsAway = "0";
        private string timeoutsHome = "0";
        private string shotClock = "0";
        private string down = "0";
        private string toGo = "0";
        private string ballOn = "0";
        private string possession = "";
        private string gameClock = "00:00";

        public string ToJson()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.None);
            json = "{\"type\":\"ocr\",\"values\":" + json + "}\n";
            Debug.WriteLine(json);
            return json;
        }

        private string checkNumericValue(string val)
        {
            int i;

            if ((val == null) || (val.Trim() == ""))
            {
                val = "0";
            }
            else if (!int.TryParse(val, out i))
            {
                val = "0";
            }

            return val;
        }
    }
}
