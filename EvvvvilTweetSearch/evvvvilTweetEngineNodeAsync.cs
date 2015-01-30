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

namespace VVVV.Nodes
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
	[PluginInfo(Name = "EvvvvilTweetAsync", Category = "Network", Help = "The ultimate Twitter API vvvv plug in, bitch.", Tags = "twitter, network, tweetinvi, twitter api, twitter rest 1.1, rest 1.1")]
	#endregion PluginInfo
    public class evvvvilTweetEngineNode : IPluginEvaluate
	{

        public System.Timers.Timer timo = new System.Timers.Timer(5000);
        public int totalNumOfTweets;
        public string accToken;
        public string accSecret;
        public string consKey;
        public string consSecret;
        public string lastSearchStatus;
        public int numOfTweets = 0;
        public bool foundNewTweets;
        public bool searching;
        public bool loggedIn;
        public int limitSearchRemaining;
        public string limitResetTime;
        public List<string> listText = new List<string>();
        public List<string> listTweetCoordinates = new List<string>();
        public List<string> listTweetCreatedAt = new List<string>();
        public List<string> listTweetAttachedPicUrl = new List<string>();
        public List<string> listTweetAttachedUrl = new List<string>();                
        public List<int> listTweetAttachedPicWidth = new List<int>();
        public List<int> listTweetAttachedPicHeight = new List<int>();
        public List<int> listTweetFavoriteCount = new List<int>();
        public List<bool> listTweetFavorited = new List<bool>();
        public List<long> listTweetCountry = new List<long>();
        public List<string> listTweetIdStr = new List<string>();
        public List<string> listTweetInReplyScreenName = new List<string>();
        public List<string> listTweetInReplyStatusIdStr = new List<string>();
        public List<string> listTweetinReplyToUserIdStr = new List<string>();
        public List<string> listTweetLang = new List<string>();
        public List<string> listTweetPlace = new List<string>();            
        public List<bool> listTweetPossiblySensitive = new List<bool>();
        public List<int> listTweetRetweetCount = new List<int>();
        public List<bool> listTweetRetweeted = new List<bool>();
        public List<string> listTweetSource = new List<string>();
        public List<bool> listTweetWithheldCopyright = new List<bool>();
        public List<string> listTweetWithheld = new List<string>();
        public List<string> listTweetWithheldScope = new List<string>();

        /*  
         * List of all TWITTER API Tweeter fields:https://dev.twitter.com/docs/platform-objects/tweets
         * TWEET FIELDS INCLUDED IN THIS API
            coordinates
            created_at
            entities
            favorite_count
            favorited
            id
            id_str
            in_reply_to_screen_name
            in_reply_to_status_id_str
            in_reply_to_user_id_str
            lang
            place.full_name
            place.country   
            possibly_sensitive
            retweet_count
            retweeted
            source
            truncated
            withheld_copyright
            withheld_in_countries
            withheld_scope*/

            //TWEET fields ignored for now: 
            //annotations (doesn't exist yet)
            //contributors (sounds pretty useless as most tweet have no contributors, only the creator of tweet is contributor)
            //current_user_retweet(Only surfaces on methods supporting the include_my_retweet parameter, when set to true. Details the Tweet ID of the user's own retweet (if existent) of this Tweet.)
            //filter_level (only for stream)
            //in_reply_to_status_id
            //in_reply_to_user_id
            //place.everything apart from 2 fields (rest seems pretty useless)
            //scopes (A set of key-value pairs indicating the intended contextual delivery of the containing Tweet. Currently used by Twitter's Promoted Products)
            //retweeted_status(This attribute contains a representation of the original Tweet that was retweeted. Note that retweets of retweets do not show representations of the intermediary retweet, but only the original tweet. )
            //truncated (derecated)
            //user (too complicated, datatype is USER would have to convert everything into string...Documentation says: The user who posted this Tweet. Perspectival attributes embedded within this object are unreliable.)
            
            
        public List<string> listUserName = new List<string>();
        public List<string> listUserScreenName = new List<string>();
        public List<string> listUserPicUrl = new List<string>();
        public List<string> listUserDescription = new List<string>();
        public List<string> listUserCreatedAt = new List<string>();
        public List<bool> listUserDefaultPicBool = new List<bool>();
        public List<int> listUserFavouriteCount = new List<int>();
        public List<int> listUserFollowersCount = new List<int>();
        public List<bool> listUserGeoEnabledBool = new List<bool>();
        public List<string> listUserId = new List<string>();
        public List<string> listUserLang = new List<string>();
        public List<string> listUserLocation = new List<string>();
        public List<bool> listUserUseBackPicBool = new List<bool>();
        public List<string> listUserBackPicUrl = new List<string>();
        public List<string> listUserBannerUrl = new List<string>();
        public List<int> listUserStatusCount = new List<int>();
        public List<string> listUserTimeZone = new List<string>();
        public List<string> listUserUrl = new List<string>();
        public List<int> listUserUtcOffset = new List<int>();
        public List<string> listUserWithheld = new List<string>();

        /*  
         * List of all TWITTER API Tweeter fields:https://dev.twitter.com/docs/platform-objects/tweets
         * TWEET FIELDS INCLUDED IN THIS API
            username
            screenname
            picUrl
            description
            created at
            default pic bool
            favourite count
            followers count
            Geo loc enabled
            user id string
            user language
            user location
            user backgronud pic bool
            user background pic url
            */

        //TWEET fields ignored for now: 
        //annotations (doesn't exist yet)
        //contributors (sounds pretty useless as most tweet have no contributors, only the creator of tweet is contributor)
        //current_user_retweet(Only surfaces on methods supporting the include_my_retweet parameter, when set to true. Details the Tweet ID of the user's own retweet (if existent) of this Tweet.)
        //filter_level (only for stream)
        //in_reply_to_status_id
        //in_reply_to_user_id
        //place.everything apart from 2 fields (rest seems pretty useless)
        //scopes (A set of key-value pairs indicating the intended contextual delivery of the containing Tweet. Currently used by Twitter's Promoted Products)
        //retweeted_status(This attribute contains a representation of the original Tweet that was retweeted. Note that retweets of retweets do not show representations of the intermediary retweet, but only the original tweet. )
        //truncated (derecated)
        //user (too complicated, datatype is USER would have to convert everything into string...Documentation says: The user who posted this Tweet. Perspectival attributes embedded within this object are unreliable.)
            

        //status(tweets object)
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
        
        public void Do_Search(){
           string tPicSize = FPicSize[0].ToString();
           ITweetSearchParameters searchPara = GetTwitterSearchParameters();
           try
           {
               var tweets = Search.SearchTweets(searchPara);
               foreach (var tweet in tweets)
               {
                   numOfTweets++;
                   listText.Insert(0, tweet.Text);
                   FLogger.Log(LogType.Debug, "FOUND A TWEET " + numOfTweets);
                   if (FUserSearchControls[0])
                   {
                       listUserName.Insert(0, tweet.Creator.Name);
                   }
                   if (FUserSearchControls[1])
                   {
                       listUserScreenName.Insert(0, tweet.Creator.ScreenName);
                   }
                   if (FUserSearchControls[2])
                   {
                       listUserPicUrl.Insert(0, tweet.Creator.ProfileImageUrl);
                   }
                   if (FUserSearchControls[3])
                   {
                       listUserDescription.Insert(0, tweet.Creator.Description);
                   }
                   if (FUserSearchControls[4])
                   {
                       //WE MIGHT WANNA USE DATE as DATA TYPE
                       listUserCreatedAt.Insert(0, tweet.Creator.CreatedAt.ToString());
                   }
                   if (FUserSearchControls[5])
                   {
                       listUserDefaultPicBool.Insert(0, tweet.Creator.DefaultProfileImage);
                   }
                   if (FUserSearchControls[6])
                   {
                       listUserFavouriteCount.Insert(0, tweet.Creator.FavouritesCount);
                   }
                   if (FUserSearchControls[7])
                   {
                       listUserFollowersCount.Insert(0, tweet.Creator.FollowersCount);
                   }
                   if (FUserSearchControls[8])
                   {
                       listUserGeoEnabledBool.Insert(0, tweet.Creator.GeoEnabled);
                   }
                   if (FUserSearchControls[9])
                   {
                       listUserId.Insert(0, tweet.Creator.IdStr);
                   }
                   if (FUserSearchControls[10])
                   {
                       listUserLang.Insert(0, tweet.Creator.Language.ToString());
                   }
                   if (FUserSearchControls[11])
                   {
                       listUserLocation.Insert(0, tweet.Creator.Location.ToString());
                   }
                   if (FUserSearchControls[12])
                   {
                       listUserUseBackPicBool.Insert(0, tweet.Creator.ProfileUseBackgroundImage);
                   }
                   if (FUserSearchControls[13])
                   {
                       listUserBackPicUrl.Insert(0, tweet.Creator.ProfileBackgroundImageUrl);
                   }
                   if (FUserSearchControls[14])
                   {
                       listUserBannerUrl.Insert(0, tweet.Creator.ProfileBannerURL);
                   }
                   if (FUserSearchControls[15])
                   {
                       listUserStatusCount.Insert(0, tweet.Creator.StatusesCount);
                   }
                   if (FUserSearchControls[16])
                   {
                       listUserTimeZone.Insert(0, tweet.Creator.TimeZone);
                   }
                   if (FUserSearchControls[17])
                   {
                       listUserUrl.Insert(0, tweet.Creator.Url);
                   }
                   if (FUserSearchControls[18])
                   {
                       listUserUtcOffset.Insert(0, ConvertToInt32(tweet.Creator.UtcOffset, 0));
                   }
                   if (FUserSearchControls[19])
                   {
                       if (tweet.Creator.WithheldInCountries != null)
                       {
                           listUserWithheld.Insert(0, String.Join(", ", tweet.Creator.WithheldInCountries.ToArray()));
                       }
                       else
                       {
                           listUserWithheld.Insert(0, "Not known");
                       }
                   }
                   if (FTweetSearchControls[0])
                   {
                       if (tweet.Coordinates != null)
                       {
                           string st = tweet.Coordinates.Latitude.ToString() + ", " + tweet.Coordinates.Longitude.ToString();
                           listTweetCoordinates.Insert(0, st);
                       }
                       else
                       {
                           listTweetCoordinates.Insert(0, "No coordinates");

                       }
                   }
                   if (FTweetSearchControls[1])
                   {
                       listTweetCreatedAt.Insert(0, tweet.CreatedAt.ToString());
                   }
                   if (FTweetSearchControls[2])
                   {
                       if (tweet.Entities != null)
                       {
                           if (tweet.Entities.Medias != null)
                           {
                               if (tweet.Entities.Medias.First().MediaType == "photo")
                               {
                                   //foreach(var mediaUrl in tweet.Entities.Medias.Where(a => a.IdStr == "mesia_url"))
                                   var media = tweet.Entities.Medias.First();
                                   listTweetAttachedPicUrl.Insert(0, media.MediaURL + ":" + tPicSize);


                                   var size = media.Sizes.Where(a => a.Key.ToString() == tPicSize).First();

                                   var mediumWidth = size.Value.Width;
                                   var mediumHeight = size.Value.Height;
                                   listTweetAttachedPicWidth.Insert(0, (int)mediumWidth);
                                   listTweetAttachedPicHeight.Insert(0, (int)mediumHeight);

                               }
                               else
                               {
                                   listTweetAttachedPicUrl.Insert(0, "No pic attached");
                                   listTweetAttachedPicWidth.Insert(0, 0);
                                   listTweetAttachedPicHeight.Insert(0, 0);
                               }
                           }
                           else
                           {
                               listTweetAttachedPicUrl.Insert(0, "No pic attached");
                               listTweetAttachedPicWidth.Insert(0, 0);
                               listTweetAttachedPicHeight.Insert(0, 0);
                           }
                       }
                       else
                       {
                           listTweetAttachedPicUrl.Insert(0, "No pic attached");
                           listTweetAttachedPicWidth.Insert(0, 0);
                           listTweetAttachedPicHeight.Insert(0, 0);
                       }
                   }

                   if (FTweetSearchControls[3])
                   {
                       if (tweet.Entities != null)
                       {
                           if (tweet.Entities.Urls != null && tweet.Entities.Urls.IsEmpty() == false)
                           {
                               string urlAttached = tweet.Entities.Urls.First().URL;
                               listTweetAttachedUrl.Insert(0, urlAttached);
                           }
                           else
                           {
                               listTweetAttachedUrl.Insert(0, "Tweet has no URL");

                           }
                       }
                       else
                       {
                           listTweetAttachedUrl.Insert(0, "Tweet has no URL");

                       }
                   }

                   if (FTweetSearchControls[4])
                   {
                       listTweetFavoriteCount.Insert(0, tweet.FavouriteCount);
                   }
                   if (FTweetSearchControls[5])
                   {
                       listTweetFavorited.Insert(0, tweet.Favourited);
                   }
                   if (FTweetSearchControls[6])
                   {
                       if (tweet.Place != null)
                       {
                           listTweetPlace.Insert(0, tweet.Place.Country);
                       }
                       else
                       {
                           listTweetPlace.Insert(0, "Country not known");
                       }
                       listTweetCountry.Insert(0, tweet.Id);
                   }
                   if (FTweetSearchControls[7])
                   {
                       listTweetIdStr.Insert(0, tweet.IdStr);
                   }
                   if (FTweetSearchControls[8])
                   {
                       listTweetInReplyScreenName.Insert(0, tweet.InReplyToScreenName);
                   }
                   if (FTweetSearchControls[9])
                   {
                       listTweetInReplyStatusIdStr.Insert(0, tweet.InReplyToStatusIdStr);
                   }
                   if (FTweetSearchControls[10])
                   {
                       listTweetinReplyToUserIdStr.Insert(0, tweet.InReplyToUserIdStr);
                   }
                   if (FTweetSearchControls[11])
                   {
                       listTweetLang.Insert(0, tweet.Language.ToString());
                   }
                   if (FTweetSearchControls[12])
                   {
                       if (tweet.Place != null)
                       {
                           listTweetPlace.Insert(0, tweet.Place.FullName);
                       }
                       else
                       {
                           listTweetPlace.Insert(0, "Place not known");
                       }
                   }
                   if (FTweetSearchControls[13])
                   {
                       listTweetPossiblySensitive.Insert(0, tweet.PossiblySensitive);
                   }
                   if (FTweetSearchControls[14])
                   {
                       listTweetRetweetCount.Insert(0, tweet.RetweetCount);
                   }
                   if (FTweetSearchControls[15])
                   {
                       listTweetRetweeted.Insert(0, tweet.Retweeted);
                   }
                   if (FTweetSearchControls[16])
                   {
                       listTweetSource.Insert(0, tweet.Source);
                   }

                   if (FTweetSearchControls[17])
                   {
                       listTweetWithheldCopyright.Insert(0, tweet.WithheldCopyright);
                   }
                   if (FTweetSearchControls[18])
                   {
                       if (tweet.WithheldInCountries != null)
                       {
                           listTweetWithheld.Insert(0, String.Join(", ", tweet.WithheldInCountries.ToArray()));
                       }
                       else
                       {
                           listTweetWithheld.Insert(0, "Not known");
                       }
                   }
                   if (FTweetSearchControls[19])
                   {
                       listTweetWithheldScope.Insert(0, tweet.WithheldScope);
                   }

               }
               foundNewTweets = true;
               searching = false;
               timo.Enabled = false;
               lastSearchStatus = "DONE! Totally fucking winning!"; 
           }
           catch (WebException e)
           {
               lastSearchStatus = "You have reached your search limit";
               FLogger.Log(LogType.Debug, "You have reached your search limit");
               throw new WebException("You have reached your search limit", e);
           }
           
        }
        public ITweetSearchParameters GetTwitterSearchParameters()
        {

            double[] sincee = new double[] { FSearchSince[0].x, FSearchSince[0].y, FSearchSince[0].z };
            double[] untile = new double[] { FSearchUntil[0].x, FSearchUntil[0].y, FSearchUntil[0].z };
            double[] geoLocal = new double[] { FSearchGeolocalised[0].x, FSearchGeolocalised[0].y, FSearchGeolocalised[0].z };
                
            
            string resultType=FResultType[0].ToString();
            
            var searchParameter = Search.GenerateSearchTweetParameter(FSearchTerm[0]);
            searchParameter.MaximumNumberOfResults = FSearchCount[0];

            if(resultType=="Mixed"){
                searchParameter.SearchType = SearchResultType.Mixed;
            }else if(resultType=="Popular"){
                searchParameter.SearchType = SearchResultType.Popular;
            }else{
                searchParameter.SearchType = SearchResultType.Recent;
            }

            if (FIgnoreRetweets[0])
            {
                searchParameter.TweetSearchFilter = TweetSearchFilter.OriginalTweetsOnly;
            }else{
                searchParameter.TweetSearchFilter = TweetSearchFilter.All;
            }

            
            if (!untile.Contains(0))
            {
                try
                {
                    searchParameter.Until = new DateTime((int)untile[2], (int)untile[1], (int)untile[0]);
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    lastSearchStatus = "Until date badly formated";
                    throw new System.ArgumentOutOfRangeException("Date badly formated", e);
                }
            }
            if (!sincee.Contains(0))
            {
                try
                {
                    searchParameter.Since = new DateTime((int)sincee[2], (int)sincee[1], (int)sincee[0]);                    
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    lastSearchStatus = "Since date badly formated";
                    throw new System.ArgumentOutOfRangeException("Date badly formated", e);
                }

            }
            if (!geoLocal.Contains(0))
            {
                try
                {
                    searchParameter.SetGeoCode(geoLocal[0], geoLocal[1], (int)geoLocal[2], DistanceMeasure.Kilometers);                  
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    lastSearchStatus = "Geoloc search went fucking wrong";
                    throw new System.ArgumentOutOfRangeException("Geoloc search went fucking wrong", e);
                }

            }

            return searchParameter;
            
            
        }
        public void clearLists()
        {
            listText.Clear();
            listTweetAttachedPicHeight.Clear();
            listTweetAttachedPicUrl.Clear();
            listTweetAttachedPicWidth.Clear();
            listTweetAttachedUrl.Clear();
            listTweetCoordinates.Clear();
            listTweetCreatedAt.Clear();
            listTweetFavoriteCount.Clear();
            listTweetFavorited.Clear();
            listTweetCountry.Clear();
            listTweetIdStr.Clear();
            listTweetInReplyScreenName.Clear();
            listTweetInReplyStatusIdStr.Clear();
            listTweetinReplyToUserIdStr.Clear();
            listTweetLang.Clear();
            listTweetPlace.Clear();
            listTweetPossiblySensitive.Clear();
            listTweetRetweetCount.Clear();
            listTweetRetweeted.Clear();
            listTweetSource.Clear();
            listTweetWithheld.Clear();
            listTweetWithheldCopyright.Clear();
            listTweetWithheldScope.Clear();
            listUserBackPicUrl.Clear();
            listUserBannerUrl.Clear();
            listUserCreatedAt.Clear();
            listUserDefaultPicBool.Clear();
            listUserDescription.Clear();
            listUserFavouriteCount.Clear();
            listUserFollowersCount.Clear();
            listUserGeoEnabledBool.Clear();
            listUserId.Clear();
            listUserLang.Clear();
            listUserLocation.Clear();
            listUserName.Clear();
            listUserPicUrl.Clear();
            listUserScreenName.Clear();
            listUserStatusCount.Clear();
            listUserTimeZone.Clear();
            listUserUrl.Clear();
            listUserUseBackPicBool.Clear();
            listUserUtcOffset.Clear();
            listUserWithheld.Clear();

        }

        public static int ConvertToInt32(object value, int defaultValue)
        {
            if (value == null)
                return defaultValue;
            return Convert.ToInt32(value);
        }






        
		#region fields & pins

        [Input("Credentials")]
        ISpread<string> FCredentials;

        [Input("Login", IsBang = true, IsSingle = true)]
        ISpread<bool> FLogin;

        [Input("Search For", IsSingle = true, DefaultString = "#london")]
        ISpread<string> FSearchTerm;

        [Input("Max Num of Tweets Per Search", IsSingle = true)]
        ISpread<int> FSearchCount;

        [Input("Tweet Search Controls")]
        ISpread<bool> FTweetSearchControls;

        [Input("User Search Controls")]
        ISpread<bool> FUserSearchControls;

        [Input("Type of results expected", IsSingle = true, DefaultEnumEntry = "Mixed")]
        public IDiffSpread<TweetResultType> FResultType;

        [Input("Tweet Picture Size", IsSingle = true, DefaultEnumEntry = "medium")]
        public IDiffSpread<TweetPictureSize> FPicSize;

        [Input("Incremental Search Results", IsSingle = true)]
        ISpread<bool> FSearchIncremental;

        [Input("Ignore Retweets", IsSingle = true)]
        public ISpread<bool> FIgnoreRetweets;

        [Input("Search Since", AsInt = true, DimensionNames = (new string[] { "(Day/", "Month/", "Year)" }), MaxValue = 4000, MinValue = 0, DefaultValues = (new double[] {0,0,0}))]
        ISpread<Vector3D> FSearchSince;

        [Input("Search Until", AsInt = true, DimensionNames = (new string[] { "(Day/", "Month/", "Year)" }), MaxValue = 4000, MinValue = 0, DefaultValues = (new double[] {0,0,0}))]
        ISpread<Vector3D> FSearchUntil;

        [Input("Geoloc Search", DimensionNames = (new string[] { "(Lat/", "Long/", "Radius-Km)" }), MaxValue = 4000, MinValue = 0, DefaultValues = (new double[] { 0, 0, 0 }))]
        ISpread<Vector3D> FSearchGeolocalised;

        [Input("Do Search", IsBang = true, IsSingle = true)]
        ISpread<bool> FSearch;
        
        [Input("Clear Search", IsBang = true, IsSingle = true)]
        ISpread<bool> FClearSearch;

        [Output("Logged In?", IsSingle = true)]
        public ISpread<string> FOutputStatus;

        [Output("Tweet Text")]
        public ISpread<string> FOutputTweetText;

        [Output("Tweet Coordinates")]
        public ISpread<string> FOutputTweetCoord;

        [Output("Tweet Created At")]
        public ISpread<string> FOutputTweetCreat;

        [Output("Tweet Attached Picture URL")]
        public ISpread<string> FOutputTweetAttachedPicUrl;

        [Output("Tweet Attached Picture Width")]
        public ISpread<int> FOutputTweetAttachedPicWidth;

        [Output("Tweet Attached Picture Height")]
        public ISpread<int> FOutputTweetAttachedPicHeight;

        [Output("Tweet 1st URL In Text")]
        public ISpread<string> FOutputTweetAttachedUrl;

        [Output("Tweet Favourite Count")]
        public ISpread<int> FOutputTweetFavCount;

        [Output("Tweet Favorited Bool")]
        public ISpread<bool> FOutputTweetFavBool;

        [Output("Tweet Country")]
        public ISpread<long> FOutputTweetCountry;

        [Output("Tweet Id String")]
        public ISpread<string> FOutputTweetIdString;

        [Output("Tweet In Reply To Screen Name")]
        public ISpread<string> FOutputInReplyScreenName;

        [Output("Tweet In Reply To Status Id")]
        public ISpread<string> FOutputReplyStatusId;

        [Output("Tweet In Reply To User Id")]
        public ISpread<string> FOutputReplyUserId;

        [Output("Tweet Language")]
        public ISpread<string> FOutputTweetLanguage;

        [Output("Tweet Place")]
        public ISpread<string> FOutputTweetPlace;

        [Output("Tweet Possibly Sensitive Bool")]
        public ISpread<bool> FOutputTweetSensitive;

        [Output("Tweet Retweet Count")]
        public ISpread<int> FOutputTweetRetweetCount;

        [Output("Tweet Retweeted Bool")]
        public ISpread<bool> FOutputTweetRetweeted;

        [Output("Tweet Source")]
        public ISpread<string> FOutputTweetSource;

        [Output("Tweet Witheld Copyright")]
        public ISpread<bool> FOutputTweetWithheldCopy;

        [Output("Tweet Witheld In Countries")]
        public ISpread<string> FOutputTweetWithheldInCountries;

        [Output("Tweet Withheld Scope")]
        public ISpread<string> FOutputTweetWitheldScope;

        [Output("Tweet User Name")]
        public ISpread<string> FOutputTweetUserName;

        [Output("Tweet User Sreen Name")]
        public ISpread<string> FOutputTweetUserScreenName;

        [Output("Tweet User Pic URL")]
        public ISpread<string> FOutputTweetUserPicUrl;

        [Output("Tweet User Description")]
        public ISpread<string> FOutputTweetUserDescription;

        [Output("Tweet User Created At")]
        public ISpread<string> FOutputTweetUserCreatedAt;
        
        [Output("Tweet User Default Pic Bool")]
        public ISpread<bool> FOutputTweetUserDefaultPicBool;

        [Output("Tweet User Number of Favourites")]
        public ISpread<int> FOutputTweetUserFavouritesCount;

        [Output("Tweet User Number of Followers")]
        public ISpread<int> FOutputTweetUserFollowersCount;

        [Output("Tweet User Geo Enabled Bool")]
        public ISpread<bool> FOutputTweetUserGeoEnabledBool;

        [Output("Tweet User Id")]
        public ISpread<string> FOutputTweetUserId;

        [Output("Tweet User Language")]
        public ISpread<string> FOutputTweetUserLanguage;

        [Output("Tweet User Location")]
        public ISpread<string> FOutputTweetUserLocation;

        [Output("Tweet User Background Picture Usage Bool")]
        public ISpread<bool> FOutputTweetUserBackPicBool;

        [Output("Tweet User Background Picture Url")]
        public ISpread<string> FOutputTweetUserBackPicUrl;

        [Output("Tweet User Banner Picture Url")]
        public ISpread<string> FOutputTweetUserBannerPic;

        [Output("Tweet User Status Count(number of tweets)")]
        public ISpread<int> FOutputTweetUserStatusCount;

        [Output("Tweet User Time Zone")]
        public ISpread<string> FOutputTweetUserTimeZone;

        [Output("Tweet User Url")]
        public ISpread<string> FOutputTweetUserUrl;

        [Output("Tweet User UTC Offset")]
        public ISpread<int> FOutputTweetUserUtcOffset;

        [Output("Tweet User Withheld from")]
        public ISpread<string> FOutputTweetUserWitheld;
        
        [Output("Number of tweets found", IsSingle = true)]
        public ISpread<int> FOutputNumOfTweets;

        [Output("Last search status", IsSingle = true)]
        public ISpread<string> FLastSearchStatus;

        [Import()]
        public ILogger FLogger;
        
        #endregion fields & pins

        public void clearOutputs(){
            FOutputNumOfTweets[0] = 0;
            FOutputTweetAttachedPicHeight.SliceCount = 1;
            FOutputTweetAttachedPicUrl.SliceCount = 1;
            FOutputTweetAttachedUrl.SliceCount = 1;
            FOutputTweetAttachedPicWidth.SliceCount = 1;
            FOutputTweetCoord.SliceCount = 1;
            FOutputTweetCreat.SliceCount = 1;
            FOutputTweetFavBool.SliceCount = 1;
            FOutputTweetFavCount.SliceCount = 1;
            FOutputTweetCountry.SliceCount = 1;
            FOutputTweetIdString.SliceCount = 1;
            FOutputTweetLanguage.SliceCount = 1;
            FOutputTweetPlace.SliceCount = 1;
            FOutputTweetRetweetCount.SliceCount = 1;
            FOutputTweetRetweeted.SliceCount = 1;
            FOutputTweetSensitive.SliceCount = 1;
            FOutputTweetSource.SliceCount = 1;
            FOutputTweetText.SliceCount = 1;
            FOutputTweetUserBackPicBool.SliceCount = 1;
            FOutputTweetUserBackPicUrl.SliceCount = 1;
            FOutputTweetUserBannerPic.SliceCount = 1;
            FOutputTweetUserCreatedAt.SliceCount = 1;
            FOutputTweetUserDefaultPicBool.SliceCount = 1;
            FOutputTweetUserDescription.SliceCount = 1;
            FOutputTweetUserFavouritesCount.SliceCount = 1;
            FOutputTweetUserFollowersCount.SliceCount = 1;
            FOutputTweetUserGeoEnabledBool.SliceCount = 1;
            FOutputTweetUserId.SliceCount = 1;
            FOutputTweetUserLanguage.SliceCount = 1;
            FOutputTweetUserLocation.SliceCount = 1;
            FOutputTweetUserName.SliceCount = 1;
            FOutputTweetUserPicUrl.SliceCount = 1;
            FOutputTweetUserScreenName.SliceCount = 1;
            FOutputTweetUserStatusCount.SliceCount = 1;
            FOutputTweetUserTimeZone.SliceCount = 1;
            FOutputTweetUserUrl.SliceCount = 1;
            FOutputTweetUserUtcOffset.SliceCount = 1;
            FOutputTweetUserWitheld.SliceCount = 1;
            FOutputTweetWitheldScope.SliceCount = 1;
            FOutputTweetWithheldCopy.SliceCount = 1;
            FOutputTweetWithheldInCountries.SliceCount = 1;

            FOutputTweetAttachedPicHeight[0] = 0;
            FOutputTweetAttachedPicUrl[0] = "";
            FOutputTweetAttachedUrl[0] = "";
            FOutputTweetAttachedPicWidth[0] = 0;
            FOutputTweetCoord[0] = "";
            FOutputTweetCreat[0] = "";
            FOutputTweetFavBool[0] = false;
            FOutputTweetFavCount[0] = 0;
            FOutputTweetCountry[0] = 0;
            FOutputTweetIdString[0] = "";
            FOutputTweetLanguage[0] = "";
            FOutputTweetPlace[0] = "";
            FOutputTweetRetweetCount[0] = 0;
            FOutputTweetRetweeted[0] = false;
            FOutputTweetSensitive[0] = false;
            FOutputTweetSource[0] = "";
            FOutputTweetText[0] = "";
            FOutputTweetUserBackPicBool[0] = false;
            FOutputTweetUserBackPicUrl[0] = "";
            FOutputTweetUserBannerPic[0] = "";
            FOutputTweetUserCreatedAt[0] = "";
            FOutputTweetUserDefaultPicBool[0] = false;
            FOutputTweetUserDescription[0] = "";
            FOutputTweetUserFavouritesCount[0] = 0;
            FOutputTweetUserFollowersCount[0] = 0;
            FOutputTweetUserGeoEnabledBool[0] = false;
            FOutputTweetUserId[0] = "";
            FOutputTweetUserLanguage[0] = "";
            FOutputTweetUserLocation[0] = "";
            FOutputTweetUserName[0] = "";
            FOutputTweetUserPicUrl[0] = "";
            FOutputTweetUserScreenName[0] = "";
            FOutputTweetUserStatusCount[0] = 0;
            FOutputTweetUserTimeZone[0] = "";
            FOutputTweetUserUrl[0] = "";
            FOutputTweetUserUtcOffset[0] = 0;
            FOutputTweetUserWitheld[0] = "";
            FOutputTweetWitheldScope[0] = "";
            FOutputTweetWithheldCopy[0] = false;
            FOutputTweetWithheldInCountries[0] = "";
        }

        public void OnTimerFinished(Object source, ElapsedEventArgs e)
        {   
            if(!CheckForInternetConnection()){
                lastSearchStatus = "Your internet is fucked.";
            }
            else
            {
                lastSearchStatus = "Search returned no results OR you have exceeded your search rate limit (180 per 15 minutes)";
            }
            timo.Enabled = false;
            searching = false;

        }
        #region Evaluate Function
        public void Evaluate(int SpreadMax)
		{
            //FSearchSince.SliceCount=3;
            FLastSearchStatus[0]=lastSearchStatus;
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
            if (FSearch[0])
            {
                if (loggedIn)
                {
                if (!searching){                
                    double[] sinco = new double[] { FSearchSince[0].x, FSearchSince[0].y, FSearchSince[0].z };
                    double[] untilo = new double[] { FSearchUntil[0].x, FSearchUntil[0].y, FSearchUntil[0].z };
                    double[] geoLoc = new double[] { FSearchGeolocalised[0].x, FSearchGeolocalised[0].y, FSearchGeolocalised[0].z };
                    if (!FSearchIncremental[0])
                    {
                        clearLists();
                    }
                    numOfTweets = 0;
                
                    try
                    {
                        Task SearchTask = new Task(Do_Search);
                        SearchTask.Start();
                        timo.Elapsed += OnTimerFinished;
                        timo.Enabled = true;
                        FLogger.Log(LogType.Debug, "SEACHING, PLEASE WAIT...");
                        lastSearchStatus = "Searching...";
                        searching = true;                        
                    }
                    catch (System.ArgumentNullException e)
                    {
                        lastSearchStatus = "It's fucked! Not logged in or no internet";
                        FLogger.Log(LogType.Debug, "It's fucked! Not logged in or no internet");
                        throw new System.ArgumentNullException("It's fucked! Not logged in or no internet", e);
                    }
                }
                else
                {
                        lastSearchStatus = "Chill out bro it's already searching";
                }
                }
                else
                {
                    lastSearchStatus = "Login first you bellend";
                }
            }
            if (FClearSearch[0])
            {
                clearLists();
                clearOutputs();
            }
            if(foundNewTweets){

                totalNumOfTweets =  listText.Count;
                FLogger.Log(LogType.Debug, "number of tweets found: " + numOfTweets);
                FOutputNumOfTweets[0] = numOfTweets;
                FOutputTweetText.SliceCount = totalNumOfTweets;

                for (int i = 0; i < totalNumOfTweets; i++)
                {
                    FOutputTweetText[i] = listText[i];
                }
                
                if(FUserSearchControls[0]){
                    FOutputTweetUserName.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                    FOutputTweetUserName[i] = listUserName[i];
                    }
                }
                if(FUserSearchControls[1]){
                    FOutputTweetUserScreenName.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserScreenName[i] = listUserScreenName[i];
                    }
                }
                
                if(FUserSearchControls[2]){
                    FOutputTweetUserPicUrl.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserPicUrl[i] = listUserPicUrl[i];
                        
                    }
                }
                
                if(FUserSearchControls[3]){
                    FOutputTweetUserDescription.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserDescription[i] = listUserDescription[i];
                        
                    }
                }
                
                if(FUserSearchControls[4]){
                    FOutputTweetUserCreatedAt.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserCreatedAt[i] = listUserCreatedAt[i];
                        
                    }
                }
                if(FUserSearchControls[5]){
                    FOutputTweetUserDefaultPicBool.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserDefaultPicBool[i] = listUserDefaultPicBool[i];
                        
                    }
                }
                if(FUserSearchControls[6]){
                    FOutputTweetUserFavouritesCount.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserFavouritesCount[i] = listUserFavouriteCount[i];
                        
                    }
                }
                if(FUserSearchControls[7]){
                    FOutputTweetUserFollowersCount.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserFollowersCount[i] = listUserFollowersCount[i];
                        
                    }
                }
                if(FUserSearchControls[8]){
                    FOutputTweetUserGeoEnabledBool.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserGeoEnabledBool[i] = listUserGeoEnabledBool[i];
                        
                    }
                }
                if(FUserSearchControls[9]){
                    FOutputTweetUserId.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserId[i] = listUserId[i];
                        
                    }
                }
                if(FUserSearchControls[10]){
                    FOutputTweetUserLanguage.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserLanguage[i] = listUserLang[i];
                        
                    }
                }
                if(FUserSearchControls[11]){
                    FOutputTweetUserLocation.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserLocation[i] = listUserLocation[i];
                        
                    }
                }
                if(FUserSearchControls[12]){
                    FOutputTweetUserBackPicBool.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserBackPicBool[i] = listUserUseBackPicBool[i];
                        
                    }
                }
                if(FUserSearchControls[13]){
                    FOutputTweetUserBackPicUrl.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserBackPicUrl[i] = listUserBackPicUrl[i];
                        
                    }
                }
                if(FUserSearchControls[14]){
                    FOutputTweetUserBannerPic.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserBannerPic[i] = listUserBannerUrl[i];
                        
                    }
                }
                if(FUserSearchControls[15]){
                    FOutputTweetUserStatusCount.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserStatusCount[i] = listUserStatusCount[i];
                        
                    }
                }
                if(FUserSearchControls[16]){
                    FOutputTweetUserTimeZone.SliceCount = totalNumOfTweets;
                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserTimeZone[i] = listUserTimeZone[i];
                        
                    }
                }
                if(FUserSearchControls[17]){
                    FOutputTweetUserUrl.SliceCount = totalNumOfTweets;                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserUrl[i] = listUserUrl[i];
                        
                    }
                }
                if(FUserSearchControls[18]){
                    FOutputTweetUserUtcOffset.SliceCount = totalNumOfTweets;
                    
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserUtcOffset[i] = listUserUtcOffset[i];
                        
                    }
                }
                if(FUserSearchControls[19]){
                    FOutputTweetUserWitheld.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetUserWitheld[i] = listUserWithheld[i];
                    }
                }
                if(FTweetSearchControls[0]){
                    FOutputTweetCoord.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetCoord[i] = listTweetCoordinates[i];
                    }
                }
                if (FTweetSearchControls[1])
                {
                    FOutputTweetCreat.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetCreat[i] = listTweetCreatedAt[i];
                    }
                }
                
                if(FTweetSearchControls[2]){
                    FOutputTweetAttachedPicUrl.SliceCount = totalNumOfTweets;
                    FOutputTweetAttachedPicWidth.SliceCount = totalNumOfTweets;
                    FOutputTweetAttachedPicHeight.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetAttachedPicUrl[i] = listTweetAttachedPicUrl[i];
                        FOutputTweetAttachedPicWidth[i] = listTweetAttachedPicWidth[i];
                        FOutputTweetAttachedPicHeight[i] = listTweetAttachedPicHeight[i];
                    }
                }
                if (FTweetSearchControls[3])
                {
                    FOutputTweetAttachedUrl.SliceCount = totalNumOfTweets;
                   
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetAttachedUrl[i] = listTweetAttachedUrl[i];
                      
                    }
                }
                if (FTweetSearchControls[4])
                {
                    FOutputTweetFavCount.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetFavCount[i] = listTweetFavoriteCount[i];
                    }
                }
                if (FTweetSearchControls[5])
                {
                    FOutputTweetFavBool.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetFavBool[i] = listTweetFavorited[i];
                    }
                }
                if (FTweetSearchControls[6])
                {
                    FOutputTweetCountry.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetCountry[i] = listTweetCountry[i];
                    }
                }
                if (FTweetSearchControls[7])
                {
                    FOutputTweetIdString.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetIdString[i] = listTweetIdStr[i];
                    }
                }
                if (FTweetSearchControls[8])
                {
                    FOutputInReplyScreenName.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputInReplyScreenName[i] = listTweetInReplyScreenName[i];
                    }
                }
                if (FTweetSearchControls[9])
                {
                    FOutputReplyStatusId.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputReplyStatusId[i] = listTweetInReplyStatusIdStr[i];
                    }
                }
                if (FTweetSearchControls[10])
                {
                    FOutputReplyUserId.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputReplyUserId[i] = listTweetinReplyToUserIdStr[i];
                    }
                }
                if (FTweetSearchControls[11])
                {
                    FOutputTweetLanguage.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetLanguage[i] = listTweetLang[i];
                    }
                }
                if (FTweetSearchControls[12])
                {
                    FOutputTweetPlace.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetPlace[i] = listTweetPlace[i];
                    }
                }
                if (FTweetSearchControls[13])
                {
                    FOutputTweetSensitive.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetSensitive[i] = listTweetPossiblySensitive[i];
                    }
                }
                if (FTweetSearchControls[14])
                {
                    FOutputTweetRetweetCount.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetRetweetCount[i] = listTweetRetweetCount[i];
                    }
                }
                if (FTweetSearchControls[15])
                {
                    FOutputTweetRetweeted.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetRetweeted[i] = listTweetRetweeted[i];
                    }
                }
                if (FTweetSearchControls[16])
                {
                    FOutputTweetSource.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetSource[i] = listTweetSource[i];
                    }
                }
                if (FTweetSearchControls[17])
                {
                    FOutputTweetWithheldCopy.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetWithheldCopy[i] = listTweetWithheldCopyright[i];
                    }
                }
                
                if (FTweetSearchControls[18])
                {
                    FOutputTweetWithheldInCountries.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetWithheldInCountries[i] = listTweetWithheld[i];
                    }
                }
                if (FTweetSearchControls[19])
                {
                    FOutputTweetWitheldScope.SliceCount = totalNumOfTweets;
                    for (int i = 0; i < totalNumOfTweets; i++)
                    {
                        FOutputTweetWitheldScope[i] = listTweetWithheldScope[i];
                    }
                }
                foundNewTweets = false;
            }

        }
        #endregion Evaluate Function


    }
   
    }
