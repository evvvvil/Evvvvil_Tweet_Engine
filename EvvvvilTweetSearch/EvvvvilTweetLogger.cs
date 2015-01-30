#region usings
using System;
using System.Net;
using System.Configuration;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Core.Enum;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Core.Interfaces;
using Tweetinvi.Core.Interfaces.Controllers;
using Tweetinvi.Core.Interfaces.DTO;
using Tweetinvi.Core.Interfaces.Models;
using Tweetinvi.Core.Interfaces.Models.Parameters;
using Tweetinvi.Core.Interfaces.oAuth;
using Tweetinvi.Core.Interfaces.Streaminvi;
using Tweetinvi.Json;

#endregion usings

namespace VVVV.Nodes.Network
{
    public enum TweetPictureSize{
        small,
        medium,
        large,
    }
    public enum TweetResultType
    {
        Mixed,
        Recent,
        Popular,
    }
	#region PluginInfo
	[PluginInfo(Name = "EvvvvilTweetLogger", Category = "Network", Help = "Part 1 of The ultimate Twitter API vvvv plug in, bitch.", Tags = "twitter, network, tweetinvi, twitter api, twitter rest 1.1, rest 1.1")]
	#endregion PluginInfo
    public class EvvvvilTweetLogger : IPluginEvaluate
	{
        public bool loggedIn;
        public int limitSearchRemaining;
        public string limitResetTime;

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
		#region fields & pins

        [Input("Credentials")]
        ISpread<string> FCredentials;

        [Input("Login", IsBang = true, IsSingle = true)]
        ISpread<bool> FLogin;        

        [Output("Logged In?", IsSingle = true)]
        public ISpread<string> FOutputStatus;        

        [Import()]
        public ILogger FLogger;
        
        #endregion fields & pins

        #region Evaluate Function
        public void Evaluate(int SpreadMax)
		{
            //FLastSearchStatus[0]=lastSearchStatus;
            if (FLogin[0])
            {
                 /*try
                {*/

                    if (!CheckForInternetConnection())
                    {

                        FOutputStatus[0] = "Internet is fucked bro.";
                    }
                    else {
                    //TwitterCredentials.SetCredentials(accToken, accSecret, consKey, consSecret);
                    TwitterCredentials.SetCredentials(FCredentials[0], FCredentials[1], FCredentials[2], FCredentials[3]);
                    var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();
                    if (rateLimits != null)
                    {
                        FOutputStatus[0] = "Fuck YEAH!/n Search Limit: " + rateLimits.SearchTweetsLimit.Limit.ToString() + " Search Remaining: " + rateLimits.SearchTweetsLimit.Remaining.ToString() +
                                            "/n FavoritesListLimit: " + rateLimits.FavoritesListLimit.Limit.ToString() + " Search Remaining: " + rateLimits.FavoritesListLimit.Remaining.ToString();
                        limitSearchRemaining = rateLimits.SearchTweetsLimit.Limit;
                        limitResetTime = rateLimits.SearchTweetsLimit.ResetDateTime.ToString();
                        loggedIn = true;
                    }
                    else
                    {
                        FOutputStatus[0] = "Nope! Bad credentials.";
                        loggedIn = false;
                    }
                    }
               // }
                /*catch (System.ArgumentNullException e)
                {
                    lastSearchStatus = "Internet connection is fucked bro";
                    throw new System.ArgumentNullException("Internet connection is fucked bro", e);
                }*/
            }
        }
        #endregion Evaluate Function


    }
   
    }
