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
using GoogleMaps.LocationServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime;
using System.Runtime.InteropServices;
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
	#region PluginInfo
    [PluginInfo(Name = "EvvvvilTweetLogger", Category = "Network", Help = "Part 1 of the ultimate Twitter API vvvv plugin, bitch.", Tags = "twitter, network", Author = "evvvvil")]
	#endregion PluginInfo
    public class EvvvvilTweetLogger : IPluginEvaluate
	{
        
        
		#region fields & pins

        [Input("Application Credentials")]
        ISpread<string> FCredentials;

        [Input("Developer Credentials")]
        ISpread<string> FDevCredentials;

        [Input("Login as a Dev", IsBang = true, IsSingle = true)]
        ISpread<bool> FLogin;

        [Input("Authorize a Twitter User", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthorizeUser;

        [Input("Verifier Code", IsSingle = true)]
        ISpread<string> FVerifierCode;

        [Input("Login as a User", IsBang = true, IsSingle = true)]
        ISpread<bool> FLoginUser;

        [Input("Get Coordinates", IsBang = true, IsSingle = true)]
        ISpread<bool> FGetCoordinates;

        [Input("Address To Get Coordinates")]
        ISpread<string> FAddressForCoordinates; 

        [Input("Refresh Login Status", IsBang = true, IsSingle = true)]
        ISpread<bool> FLoginRefresh;

        [Input("Logout", IsBang = true, IsSingle = true)]
        ISpread<bool> FLogout;

        [Output("Authorization URL", IsSingle = true)]
        public ISpread<string> FOutputURL;

        [Output("Logger Status", IsSingle = true)]
        public ISpread<string> FOutputStatus;

        [Output("Logged In?", IsSingle = true)]
        public ISpread<bool> FOutputStatusBool;

        [Output("Geolocation Coordinates(Lat/Long)")]
        public ISpread<double> FOutputCoordinates;

        [Output("Search Limit", IsSingle = true)]
        public ISpread<int> FOutputSearchesRemaining;

        [Output("Next Search Limit Reset", IsSingle = true)]
        public ISpread<string> FOutputSearchesReset;        

        [Import()]
        public ILogger FLogger;
        
        #endregion fields & pins

        public Task loginTask;
        public Task loginRefreshTask;
        public Task getGeoLocTask;
        public Task logoutTask;
        public Task getCredentialsTask;
        public Task getVerifyUserLoginTask;
        public Tweetinvi.Core.Interfaces.Credentials.ITemporaryCredentials applicationCredentials;
        public IOAuthCredentials newCredentials;
        public string url;
        public string userAccessToken;
        public string userAccessSecret;
        public bool loggedIn = false;

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }

        public void TryToGetCredentials()
        {
            GetVerifierURL(FCredentials[0], FCredentials[1]);            
        }
        public void TryUserLogin()
        {            
            GetCredentialsAndLogin(FVerifierCode[0]);            
        }
        public void LoginAsDev()
        {
            if (!loggedIn)
                {
                    if (!IsConnectedToInternet())
                    {
                        FOutputStatus[0] = "Internet is fucked bro. Can't login as dev...";
                        FLogger.Log(LogType.Debug, "Internet is fucked bro. Can't login as dev...");
                        FOutputStatusBool[0] = false; loggedIn=false;
                        ClearOutputPins();
                    }
                    else
                    {
                            try
                            {
                                TwitterCredentials.SetCredentials(FDevCredentials[0], FDevCredentials[1], FCredentials[0], FCredentials[1]);
                                //TwitterCredentials.SetCredentials("Access_Token", "Access_Token_Secret", "Consumer_Key", "Consumer_Secret");
                                GetRateLimits("Login failed! Bad credentials?");
                            }
                            catch
                            {
                                FOutputStatus[0] = "Bad application or dev credentials, monsieur tete de bite.";
                                FLogger.Log(LogType.Debug, "Bad application or dev credentials, monsieur tete de bite.");
                            }                
                    }
                }
            else
            {
                FOutputStatus[0] = "You're already logged in, you douche, logout first.";
                FLogger.Log(LogType.Debug, "You'RE already logged in, you douche, logout first.");
            }
        }
        public void LoginRefreshTry()
        {
            if (!IsConnectedToInternet())
            {

                FOutputStatus[0] = "Internet is fucked bro.";
                FLogger.Log(LogType.Debug, "Internet is fucked bro.");
                FOutputStatusBool[0] = false; loggedIn=false;
                ClearOutputPins();
            }
            else
            {
                GetRateLimits("You are not logged in anymore, login again.");
            }
        }
        public void GetGeolocFromAddress()
        {
            if (!IsConnectedToInternet())
            {
                FOutputStatus[0] = "Can't get geoloc as Internet is fucked bro.";
                FLogger.Log(LogType.Debug, "Can't get geoloc as Internet is fucked bro.");
                FOutputStatusBool[0] = false; loggedIn=false;
                ClearOutputPins();
            }
            else
            {
                try
                {
                    var locationService = new GoogleLocationService();
                    var point = locationService.GetLatLongFromAddress(FAddressForCoordinates[0]);
                    FOutputCoordinates.SliceCount = 2;
                    FOutputCoordinates[0] = point.Latitude;
                    FOutputCoordinates[1] = point.Longitude;
                    FOutputStatus[0] = "DONE! Got geolocation from address, fucking riiiiiiiight!";
                    FLogger.Log(LogType.Debug, "DONE! Got geolocation from address, fucking riiiiiiiight!");
                }
                catch
                {
                    FOutputStatus[0] = "Could not get geocoordinates from address.";
                    FLogger.Log(LogType.Debug, "Could not get geocoordinates from address.");
                }                
            }
        }        
        public void ClearOutputPins()
        {
            FOutputSearchesRemaining[0] = 0;
            FOutputSearchesReset[0] = "";
        }
        public void GetRateLimits(string mess)
        {
            var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();
            if (rateLimits != null)
            {
                try
                {
                    var currentUser = User.GetLoggedUser().Name;
                   FOutputStatus[0] = "Fuck YEAH! Logged in as " + currentUser;
                    FLogger.Log(LogType.Debug, "Fuck YEAH! Logged in as " + currentUser);
                    FOutputSearchesRemaining[0] = rateLimits.SearchTweetsLimit.Limit;
                    FOutputSearchesReset[0] = rateLimits.SearchTweetsLimit.ResetDateTime.ToString();
                    FOutputStatusBool[0] = true;
                    loggedIn=true;
                }
                catch
                {
                    FOutputStatus[0] = mess;
                    FLogger.Log(LogType.Debug, mess);
                    ClearOutputPins();
                    FOutputStatusBool[0] = false; 
                    loggedIn = false;

                }
            }
            else
            {
                FOutputStatus[0] = mess;
                FLogger.Log(LogType.Debug, mess);
                ClearOutputPins();
                FOutputStatusBool[0] = false; 
                loggedIn=false;
            }
        }
        public void Logout()
        {
                IOAuthCredentials emptyCred = TwitterCredentials.CreateCredentials("", "", "", "");
                TwitterCredentials.Credentials = emptyCred;
                FOutputStatus[0] = "Logged out!";
                FLogger.Log(LogType.Debug, "Logged out!");
                ClearOutputPins();
                FOutputStatusBool[0] = false; loggedIn=false;  
        }
        public void GetVerifierURL(string consumerKey, string consumerSecret)
        {
            if (!loggedIn)
                {
                    if (!IsConnectedToInternet())
                    {
                        FOutputStatus[0] = "Can't authorize as your internet is fucked bro.";
                        FLogger.Log(LogType.Debug, "Can't authorize as your internet is fucked bro.");
                        ClearOutputPins();
                        FOutputStatusBool[0] = false; loggedIn=false;
                    }
                    else
                    {
                
                            try
                            {
                                applicationCredentials = CredentialsCreator.GenerateApplicationCredentials(consumerKey, consumerSecret);
                                url = CredentialsCreator.GetAuthorizationURLForCallback(applicationCredentials, "");
                                FOutputURL[0] = url;
                                FOutputStatus[0] = "Authorization URL passed, nice one.";
                                FLogger.Log(LogType.Debug, "Authorization URL passed, nice one.");
                            }
                            catch
                            {
                                FOutputStatus[0] = "Bad consumer key or secret, monsieur tete de bite.";
                                FLogger.Log(LogType.Debug, "Bad consumer key or secret, monsieur tete de bite.");
                            }
                    }
                }
            else
            {
                FOutputStatus[0] = "You're already logged in, you douche, logout first.";
                FLogger.Log(LogType.Debug, "You'RE already logged in, you douche, logout first.");
            }
        }

        public void GetCredentialsAndLogin(string verifierCode)
        {
            
            if (!IsConnectedToInternet())
            {
                FOutputStatus[0] = "Can't login as your internet is fucked bro.";
                FLogger.Log(LogType.Debug, "Can't login as your internet is fucked bro.");
                ClearOutputPins();
                FOutputStatusBool[0] = false; loggedIn=false;
            }
            else
            {
                try
                {
                    newCredentials = CredentialsCreator.GetCredentialsFromVerifierCode(verifierCode, applicationCredentials);
                    TwitterCredentials.Credentials = newCredentials;
                    GetRateLimits("Login failed! Expired or bad verifier code?");
                }
                catch
                {
                    FOutputStatus[0] = "Expired or bad verifier code, sac a vin";
                    FLogger.Log(LogType.Debug, "Expired or bad verifier code, sac a vin.");
                }
            }
        }

        #region Evaluate Function
        public void Evaluate(int SpreadMax)
		{
            //FLastSearchStatus[0]=lastSearchStatus;
            if (FLogin[0])
            {
                loginTask = new Task(LoginAsDev);
                loginTask.Start();
            }
            if (FLogout[0])
            {
                logoutTask = new Task(Logout);
                logoutTask.Start();
            }
            if (FLoginRefresh[0])
            {
                loginRefreshTask = new Task(LoginRefreshTry);
                loginRefreshTask.Start();
            }
            if(FGetCoordinates[0])
            {
                getGeoLocTask = new Task(GetGeolocFromAddress);
                getGeoLocTask.Start();
            }

            if (FAuthorizeUser[0])
            {
                
                getCredentialsTask = new Task(TryToGetCredentials);
               getCredentialsTask.Start();
            }

            if (FLoginUser[0])
            {
                getVerifyUserLoginTask = new Task(TryUserLogin);
                getVerifyUserLoginTask.Start();
            }
        }
        #endregion Evaluate Function


    }
   
    }
