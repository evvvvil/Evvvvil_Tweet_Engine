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
	[PluginInfo(Name = "EvvvvilTweetAction", Category = "Network", Help = "Part 3 of the ultimate Twitter API vvvv plugin, bitch.", Tags = "twitter, network", Author="evvvvil")]
	#endregion PluginInfo
    public class EvvvvilTweetAction : IPluginEvaluate
	{
       
		#region fields & pins

        [Input("Logged In?", IsSingle = true)]
        public ISpread<bool> FLoginBool;

        [Input("Publish Tweet Text", MaxChars=140)]
        public ISpread<string> FPublishText;

        [Input("Publish Picture", StringType = StringType.Filename, DefaultString="")]
        public ISpread<string> FPublishPicture;

        [Input("Machine Gun Mode?", IsSingle = true, DefaultBoolean=true)]
        public ISpread<bool> FMachineGun;

        [Input("Machine Gun Interval(Seconds)", IsSingle = true, DefaultValue = 1, MinValue=1)]
        public ISpread<int> FMachineGunInterval;

        [Input("Randomize replies?", IsSingle = true, DefaultBoolean = false)]
        public ISpread<bool> FRandom;

        [Input("Publish In Reply To(Tweet Ids)")]
        public ISpread<string> FPublishInReplyIds;

        [Input("Publish Coordinates")]
        public ISpread<double> FPublishCoordinates;

        [Input("Publish Tweet", IsBang = true, IsSingle = true)]
        public ISpread<bool> FPublish;

        [Input("Favorite Tweets(Tweet Ids)")]
        public ISpread<string> FFavoriteIds;

        [Input("Favorite Tweets", IsBang = true, IsSingle = true)]
        public ISpread<bool> FFavorite;

        [Input("Retweet Tweets(Tweet Ids)")]
        public ISpread<string> FRetweetIds;

        [Input("Retweet Tweets", IsBang = true, IsSingle = true)]
        public ISpread<bool> FRetweet;

        [Input("Tweet Ids to destroy")]
        public ISpread<string> FDestroyIds;

        [Input("Destroy Tweets or Retweets", IsBang = true, IsSingle = true)]
        public ISpread<bool> FDestroy;

        [Input("Cancel action", IsBang = true, IsSingle = true)]
        public ISpread<bool> FCancel;

        [Output("Action Done Bang", IsBang = true, IsSingle = true)]
        public ISpread<bool> FActionDone;

        [Output("Published Tweet Ids")]
        public ISpread<string> FPublishedTweetId;        

        [Output("Action Status", IsSingle = true)]
        public ISpread<string> FActionStatus;

        [Output("All Actions Finished Bang", IsBang = true, IsSingle = true)]
        public ISpread<bool> FAllActionDone;

        [Import()]
        public ILogger FLogger;
        
        #endregion fields & pins

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }

        public System.Timers.Timer timo = new System.Timers.Timer(5000);
        public string lastActionStatus;
        public long tweetIdToReply;
        public long tweetIdToFav;
        public long tweetIdToRT;
        public long tweetIdToDestroy;
        public int conter = 0;
        public int conter2 = 0;
        public bool alreadySleeping;
        public bool badCoord;
        public int numOfTweetsPublished = 0;
        public Random rando = new Random();
        public Random randi = new Random();
        public CancellationTokenSource FavTaskToken;
        public CancellationTokenSource RTTaskToken;
        public CancellationTokenSource PublishTaskToken;
        public CancellationTokenSource DestroyTaskToken;
        public List<string> listTweetsPublished = new List<string>();
        public Task FavTask;
        public Task RTTask;
        public Task PublishTask; 
        public Task DestroyTask;
        public bool canceledTask;

        public bool HasImageExtension(string source)
        {
            return (source.EndsWith(".png") || source.EndsWith(".jpg") || source.EndsWith(".gif") || source.EndsWith(".bmp") || source.EndsWith(".jpeg"));
        }

        public void SleepOrNot(ISpread<string> spread, string tasko, int posInLoop)
        {
            if (FMachineGun[0]) {
                if (posInLoop < (spread.Count() - 1))
                {                
                    alreadySleeping = true;
                    MakeTaskWait(tasko);
                }                
            }
        }

        public void MakeTaskWait(string whichTask)
        {
            int smallInterval = rando.Next(0, (FMachineGunInterval[0]/5 * 1000));
            
            if(smallInterval<=200)
                smallInterval=200+rando.Next(0, 200);
            int intervalo = (FMachineGunInterval[0] * 1000) + smallInterval;
            double intervaloSecs = Convert.ToDouble(intervalo) / 1000;
            FLogger.Log(LogType.Debug, "sleeping for: {0} seconds ", intervaloSecs);

            if (whichTask == "Favourite")
            {
                FavTask.Wait(intervalo, FavTaskToken.Token);
            }
            else if (whichTask == "Publish")
            {
                PublishTask.Wait(intervalo, PublishTaskToken.Token);
            }
            else if (whichTask == "Retweet")
            {
                RTTask.Wait(intervalo, RTTaskToken.Token);
            }
            else
            {
                DestroyTask.Wait(intervalo, DestroyTaskToken.Token);
            }
        }

        public void GetPublishedResult(ITweet tweeto, string successMessage)
        {
            if (tweeto.IsTweetPublished)
            {
                FActionStatus[0] = tweeto.IdStr+" "+successMessage + " Fucking WICKED!";
                FLogger.Log(LogType.Debug, tweeto.IdStr + " " + successMessage + " Fucking WICKED!");
                listTweetsPublished.Add(tweeto.IdStr);
                numOfTweetsPublished=listTweetsPublished.Count;
                FPublishedTweetId.SliceCount = numOfTweetsPublished;
                FPublishedTweetId[numOfTweetsPublished - 1] = listTweetsPublished[listTweetsPublished.Count - 1];
                FActionDone[0] = true;
            }
            else
            {
                FActionStatus[0] = "FAILED! Tweet was not published, fuckkkkk!";
                FLogger.Log(LogType.Debug, "FAILED! Tweet was not published, fuckkkkk!");
            }
        }

        public void GetDestroyedResult(ITweet tweeto, long tweetId)
        {
            if (tweeto.IsTweetDestroyed)
            {
                FActionStatus[0] = "Tweet " + tweetId.ToString() + " has been destroyed, fucking smashing it big time.";
                FLogger.Log(LogType.Debug, "Tweet " + tweetId.ToString() + " has been destroyed, fucking smashing it big time.");               

                FActionDone[0] = true;
            }
            else
            {
                FActionStatus[0] = "FAILED! Couldn't kill that tweet.";
                FLogger.Log(LogType.Debug, "FAILED! Couldn't kill that tweet.");
            }
            listTweetsPublished.Remove(tweetId.ToString());
            numOfTweetsPublished = listTweetsPublished.Count;
            FPublishedTweetId.Remove(tweetId.ToString());
            
            FLogger.Log(LogType.Debug, "Tweet " + tweetId.ToString() + " has been removed from published list");
        }

        public void GetFavoritedResult(ITweet tweeto, long tweetId)
        {
            if (tweeto.Favourited)
            {
                FActionStatus[0] = ("Tweet "+ tweetId.ToString() +" favorited! Nice one pal.");
                FLogger.Log(LogType.Debug, ("Tweet " + tweetId.ToString() + " favorited! Nice one pal."));
                FActionDone[0]=true;
            }
            else
            {
                FActionStatus[0] = "FAILED! Nothing favorited bro.";
                FLogger.Log(LogType.Debug, "FAILED! Nothing favorited bro.");
            }
        }

        public bool TextEmpty(string text)
        {            
            if (!text.IsNullOrEmpty())
            {
                return false;
            }
            else
            {
                FActionStatus[0] = "Nope, Tweet has no text, dickhead.";
                FLogger.Log(LogType.Debug, "Nope, Tweet has no text, dickhead.");
                return true;
            }
        }

        public void Tweet_PublishTweet(string text)
        {
            if (!TextEmpty(text))
            {
                var newTweet = Tweet.CreateTweet(text);
                newTweet.Publish();
                GetPublishedResult(newTweet, "Tweet published!");
            }
            
        }

        public void Tweet_PublishTweetWithImage(string text, string filePath)
        {
            if (!TextEmpty(text))
            {
                byte[] file1 = File.ReadAllBytes(filePath);

                var tweet = Tweet.CreateTweetWithMedia(text, file1);

                // !! MOST ACCOUNTS ARE LIMITED TO 1 File per Tweet     !!
                // !! IF YOU ADD 2 MEDIA, YOU MAY HAVE ONLY 1 PUBLISHED !!
                // ADD: ", string filepath2 = null" as new ARG in the function definition
                /*if (filepath2 != null)
                {
                    byte[] file2 = File.ReadAllBytes(filepath2);
                    tweet.AddMedia(file2);
                }*/
                tweet.Publish();
                GetPublishedResult(tweet, "Tweet with image published!");
            }
        }

        public void Tweet_PublishTweetWithImageInReplyToAnotherTweet(string text, string filePath, long tweetIdtoRespondTo)
        {
            if (!TextEmpty(text))
            {
                byte[] file1 = File.ReadAllBytes(filePath);

                var substringed = "";
                try
                {
                    var tweeto = Tweet.GetTweet(tweetIdtoRespondTo);
                    string texto = "@" + tweeto.Creator.ScreenName + " " + text;
                    if (texto.Length > 140)
                    {
                        texto = texto.Substring(0, 136);
                        texto += "...";
                        substringed = " BUT tweet text was cropped because the reply Id was too long.";
                    }

                    var tweet = Tweet.CreateTweetWithMedia(texto, file1);
                    tweet.PublishInReplyTo(tweetIdtoRespondTo);
                    GetPublishedResult(tweet, "Tweet with image in reply to: " + tweetIdtoRespondTo.ToString() + " published!" + substringed);
                }
                catch
                {
                    FActionStatus[0] = "Tweet Id to reply to is not a published tweet!";
                    FLogger.Log(LogType.Debug, "Tweet Id to reply to is not a published tweet!");
                }

                
            }
        }

        public void Tweet_PublishTweetInReplyToAnotherTweet(string text, long tweetIdtoRespondTo)
        {
            if (!TextEmpty(text))
            {
                var substringed = "";
                try
                {
                    var tweeto = Tweet.GetTweet(tweetIdtoRespondTo);
                    string texto = "@" + tweeto.Creator.ScreenName + " " + text;
                    if (texto.Length > 140)
                    {
                        texto = texto.Substring(0, 136);
                        texto += "...";
                        substringed = " BUT tweet text was cropped because the reply Id was too long.";
                    }

                    var newTweet = Tweet.CreateTweet(texto);

                    newTweet.PublishInReplyTo(tweetIdtoRespondTo);
                    GetPublishedResult(newTweet, "Tweet in reply to " + tweetIdtoRespondTo.ToString() + " published!" + substringed);
                }
                catch
                {
                    FActionStatus[0] = "Tweet Id to reply to is not a published tweet!";
                    FLogger.Log(LogType.Debug, "Tweet Id to reply to is not a published tweet!");
                }                
            }
        }

        private void Tweet_PublishTweetWithGeo(string text, double longitude, double latitude)
        {
            if (!TextEmpty(text))
            {
                var newTweet = Tweet.CreateTweet(text);
                newTweet.PublishWithGeo(longitude, latitude);
                GetPublishedResult(newTweet, "Tweet published!(with fucking coordinates)");
            }
        }

        private void Tweet_PublishTweetWithGeoInReplyToAnotherTweet(string text, long tweetIdtoRespondTo, double longitude, double latitude)
        {
            if (!TextEmpty(text))
            {
                var substringed = "";
                try
                {
                    var tweeto = Tweet.GetTweet(tweetIdtoRespondTo);
                    string texto = "@" + tweeto.Creator.ScreenName + " " + text;
                    if (texto.Length > 140)
                    {
                        texto = texto.Substring(0, 136);
                        texto += "...";
                        substringed = " BUT tweet text was cropped because the reply Id was too long.";
                    }
                    var newTweet = Tweet.CreateTweet(texto);
                    newTweet.PublishWithGeoInReplyTo(longitude, latitude, tweetIdtoRespondTo);
                    GetPublishedResult(newTweet, "Tweet in reply to " + tweetIdtoRespondTo.ToString() + " published!(with fucking coordinates)" + substringed);
                }
                catch
                {
                    FActionStatus[0] = "Tweet Id to reply to is not a published tweet!";
                    FLogger.Log(LogType.Debug, "Tweet Id to reply to is not a published tweet!");
                }
            }
        }

        public void Tweet_PublishRetweet(long tweetId)
        {

            try
            {
                var tweet = Tweet.GetTweet(tweetId);
                if (!tweet.Retweeted)
                {
                    var retweet = tweet.PublishRetweet();
                    GetPublishedResult(retweet, "Retweeted " + tweetId.ToString());
                }
                else
                {
                    FActionStatus[0] = "Fuck that! Tweet " + tweetId + " already retweeted!";
                    FLogger.Log(LogType.Debug, "Fuck that! Tweet " + tweetId + " already retweeted!");
                }
            }
            catch
            {
                FActionStatus[0] = "Tweet doesn't exist!";
                FLogger.Log(LogType.Debug, "Tweet doesn't exist!");
            }     
            
        }

        public void Tweet_Destroy(long tweetId)
        {
            try
            {
                var tweet = Tweet.GetTweet(tweetId);
                if (tweet.IsTweetPublished)
                {
                    tweet.Destroy();
                    GetDestroyedResult(tweet, tweetId);
                }
                else
                {
                    FActionStatus[0] = "Tweet isn't published!";
                    FLogger.Log(LogType.Debug, "Tweet isn't published!");
                    listTweetsPublished.Remove(tweetId.ToString());
                    numOfTweetsPublished = listTweetsPublished.Count;
                    FPublishedTweetId.Remove(tweetId.ToString());
                    FLogger.Log(LogType.Debug, "Tweet " + tweetId.ToString() + " has been removed from published list");
                }
            }
            catch
            {
               if(FDestroyIds.SliceCount>0){
                   FActionStatus[0] = "Tweet doesn't exist!";
                   FLogger.Log(LogType.Debug, "Tweet doesn't exist!");
                   listTweetsPublished.Remove(tweetId.ToString());
                   numOfTweetsPublished = listTweetsPublished.Count;
                   FPublishedTweetId.Remove(tweetId.ToString());
                   FLogger.Log(LogType.Debug, "Tweet " + tweetId.ToString() + " has been removed from published list");

               }
                
            }
        }

        public void Tweet_SetTweetAsFavorite(long tweetId)
        {

            try
            {
                var tweet = Tweet.GetTweet(tweetId);
                if (!tweet.Favourited)
                {
                    tweet.Favourite();
                    GetFavoritedResult(tweet, tweetId);
                }
                else
                {
                    FActionStatus[0] = "Nah, Tweet " + tweetId + " already favourited!";
                    FLogger.Log(LogType.Debug, "Nah, Tweet " + tweetId + " already favourited!");
                }
            }
            catch
            {
                FActionStatus[0] = "Tweet doesn't exist!";
                FLogger.Log(LogType.Debug, "Tweet doesn't exist!");
            }





            
            
            
        }
        public void DoPublish()
        {
            canceledTask = false;
            bool badId = true;
            badCoord = true;
            alreadySleeping = false;
            if (FPublishInReplyIds.SliceCount > 1 || (FPublishInReplyIds.SliceCount == 1 && !FPublishInReplyIds[0].IsNullOrEmpty()))
            {
                for (int i = 0; i < FPublishInReplyIds.SliceCount; i++)
                {                    
                    if(!FPublishInReplyIds[i].IsNullOrEmpty())
                    {
                        try
                        {
                            tweetIdToReply = Convert.ToInt64(FPublishInReplyIds[i]);
                            badId = false;
                        }
                        catch
                        {
                            FActionStatus[0] = "Tweet Id to reply to not recognised, you bell-end";
                            FLogger.Log(LogType.Debug, "Tweet Id to reply to not recognised, you bell-end");
                            badId = true;
                        }
                        if (!badId)
                        {
                            if (FPublishPicture.SliceCount == 1 && !HasImageExtension(FPublishPicture[0]))
                            {
                                if (FRandom[0])
                                {
                                    var randomo = randi.Next(0, FPublishText.SliceCount);
                                    FLogger.Log(LogType.Debug, "random "+randomo);
                                    if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                                    {
                                        Tweet_PublishTweetWithGeoInReplyToAnotherTweet(FPublishText[randomo], tweetIdToReply, FPublishCoordinates[1], FPublishCoordinates[0]);                                                                                 
                                    }
                                    else
                                    {
                                        Tweet_PublishTweetInReplyToAnotherTweet(FPublishText[randomo], tweetIdToReply);
                                    }
                                    FLogger.Log(LogType.Debug, "sleeping or not");
                                    SleepOrNot(FPublishInReplyIds, "Publish", i);
                                    if (!FMachineGun[0] || canceledTask)
                                    {
                                        break;
                                    }
                                }
                                else
                                {                                
                                    for (int j = 0; j < FPublishText.SliceCount; j++)
                                    {
                                            if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                                            {                                          
                                                    Tweet_PublishTweetWithGeoInReplyToAnotherTweet(FPublishText[j], tweetIdToReply, FPublishCoordinates[1], FPublishCoordinates[0]);
                                            }
                                            else
                                            {
                                                Tweet_PublishTweetInReplyToAnotherTweet(FPublishText[j], tweetIdToReply);                                                
                                            }
                                            SleepOrNot(FPublishText, "Publish", j);
                                            if (!FMachineGun[0] || canceledTask)
                                            {
                                            break;
                                            }
                                      }
                                }
                            }
                            else
                            {
                                
                                if (FRandom[0])
                                {
                                        
                                        var randomo = randi.Next(0, FPublishText.SliceCount);                                        
                                        var randomPicNum = 0;
                                        if (randomo < FPublishPicture.SliceCount)
                                        {
                                            randomPicNum = randomo;
                                        }
                                        else
                                        {
                                            randomPicNum = randomo % FPublishPicture.SliceCount;
                                        }
                                        if (HasImageExtension(FPublishPicture[randomPicNum]))
                                        {
                                            
                                            Tweet_PublishTweetWithImageInReplyToAnotherTweet(FPublishText[randomo], FPublishPicture[randomPicNum], tweetIdToReply);                                            
                                        }
                                        else
                                        {
                                            FActionStatus[0] = "Slice " + randomPicNum + " in your image spread is not a valid image, sending tweeting without image...";
                                            FLogger.Log(LogType.Debug, "Slice " + randomPicNum + " in your image spread is not a valid image, sending tweeting without image...");
                                            if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                                            {
                                                Tweet_PublishTweetWithGeoInReplyToAnotherTweet(FPublishText[randomo], tweetIdToReply, FPublishCoordinates[1], FPublishCoordinates[0]);
                                            }
                                            else
                                            {
                                                Tweet_PublishTweetInReplyToAnotherTweet(FPublishText[randomo], tweetIdToReply);
                                            }
                                        }
                                        SleepOrNot(FPublishInReplyIds, "Publish", i);
                                        if (!FMachineGun[0] || canceledTask)
                                        {
                                            break;
                                        }                                   
                                }
                                else
                                {
                                    for (int j = 0; j < FPublishText.SliceCount; j++)
                                    {
                                        var k = 0;
                                        if (j < FPublishPicture.SliceCount)
                                        {
                                            k = j;
                                        }
                                        else
                                        {
                                            k = j % FPublishPicture.SliceCount;
                                        }
                                        if (HasImageExtension(FPublishPicture[k]))
                                        {
                                            Tweet_PublishTweetWithImageInReplyToAnotherTweet(FPublishText[j], FPublishPicture[k], tweetIdToReply);
                                            
                                        }
                                        else
                                        {
                                            FActionStatus[0] = "Slice " + j + " in your image spread is not a valid image, sending tweeting without image...";
                                            FLogger.Log(LogType.Debug, "Slice " + j + " in your image spread is not a valid image, sending tweeting without image...");
                                            if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                                            {
                                                Tweet_PublishTweetWithGeoInReplyToAnotherTweet(FPublishText[j], tweetIdToReply, FPublishCoordinates[1], FPublishCoordinates[0]);                                                
                                            }
                                            else
                                            {
                                                Tweet_PublishTweetInReplyToAnotherTweet(FPublishText[j], tweetIdToReply);
                                                
                                            }
                                        }
                                        SleepOrNot(FPublishText, "Publish", j);
                                        if (!FMachineGun[0] || canceledTask)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        
                }
                else
                {
                    FActionStatus[0] = "Tweet Id to reply to is null or empty, you twat";
                    FLogger.Log(LogType.Debug, "Tweet Id to reply to is null or empty, you twat");
                }
                if (!alreadySleeping)
                {
                    //csshnage here? to SleepOrNot(FPublishText, "Publish", j);
                    MakeTaskWait("Publish");
                }
                if (!FMachineGun[0] || canceledTask)
                {
                    break;
                }
                }                
            }
            else
            {
                if (FPublishPicture.SliceCount == 1 && !HasImageExtension(FPublishPicture[0]))
                {
                    for (int j = 0; j < FPublishText.SliceCount; j++)
                    {
                        if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                        {
                            Tweet_PublishTweetWithGeo(FPublishText[j], FPublishCoordinates[1], FPublishCoordinates[0]);                           
                        }
                        else
                        {
                            Tweet_PublishTweet(FPublishText[j]);
                        }
                        SleepOrNot(FPublishText, "Publish", j);
                        if(!FMachineGun[0]){
                        break;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < FPublishText.SliceCount; j++)
                    {
                        var k = 0;
                        if (j < FPublishPicture.SliceCount)
                        {
                            k = j;
                            
                        }
                        else
                        {
                            k = j % FPublishPicture.SliceCount;
                        }
                        if (HasImageExtension(FPublishPicture[k]))
                        {
                            Tweet_PublishTweetWithImage(FPublishText[j], FPublishPicture[k]);                           
                        }
                        else
                        {
                            FActionStatus[0] = "Slice " + j + " in your image spread is not a valid image, sending tweeting without image...";
                            FLogger.Log(LogType.Debug, "Slice " + j + " in your image spread is not a valid image, sending tweeting without image...");
                                            
                            if (FPublishCoordinates.SliceCount == 2 && !FPublishCoordinates.Contains(0))
                            {
                                Tweet_PublishTweetWithGeo(FPublishText[j], FPublishCoordinates[1], FPublishCoordinates[0]);                                
                            }
                            else
                            {
                                Tweet_PublishTweet(FPublishText[j]);
                            }
                        }
                        SleepOrNot(FPublishText, "Publish", j);
                        if (!FMachineGun[0] || canceledTask)
                        {
                            break;
                        }
                    }                  
                }
            }
            FAllActionDone[0] = true;
            //FActionStatus[0] = "FINISHED! Whatever you did, you did well. I believe in you mate.";
            FLogger.Log(LogType.Debug, "FINISHED! Whatever you did, you did well. I believe in you mate.");
        }
        public void DoFavorite()
        {
            canceledTask = false;
            bool badId = true;
            for (int i = 0; i < FFavoriteIds.SliceCount; i++)
            {
                try
                {
                    tweetIdToFav = Convert.ToInt64(FFavoriteIds[i]);
                    badId = false;
                }
                catch
                {
                    FActionStatus[0] = "Tweet Id to favorite not recognised, dickhead.";
                    FLogger.Log(LogType.Debug, "Tweet Id to favorite not recognised, dickhead.");
                    badId = true;
                }
                if (!badId)
                {
                    Tweet_SetTweetAsFavorite(tweetIdToFav);
                    SleepOrNot(FFavoriteIds, "Favourite", i);

                }
                if (!FMachineGun[0] || canceledTask)
                {
                    break;
                }
            }
            FAllActionDone[0] = true;
            FLogger.Log(LogType.Debug, "FINISHED! Whatever you did, you did well. I believe in you mate.");
        }
        public void DoRetweet()
        {
            canceledTask = false;
            bool badId = true;
            for (int i = 0; i < FRetweetIds.SliceCount; i++)
            {
                try
                {
                    tweetIdToRT = Convert.ToInt64(FRetweetIds[i]);
                    badId = false;
                }
                catch
                {
                    FActionStatus[0] = "Tweet Id to retweet not recognised, you spineless turd";
                    FLogger.Log(LogType.Debug, "Tweet Id to retweet not recognised, you spineless turd");
                    badId = true;
                }
                if (!badId)
                {
                    Tweet_PublishRetweet(tweetIdToRT);

                    SleepOrNot(FRetweetIds, "Retweet", i);
                }
                if (!FMachineGun[0] || canceledTask)
                {
                    break;
                }
            }
            FAllActionDone[0] = true;
            FLogger.Log(LogType.Debug, "FINISHED! Whatever you did, you did well. I believe in you mate.");
        }
        public void DoDestroy()
        {
            if (!FDestroyIds.IsNullOrEmpty())
            {           
                canceledTask = false;
                bool badId = true;
                for (int i = 0; i < FDestroyIds.SliceCount; i=i)
                {
                    try
                    {
                        tweetIdToDestroy = Convert.ToInt64(FDestroyIds[i]);
                        badId = false;
                    }
                    catch
                    {
                        FActionStatus[0] = "Tweet Id to destroy not recognised, you spineless turd";
                        FLogger.Log(LogType.Debug, "Tweet Id to destroy not recognised, you spineless turd");
                        badId = true;
                        
                    }
                    if (!badId)
                    {
                        Tweet_Destroy(tweetIdToDestroy);
                        if (FDestroyIds.SliceCount>1)
                        {
                            MakeTaskWait("Destroy");
                        }

                    }
                    else
                    {
                        FLogger.Log(LogType.Debug, "...still removing it from the published list ");
                        listTweetsPublished.RemoveAt(i);                        
                        numOfTweetsPublished = listTweetsPublished.Count;
                        FPublishedTweetId.RemoveAt(i);
                    }

                    if (!FMachineGun[0] || canceledTask)
                    {
                        break;
                    }
                }
            FAllActionDone[0] = true;
            FLogger.Log(LogType.Debug, "FINISHED! Whatever you did, you did well. I believe in you mate.");
            }
            else
            {
                FActionStatus[0] = "Tweet Id to destroy null or empty, dickhead.";
                FLogger.Log(LogType.Debug, "Tweet Id to destroy null or empty, dickhead.");
            }
        }
       
        #region Evaluate Function
        public void Evaluate(int SpreadMax)
		{
            if(FActionDone[0]){
                conter++;
                if (conter == 2)
                {
                    FActionDone[0] = false;
                    conter = 0;                    
                }
            }
            if (FAllActionDone[0])
            {
                conter2++;
                if (conter2 == 2)
                {
                    FAllActionDone[0] = false;
                    conter2 = 0;
                }
            }
            if (!FLoginBool[0])
            {
                numOfTweetsPublished = 0;
                listTweetsPublished.Clear();
                FPublishedTweetId.SliceCount = 1;
                FPublishedTweetId[0] = "";

            }
            if (FPublish[0])
            {
                if (FLoginBool[0])
                {   
                    if (FPublishText[0].Length != 0)
                    {

                        if ((PublishTask != null && PublishTask.Status == TaskStatus.Running) || (DestroyTask != null && DestroyTask.Status == TaskStatus.Running) || (RTTask != null && RTTask.Status == TaskStatus.Running) || (FavTask != null && FavTask.Status == TaskStatus.Running))
                        {

                            FActionStatus[0] = "Action already running bro";
                            FLogger.Log(LogType.Debug, "Action already running bro");
                        }
                        else
                        {
                            PublishTaskToken = new CancellationTokenSource();
                            PublishTask = new Task(DoPublish, PublishTaskToken.Token);
                            PublishTask.Start();
                        }
                   
                    }
                    else
                    {
                        FActionStatus[0] = "Tweet text is blank you tosser";
                        FLogger.Log(LogType.Debug, "Tweet text is blank you tosser");
                    }    
                    
                }
                else
                {
                    FActionStatus[0] = "Login first you douchebag";
                    FLogger.Log(LogType.Debug, "Login first you douchebag");
                }
            }

            if (FFavorite[0])
            {
                if (FLoginBool[0])
                {
                    if ((PublishTask != null && PublishTask.Status == TaskStatus.Running) || (DestroyTask != null && DestroyTask.Status == TaskStatus.Running) || (RTTask != null && RTTask.Status == TaskStatus.Running) || (FavTask != null && FavTask.Status == TaskStatus.Running))
                    {
                        FActionStatus[0] = "Action already running bro";
                        FLogger.Log(LogType.Debug, "Action already running bro");
                    }
                    else
                    {
                        FavTaskToken = new CancellationTokenSource();
                        FavTask = new Task(DoFavorite, FavTaskToken.Token);
                        FavTask.Start();
                    }
                    
                }
                else
                {
                    FActionStatus[0] = "Login first you douchebag";
                    FLogger.Log(LogType.Debug, "Login first you douchebag");
                }
            }

            if (FRetweet[0])
            {
                if (FLoginBool[0])
                {
                    if ((PublishTask != null && PublishTask.Status == TaskStatus.Running) || (DestroyTask != null && DestroyTask.Status == TaskStatus.Running) || (RTTask != null && RTTask.Status == TaskStatus.Running) || (FavTask != null && FavTask.Status == TaskStatus.Running))
                    {
                        FActionStatus[0] = "Action already running bro";
                        FLogger.Log(LogType.Debug, "Action already running bro");                        
                    }
                    else
                    {
                        RTTaskToken = new CancellationTokenSource();
                        RTTask = new Task(DoRetweet, RTTaskToken.Token);
                        RTTask.Start();
                    }
                }
                else
                {
                    FActionStatus[0] = "Login first you douchebag";
                    FLogger.Log(LogType.Debug, "Login first you douchebag");
                }
            }

            if (FDestroy[0])
            {
                if (FLoginBool[0])
                {
                    if ((PublishTask != null && PublishTask.Status == TaskStatus.Running) || (DestroyTask != null && DestroyTask.Status == TaskStatus.Running) || (RTTask != null && RTTask.Status == TaskStatus.Running) || (FavTask != null && FavTask.Status == TaskStatus.Running))
                    {
                        FActionStatus[0] = "Action already running bro";
                        FLogger.Log(LogType.Debug, "Action already running bro");
                    }
                    else
                    {
                        DestroyTaskToken = new CancellationTokenSource();
                        DestroyTask = new Task(DoDestroy, DestroyTaskToken.Token);
                        DestroyTask.Start();
                    }
                }
                else
                {
                    FActionStatus[0] = "Login first you douchebag";
                    FLogger.Log(LogType.Debug, "Login first you douchebag");
                }
            }

            if (FCancel[0])
            {
                bool boolo = false;
                if (FavTask != null && FavTask.Status == TaskStatus.Running)
                {
                    FavTaskToken.Cancel();
                    canceledTask = true;
                    boolo = true;
                }
                if (RTTask != null && RTTask.Status == TaskStatus.Running)
                {
                    RTTaskToken.Cancel();
                    canceledTask = true;
                    boolo = true;
                }
                if (PublishTask != null && PublishTask.Status == TaskStatus.Running)
                {
                    PublishTaskToken.Cancel();
                    canceledTask = true;
                    boolo = true;
                }
                if (DestroyTask != null && DestroyTask.Status == TaskStatus.Running)
                {
                    DestroyTaskToken.Cancel();
                    canceledTask = true;
                    boolo = true;
                }
                if (!boolo)
                {
                    FActionStatus[0] = "No action currently running, fuck face.";
                    FLogger.Log(LogType.Debug, "No action currently running, fuck face.");
                }else{
                    FActionStatus[0] = "CANCELED! You are SO in control bro";
                    FLogger.Log(LogType.Debug, "CANCELED! You are SO in control bro");
                }
            }
         }
        #endregion Evaluate Function
        
    }
   
    }
