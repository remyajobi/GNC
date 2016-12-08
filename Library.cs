using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceModel.Syndication;
using System.Xml;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Newtonsoft.Json;
using System.Xml.XPath;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xaml;
using System.Security;
using edu.stanford.nlp.parser;
using java.util;
using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace GNCService
{
    class Library
    {
        
        enum Beats { Business, Entertainment, Sports, Technology, Science, Health, Spotlight, Elections, World, Local };
      
        public static string googleNewsApiUrl = "https://news.google.com/news?pz=1&cf=all&ned=us&hl=en&cf=all&as_drrb=b:as_mind:D2&scoring=d&num=50&output=rss";
        public static List<string> newsTopics = new List<string>();
        public static DataAccess dataAccess = new DataAccess();
        public static int crawlingCount = 1;
        public static int recordCount = 0;
        public static int dailyRecordCount = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string LoadProxies()
        {
            List<string> ProxiesList = new List<string>();
            ProxiesList.Clear();

            ProxiesList.Add("108.62.102.31:3128");
            ProxiesList.Add("108.62.124.56:3128");
            ProxiesList.Add("192.126.167.213:3128");
            ProxiesList.Add("192.126.167.201:3128");
            ProxiesList.Add("192.126.168.64:3128");
            ProxiesList.Add("192.126.167.254:3128");
            ProxiesList.Add("192.126.167.23:3128");
            ProxiesList.Add("192.126.167.237:3128");
            ProxiesList.Add("108.62.102.19:3128");
            ProxiesList.Add("192.126.167.62:3128");
            ProxiesList.Add("108.62.124.75:3128");
            ProxiesList.Add("108.62.124.62:3128");
            ProxiesList.Add("108.62.102.111:3128");
            ProxiesList.Add("192.126.168.56:3128");
            ProxiesList.Add("192.126.168.52:3128");
            ProxiesList.Add("108.62.102.204:3128");
            ProxiesList.Add("108.62.124.126:3128");
            ProxiesList.Add("108.62.102.231:3128");
            ProxiesList.Add("108.62.102.105:3128");
            ProxiesList.Add("108.62.124.144:3128");
            ProxiesList.Add("192.126.168.253:3128");
            ProxiesList.Add("192.126.168.246:3128");
            ProxiesList.Add("108.62.124.202:3128");
            ProxiesList.Add("192.126.168.43:3128");
            ProxiesList.Add("108.62.102.77:3128");
           
            System.Random random = new System.Random();
            string proxyAddress = ProxiesList[random.Next(ProxiesList.Count)];

            return proxyAddress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static void ProxySettings(WebClient Client)
        {
            WebProxy Proxy = null;

            try
            {
                var proxyAddress = LoadProxies();

                if (!(string.IsNullOrEmpty(proxyAddress)))
                {
                    Proxy = new WebProxy(proxyAddress);
                    Client.Proxy = Proxy;

                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Proxy Error. " + ex.InnerException.ToString());
            }

        }

        public static WebClient HtmlAgilityPackProxySettings()
        {
            WebClient webClient = new WebClient();

            var proxyAddress = LoadProxies();
            if (!(string.IsNullOrEmpty(proxyAddress)))
            {
                webClient.Proxy = new WebProxy(proxyAddress);
            }
           
            

            return webClient;
        }

        /// <summary>
        /// Crawl the news
        /// </summary>
        public static void Crawler()
        {
            CrawlerEventInfoLog("Crawling started. ");

            DataSet rssFeedDataSet = new DataSet();
            rssFeedDataSet.Tables.Clear();

            GetNewsTopics();
            
            Task[] rssFeedTasks = newsTopics
                .Select(url => Task.Factory.StartNew(
                    state =>
                    {
                      
                        using (var client = new WebClient())
                        {
                            ProxySettings(client);
                            var newsUrl = (string)state;
                                      
                            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                            Stream rssOutput = client.OpenRead(newsUrl);
                            StreamReader streamReader = new StreamReader(rssOutput);
                            string htmlString = streamReader.ReadToEnd();
                            rssOutput.Close();
                            streamReader.Close();
                            return htmlString;
                        }
                    }, url).ContinueWith(x =>
                    {
                        StringReader stringReader = new StringReader(x.Result);

                        rssFeedDataSet.ReadXml(stringReader);

                    }

                )).ToArray();
         
            //......................................................................................//
            try
            {
                Task.WaitAll(rssFeedTasks);
            }

            catch (AggregateException ae)
            {
                Exception e = ae.Flatten().InnerException;
                CrawlerEventErrorLog("RSS Feed task failed. " + e);
                goto ContinueRssFeedTask;

              
            }
           
            //......................................................................................//
        ContinueRssFeedTask: 

            DataTable dtGetNews = new DataTable();
            dtGetNews.Clear();
            DataTable dtGuid = new DataTable();
            dtGuid.Clear();
            DataTable dtCrawler = new DataTable();
            dtCrawler.Clear();
            DataTable dtSubBeats = new DataTable();
            dtSubBeats.Clear();

            if (rssFeedDataSet.Tables.Count > 3)
            {
                dtGuid = rssFeedDataSet.Tables["guid"];
                dtGetNews = rssFeedDataSet.Tables["item"];
                dtCrawler = GetTable();
                dtSubBeats = GetSubBeatTable();
                DataRow drCrawler;
                DataRow drSubBeats;

                //var coreTaskCancellationTokenSource = new CancellationTokenSource();
                //var coreTaskToken = coreTaskCancellationTokenSource.Token;
               
                Task[] coreTasks = new Task[dtGetNews.Rows.Count];
                //TaskFactory factory = new TaskFactory(coreTaskToken);
               
                //factory.ContinueWhenAny(coreTasks, (t) =>
                //{
                    
                //}, TaskContinuationOptions.OnlyOnRanToCompletion);
               
                try
                {
                    Parallel.For(0, dtGetNews.Rows.Count, row =>
                    {
                        coreTasks[row] =Task.Factory.StartNew(() =>
                        {
                            string PublisherUrl = dtGetNews.Rows[row]["link"].ToString();
                            var titleAndPub = dtGetNews.Rows[row]["title"].ToString();
                            string[] titleAndPubArray = titleAndPub.Split('-');
                            string title = titleAndPubArray[0];
                            string Publisher = titleAndPub.Split('-').Last();

                            //Get author of the article.
                            Parallel.Invoke(() =>
                            {
                                var JournalistInfo = FindJournalistInfo(PublisherUrl);

                                if (JournalistInfo.Count() != 0)
                                {

                                    drCrawler = dtCrawler.NewRow();
                                    drSubBeats = dtSubBeats.NewRow();
                                    try
                                    {
                                        Parallel.ForEach(JournalistInfo,
                                         item =>
                                         {

                                             if (item.Contains("email:"))
                                             {

                                                 item = item.Replace("email:", "");
                                                 drCrawler["Email"] = item;

                                             }

                                             if (item.Contains("name:"))
                                             {
                                                 item = item.Replace("name:", "");
                                                 drCrawler["name"] = item;
                                             }

                                             if (item.Contains("Loc:"))
                                             {
                                                 item = item.Replace("Loc:", "");
                                                 if (!string.IsNullOrEmpty(item))
                                                 {
                                                     string[] loc = item.Split(',').ToArray();

                                                     if (loc.Count() > 1)
                                                     {
                                                         drCrawler["state"] = loc[0];
                                                         drCrawler["country"] = loc[1];
                                                     }
                                                     else
                                                     {
                                                         drCrawler["state"] = loc[0];
                                                     }
                                                 }
                                             }

                                             if (item.Contains("Media link:"))
                                             {
                                                 item = item.Replace("Media link:", "");
                                                 drCrawler["MediaLink"] = item;
                                             }
                                             if (item.Contains("LinkedIn:"))
                                             {
                                                 item = item.Replace("LinkedIn:", "");
                                                 drCrawler["LinkedInUrl"] = item;
                                             }

                                             if (item.Contains("website:"))
                                             {
                                                 item = item.Replace("website:", "");
                                                 drCrawler["Website"] = item;
                                             }

                                             if (item.Contains("bio:"))
                                             {
                                                 item = item.Replace("bio:", "");
                                                 drCrawler["AboutInfo"] = UnescapeHTMLValue(item);
                                             }
                                           
                                             //Set default value to the journalistid.
                                             drCrawler["JournalistId"] = 0;
                                             drCrawler["Beat"] = dtGetNews.Rows[row]["category"];

                                             drCrawler["Association"] = Publisher;
                                             drCrawler["PublicationDate"] = dtGetNews.Rows[row]["pubDate"];
                                             drCrawler["ArticleTitle"] = UnescapeHTMLValue(dtGetNews.Rows[row]["title"].ToString());
                                             drCrawler["ArticleLink"] = PublisherUrl;

                                            // drSubBeats["BeatId"] = GetBeatId(dtGetNews.Rows[row]["category"].ToString().Trim());
                                             drSubBeats["newsId"] = Convert.ToInt32(dtGetNews.Rows[row]["item_Id"]);
                                             drSubBeats["channelId"] = Convert.ToInt32(dtGetNews.Rows[row]["channel_Id"]);

                                             try
                                             {

                                                 drSubBeats["guidText"] = (from DataRow dr in dtGuid.Rows
                                                                           where (int)dr["item_Id"] == Convert.ToInt32(drSubBeats["newsId"])
                                                                           select (string)dr["guid_Text"]).FirstOrDefault();


                                                 drSubBeats["BeatLink"] = (from DataRow dr in rssFeedDataSet.Tables["channel"].Rows
                                                                           where (int)dr["channel_Id"] == Convert.ToInt32(drSubBeats["channelId"])
                                                                           select (string)dr["link"]).FirstOrDefault();


                                             }
                                             catch (Exception ex)
                                             {
                                                 CrawlerEventErrorLog("Core task failed while getting sub beats. " + ex.InnerException.ToString());
                                             }

                                         });
                                    }
                                    catch (Exception ex)
                                    {
                                        CrawlerEventErrorLog("Core task failed while processing journalist info. " + ex.InnerException.ToString());
                                    }

                                    try
                                    {
                                        dtCrawler.Rows.Add(drCrawler);
                                        dtSubBeats.Rows.Add(drSubBeats);

                                        recordCount = dtCrawler.Rows.Count;
                                    }
                                    catch (Exception ex)
                                    {
                                        CrawlerEventErrorLog("Core task failed while sub beats to data set. " + ex.InnerException.ToString());
                                    }
                                }

                            },// close first Action

                                  () =>
                                  {
                                      //Reserved for implementing any other parallel 'method'
                                  }//close second Action

                                );
                        });
                      
                    });
                   
                    //// Wait for fastest task to complete.
                    //Task.WaitAny(coreTasks);

                }
                catch (Exception ex)
                {
                    CrawlerEventErrorLog("Core task failed while parsing news content. " + ex.InnerException.ToString());
                }

                //......................................................................................//
                try
                {
                    Task.WaitAll(coreTasks);
    
                }
                catch (AggregateException ae)
                 {
                    
                    Exception ex = ae.Flatten().InnerException;
                    goto ContinueCoreTask;
                    
                }
                

              //......................................................................................//

               ContinueCoreTask:
                try
                {
                    CrawlerEventInfoLog("core task completed." + dtCrawler.Rows.Count);
                    SaveAll(dtCrawler, dtSubBeats);
                    //Task saveTask = Task.Run(() => SaveAll(dtCrawler, dtSubBeats));
                    //saveTask.Wait();
                }

                catch (Exception ex)
                {
                    CrawlerEventErrorLog("Core task failed while save journalist info. " + ex.InnerException.ToString());
                }
            }
        }

        public static void SaveAll(DataTable dtCrawler, DataTable dtSubBeats)
        {
            try
            {
                FillBeatIds(dtCrawler,dtSubBeats);
                var JournalistRecord = CreateJournalistRecord(dtCrawler);
                TrimData(JournalistRecord);

                //.............Save 'journalist' records if doesn't exists....................//
                var ResultingJournalistRecord = ProcessingForJournalist(JournalistRecord);
                if (ResultingJournalistRecord.Tables["Sorted Table"].Rows.Count > 0)
                {
                    var ArticleRec = CreateArticleRecords(dtCrawler, ResultingJournalistRecord);
                   
                    if (ArticleRec != null)
                    {
                        TrimData(ArticleRec);

                        //.............Save 'article' records...................//
                        if (SaveArticles(ArticleRec))
                        {
                            var ArticleIds = GetArticleTopIds();
                            ArticleIds.Sort();

                            FillSubBeat(dtSubBeats);
                            TrimData(dtSubBeats);

                            var Records = SaveSubBeat(dtSubBeats);

                            if (Records.Rows.Count > 0)
                            {
                                FillArticleId(Records, ArticleIds);
                                //.............Save 'article-Subbeats' records...................//
                                var ArticleSubbeatRec = CreateArticleSubbeatRecords(Records);
                                TrimData(ArticleSubbeatRec);

                                if (SaveArticleSubBeats(ArticleSubbeatRec))
                                {
                                    CrawlerEventInfoLog("Crawling " + crawlingCount + " completed on " + DateTime.Now.ToString("dd'/'MM'/'yyyy HH:mm:ss") + "; " + recordCount + " records crawled.");
                                    crawlingCount++;
                                }
                            }


                        }
                        else
                        {
                           //skip
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Error in saving all records. " + ex.InnerException.ToString());
            }
        }

        public static void FillBeatIds(DataTable dtCrawler, DataTable dtSubBeats)
        {
            if (dtSubBeats.Rows.Count > 0)
            {
                for (int row = 0; row < dtSubBeats.Rows.Count; row++)
                {
                    dtSubBeats.Rows[row]["BeatId"] = GetBeatId(dtCrawler.Rows[row]["Beat"].ToString().Trim());

                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void CrawlerEventInfoLog(string message)
        {
            EventLog eventLog = new EventLog("");
            eventLog.Source = "GNC Service";
            eventLog.WriteEntry(message, EventLogEntryType.Information);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void CrawlerEventErrorLog(string message)
        {
            EventLog eventLog = new EventLog("");
            eventLog.Source = "GNC Service";
            eventLog.WriteEntry(message, EventLogEntryType.Error);
        }

        /// <summary>
        /// Get news topics to crawl
        /// </summary>
        /// <returns></returns>
        public static List<string> GetNewsTopics()
        {
            DataSet dsBeats=new DataSet();
            dsBeats.Clear();
            newsTopics.Clear();
            try
            {
                dsBeats = GetBeats();
               
                if (dsBeats.Tables[0].Rows.Count > 0)
                {

                    foreach (DataRow Row in dsBeats.Tables[0].Rows)
                    {
                       string beat=Row["BeatTopic"].ToString();

                       switch (beat)
                        {
                            case "Business": newsTopics.Add(googleNewsApiUrl + "&topic=b");
                                break;
                            case "Entertainment": newsTopics.Add(googleNewsApiUrl + "&topic=e");
                                break;
                            case "Sports": newsTopics.Add(googleNewsApiUrl + "&topic=s");
                                break;
                            case "Technology": newsTopics.Add(googleNewsApiUrl + "&topic=tc");
                                break;
                            case "Science": newsTopics.Add(googleNewsApiUrl + "&topic=snc");
                                break;
                            case "Health": newsTopics.Add(googleNewsApiUrl + "&topic=m");
                                break;
                            case "Spotlight": newsTopics.Add(googleNewsApiUrl + "&topic=ir");
                                break;
                            case "Elections": newsTopics.Add(googleNewsApiUrl + "&topic=el");
                                break;
                            case "World": newsTopics.Add(googleNewsApiUrl + "&topic=w");
                                break;
                            case "Local": newsTopics.Add(googleNewsApiUrl + "&geo=detect_metro_area");
                                break;
                            default:
                                break;
                        }

                    }
                }
              
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Core task failed while getting news topics. " + ex.InnerException.ToString());
            }

            return newsTopics;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PublisherUrl"></param>

        public static List<string> FindJournalistInfo(string PublisherUrl)
        {
            List<string> JournalistProfile = new List<string>();
            JournalistProfile.Clear();
            bool b = false;

            var PublisherPage = HtmlAgilityPackProxySettings().DownloadString(PublisherUrl);
            HtmlAgilityPack.HtmlDocument document1 = new HtmlAgilityPack.HtmlDocument();
            document1.LoadHtml(PublisherPage);

            //HtmlWeb web = new HtmlWeb();
            //var document1 = web.Load(PublisherUrl);

            try
            {
                JournalistProfile=FetchNameFromHtmlNode(document1);
                
                HtmlNode AnchorTag1 = document1.DocumentNode.SelectSingleNode("//a[@rel='author']");//Author Link
                HtmlNode AnchorTag2 = document1.DocumentNode.SelectSingleNode("//a[@class='author-twitter']");//Twitter Link
                HtmlNode AnchorTag3 = document1.DocumentNode.SelectSingleNode("//a[@class='author-linkedin']");//LinkedIn Link
               
                if (AnchorTag1 != null)
                {

                    //Case 1: Author bio icon directly append to author name.
                    //eg:http://www.wsj.com/articles/salesforce-won-t-pursue-bid-for-twitter-1476468050
                    //List of author bio icons.

                    HtmlNode ListIconNode = document1.DocumentNode.SelectSingleNode("//ul[@class='author-info']");

                    if (ListIconNode != null)
                    {
                        HtmlNode EmailIcon = ListIconNode.SelectSingleNode("//a[@class='author icon email']");//Email Icon
                        if (EmailIcon != null)
                        {
                            var email = FetchEmaiId(EmailIcon.Attributes["href"].Value);
                            b = true;
                            var name = AnchorTag1.InnerText;

                            if (email != null)
                            { JournalistProfile.Add("email:" + email); }
                            else
                            { JournalistProfile.Add("email:" + null); }

                            //if (name != null)
                            //{ JournalistProfile.Add("name:" + name); }
                            //else
                            //{ JournalistProfile.Add("name:" + null); }
                        }
                      
                        HtmlNode TwitterIcon = ListIconNode.SelectSingleNode("//a[@class='author icon twitter']");//Twitter Icon

                        if (TwitterIcon != null)
                        {
                            var TwitterUrl = TwitterIcon.Attributes["href"].Value;
                            JournalistProfile.Add("Media link:" + TwitterUrl);
                            JournalistProfile.AddRange(GetTwitterProfile(TwitterUrl));
                        }
                        else { JournalistProfile.Add("Media link:" + null); }

                        HtmlNode LinkedInIcon = ListIconNode.SelectSingleNode("//a[@class='author icon linkedin']");//LinkedIn Icon

                        if (LinkedInIcon != null)
                        {
                            var linkedInUrl = LinkedInIcon.Attributes["href"].Value;
                            JournalistProfile.Add("LinkedIn:" + linkedInUrl);
                        }
                        else
                        { JournalistProfile.Add("LinkedIn:" + null); }

                    }

                    if (b == false)
                    {
                        //Case 2:Athor bio icon Info fetch from author bio infourl.
                        //eg:
                        //////////////////////////////////////////////////////////////
                        string authorBioInfoUrl = string.Empty;
                        if (AnchorTag1.Attributes["href"].Value.StartsWith("http://"))
                        {
                            authorBioInfoUrl = AnchorTag1.Attributes["href"].Value;
                            JournalistProfile.Add("website:" + authorBioInfoUrl);
                        }
                        else
                        {
                            //TODO:
                            //  string pubUrl =PublisherUrl.Split
                            // Uri uri = new Uri(pubUrl);
                            //  String hostname = uri.Host;

                            // authorBioInfoUrl=hostname+AnchorTag1.Attributes["href"].Value;
                        }


                        if (authorBioInfoUrl != null && authorBioInfoUrl != string.Empty)
                        {
                            var authorBioInfoPage = HtmlAgilityPackProxySettings().DownloadString(authorBioInfoUrl);
                            HtmlAgilityPack.HtmlDocument document2 = new HtmlAgilityPack.HtmlDocument();
                            document2.LoadHtml(authorBioInfoPage);

                            //HtmlWeb web1 = new HtmlWeb();
                            //var document2 = web1.Load(authorBioInfoUrl);
                           
                            HtmlNodeCollection ListNodeCollection = document2.DocumentNode.SelectNodes("//li[@class='social-icon-list-item']");

                            if (ListNodeCollection != null)
                            {
                                foreach (var ListNode in ListNodeCollection)
                                {
                                    HtmlNodeCollection SocialIconItems = ListNode.ChildNodes;

                                    foreach (var IconItem in SocialIconItems)
                                    {
                                        if (IconItem.Name == "a")
                                        {
                                            var ContactItem = IconItem.Attributes["href"].Value;

                                            if (ContactItem.Contains("twitter.com"))//Twitter Contact
                                            {
                                                var Twitter = ContactItem;
                                                if (Twitter != null)
                                                {
                                                    JournalistProfile.AddRange(GetTwitterProfile(Twitter));
                                                    JournalistProfile.Add("Media link:" + Twitter);
                                                }
                                                else
                                                { JournalistProfile.Add("Media link:" + null); }
                                            }
                                           
                                            if (ContactItem.Contains("linkedin.com"))//linkedIn Contact
                                            {
                                                var LinkedIn = ContactItem;
                                                if (LinkedIn != null)
                                                {
                                                    JournalistProfile.Add("LinkedIn:" + LinkedIn);
                                                }
                                                else
                                                { JournalistProfile.Add("LinkedIn:" + null); }
                                            }

                                            if (ContactItem.Contains("mailto:"))//Email Contact
                                            {
                                                var email = FetchEmaiId(ContactItem);

                                                if (email != null)
                                                {
                                                    JournalistProfile.Add("email:" + email);

                                                }
                                                else
                                                {
                                                    JournalistProfile.Add("email:" + null);
                                                }
                                            }

                                        }

                                    }

                                }
                            }

                        }

                    }
                }

                else
                {
                    //Case 3://a[@rel='author'] doesn't contain href;
                    //List of author bio icons.
                    HtmlNode ListIconNode = document1.DocumentNode.SelectSingleNode("//ul[@class='author-info']");

                    if (ListIconNode != null)
                    {
                        HtmlNode EmailIcon = ListIconNode.SelectSingleNode("//a[@class='author icon email']");//Email
                        if (EmailIcon != null)
                        {
                            var email = FetchEmaiId(EmailIcon.Attributes["href"].Value);
                            JournalistProfile.Add("email:" + email);
                            b = true;
                        }
                        else { JournalistProfile.Add("email:" + null); }

                        HtmlNode TwitterIcon = ListIconNode.SelectSingleNode("//a[@class='author icon twitter']");//Twitter

                        if (TwitterIcon != null)
                        {
                            var TwitterUrl = TwitterIcon.Attributes["href"].Value;
                            JournalistProfile.AddRange(GetTwitterProfile(TwitterUrl));
                            JournalistProfile.Add("Media link:" + TwitterUrl);
                        }
                        else
                        { JournalistProfile.Add("Media link:" + null); }

                        HtmlNode LinkedInIcon = ListIconNode.SelectSingleNode("//a[@class='author icon linkedin']");//LinkedIn

                        if (LinkedInIcon != null)
                        {
                            var linkedInUrl = LinkedInIcon.Attributes["href"].Value;
                            JournalistProfile.Add("LinkedIn:" + linkedInUrl);
                        }
                        else
                        { JournalistProfile.Add("LinkedIn:" + null); }
                    }

                }

                if (b == false)
                {
                    if (AnchorTag2 != null)
                    {
                        var twitterUrl = AnchorTag2.Attributes["href"].Value;
                        JournalistProfile.AddRange(GetTwitterProfile(twitterUrl));
                        JournalistProfile.Add("Media link:" + twitterUrl);
                    }
                   
                    if (AnchorTag3 != null)
                    {
                        var linkedInUrl = AnchorTag3.Attributes["href"].Value;
                        JournalistProfile.Add("LinkedIn:" + linkedInUrl);
                    }
                }

            }
            catch (Exception)
            { }

            if (JournalistProfile.Count != 0)
            {

                if (!(IsEmailIdExist(JournalistProfile)))
                {
                    JournalistProfile.Clear();
                }
                
                return GetDistinctProfile(JournalistProfile);
            }
            return JournalistProfile;


        }

        public static List<string> FetchNameFromHtmlNode(HtmlAgilityPack.HtmlDocument document)
        {
            List<string> JournalistProfile = new List<string>();
            JournalistProfile.Clear();
            string text = string.Empty;
            try
            {
             
                //--------------------------------------checking1--------------------------------------------------------------------------//
                HtmlNode AngorTagNameNode = document.DocumentNode.SelectSingleNode("//a[@rel='author']");
                //------------------------------------cheching 2----------------------------------------------------------------------------//
                HtmlNode SpanNameNode = document.DocumentNode.SelectSingleNode("//span[@class='name']");
                //---------------------------------------checking3-------------------------------------------------------------------------//
                HtmlNode SpanAssetMetaItemNode = document.DocumentNode.SelectSingleNode("//span[@class='asset-metabar-author asset-metabar-item']");
                  
                    if (AngorTagNameNode != null )
                    {   
                        text = AngorTagNameNode.InnerText;
                        goto NameFilteration;
                    }
                    if(SpanNameNode != null)
                    {
                        text = AngorTagNameNode.InnerText;
                        goto NameFilteration;
                    }
                    if(SpanAssetMetaItemNode != null)
                    {
                        text = AngorTagNameNode.InnerText;
                        goto NameFilteration;
                    }
            NameFilteration:
                    if (!(string.IsNullOrEmpty(text)))
                    {
                        text = NameNodeInnerTextFilteration(text);
                        bool isOther = IsValidString(text);

                        if (isOther == false)
                        {
                            JournalistProfile.Add("name:" + text);
                        }
                    }
                   
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return JournalistProfile;
        }

        public static string NameNodeInnerTextFilteration(string TextToSplit)
        {
            string text = string.Empty;

            if (!(string.IsNullOrEmpty(TextToSplit)))
            {

                string[] token = TextToSplit.Split(',').ToArray();

                if (token.Count() > 1)
                {
                    text = token[0];
                }
                else
                {
                    text = TextToSplit;
                }
            }
            return text; 
        }

        public static string StateCountryFilterarion(string textToFilterate)
        {
            string geoGraphicalWord=string.Empty;

            try
            {
                Regex regex = new Regex(@"\w(?<!\d)[\w'-]*");
                MatchCollection  matchCollection = regex.Matches(textToFilterate);

                foreach (Match match in matchCollection)
                {
                    GroupCollection groups = match.Groups;
                    foreach (Group group in groups)
                    {
                        string word = group.Value;
                        bool isOther = IsValidString(word);

                        if (isOther == false)
                        {
                            geoGraphicalWord = word;
                            break;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return geoGraphicalWord;
        }

        /// <summary>
        /// //FETCH THE "twitter" PROFILE INFO OF A REPORTER.
        /// </summary>
        /// <param name="twitterAcUrl"></param>
        /// <returns></returns>
        public static List<string> GetTwitterProfile(string twitterAcUrl)
        {
            List<string> twitterProfiles = new List<string>();
            twitterProfiles.Clear();
        
            try
            {
                var twitterPage = HtmlAgilityPackProxySettings().DownloadString(twitterAcUrl);
                HtmlAgilityPack.HtmlDocument Twitterdocument = new HtmlAgilityPack.HtmlDocument();
                Twitterdocument.LoadHtml(twitterPage);

                //HtmlWeb web1 = new HtmlWeb();
                //var Twitterdocument = web1.Load(twitterAcUrl);
               
                HtmlNode ProfileCard = Twitterdocument.DocumentNode.SelectSingleNode("//div[@class='ProfileHeaderCard']");
                HtmlNodeCollection ProfileCardChilds = ProfileCard.ChildNodes;

                foreach (HtmlNode BioInfoNode in ProfileCardChilds)
                {
                    if (BioInfoNode.Name == "p")
                    {
                        var BioInfo = BioInfoNode.SelectSingleNode("//p[@class='ProfileHeaderCard-bio u-dir']");
                        var BioInformation = BioInfo.InnerText;
                        var email = FetchEmaiId(BioInformation);

                        if (email != null)
                        { twitterProfiles.Add("email:" + email); }
                        else { twitterProfiles.Add("email:" + null); }

                        if (BioInformation != null)
                        { twitterProfiles.Add("bio:" + BioInformation); }
                        else
                        { twitterProfiles.Add("bio:" + null); }

                    }
                    //if (BioInfoNode.Name == "a")
                    //{
                    //    var nameInfo = BioInfoNode.SelectSingleNode("//a[@class='ProfileHeaderCard-nameLink u-textInheritColor js-nav']");
                    //    var name = nameInfo.InnerText;

                    //    if (name != null)
                    //    { twitterProfiles.Add("name:" + name); }
                    //    else
                    //    { twitterProfiles.Add("name:" + null); }

                    //}

                    if (BioInfoNode.Name == "div")
                    {
                        if (BioInfoNode.Attributes["class"].Value == "ProfileHeaderCard-location")
                        {
                            var Location = BioInfoNode.ChildNodes[3].InnerText;
                            if (Location != null)
                            {
                                Location = StateCountryFilterarion(Location);
                                twitterProfiles.Add("Loc:" + Location); 
                            }
                            else
                            { twitterProfiles.Add("Loc:" + null); }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Core task failed while getting profile info from twitter. " + ex.InnerException.ToString());
            }

            return twitterProfiles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtContents"></param>
        public static void ExportToLogFile(DataTable dtContents)
        {
            StreamWriter writer = null;
            try
            {
                string fileName = "news_" + DateTime.Today.ToString("d MMM yyyy") + ".csv";
                var dir = @"C:\GNC\Log\";

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    DeleteOlderFiles();
                }

                using (FileStream fs = new FileStream(dir + fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    if (writer != null)
                    {
                    writer.Close();
                    }
                    using (writer = new StreamWriter(fs))
                    {
                        WriteDataTable(dtContents, writer, true);
                    }
                }

            }
            
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Failed to write to log file. " + ex.InnerException.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceTable"></param>
        /// <param name="writer"></param>
        /// <param name="includeHeaders"></param>
        public static void WriteDataTable(DataTable sourceTable, TextWriter writer, bool includeHeaders) 
        {
            try
            {
                if (includeHeaders)
                {
                    var headerColumns = new string[] { "Email", "Name", "State", "Country", "Association", "MediaLink", "Website", "AboutInfo" };
                    SetColumnsOrder(sourceTable, headerColumns);

                    IEnumerable<String> headerValues = sourceTable.Columns
                        .OfType<DataColumn>()
                        .Select(column => QuoteValue(column.ColumnName));

                    writer.WriteLine(String.Join(",", headerValues));
                }

                IEnumerable<String> items = null;

                foreach (DataRow row in sourceTable.Rows)
                {
                    items = row.ItemArray.Select(o => QuoteValue(o.ToString()));
                    writer.WriteLine(String.Join(",", items));
                }

                writer.Flush();
                writer.Close();

                CrawlerEventInfoLog("Records logged.");
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog("Failed to write to log file. " + ex.InnerException.ToString());
            }
              
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        public static void SetColumnsOrder( DataTable dataTable, params String[] columnNames)
        {
            try
            {
                int columnIndex = 0;
                foreach (var columnName in columnNames)
                {
                    dataTable.Columns[columnName].SetOrdinal(columnIndex);
                    columnIndex++;
                }

                var toRemove = new string[] { "JournalistId", "Designation", "Tags"};

                foreach (string col in toRemove)
                    dataTable.Columns.Remove(col);
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
                
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string QuoteValue(string value)
        {
              return String.Concat("\"",
              value.Replace("\"", "\"\""), "\"");
          
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        //public static bool SaveJournalist(DataTable dtJournalist)
        //{
        //    bool b = false;
        //    try
        //    {
        //         // string query = "INSERT INTO Journalist(JournalistName,JournalistTitle,JournalistBeats,JournalistSubBeats,JournalistLocation,JournalistEmail,JournalistSMLinks,JournalistWebsite,JournalistAssociation,JournalistBioInfo,PublicationDate,PublicationTitle) VALUES('" + dtJournalist.Rows[0]["name"] + "','" + dtJournalist.Rows[0]["ReporterTitle"] + "','" + dtJournalist.Rows[0]["Beat"] + "','" + dtJournalist.Rows[0]["SubBeat"] + "','" + dtJournalist.Rows[0]["Location"] + "','" + dtJournalist.Rows[0]["Email"] + "','" + dtJournalist.Rows[0]["MediaLinks"] + "','" + dtJournalist.Rows[0]["Website"] + "','" + dtJournalist.Rows[0]["Accosiation"] + "','" + dtJournalist.Rows[0]["AboutInfo"] + "','" + dtJournalist.Rows[0]["PubDate"] + "','" + dtJournalist.Rows[0]["NewsTitle"] + "')";
        //        if (dataAccess.sqlExecute(dtJournalist, "Journalist"))
        //        {
        //            b = true;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        CrawlerEventErrorLog(ex.InnerException.ToString());
        //    }
        //    return b;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        public static DataTable  SaveSubBeat(DataTable dtSubBeats)
        {
            List<int> subBeatIds = new List<int>();
            subBeatIds.Clear();
           
            try
            {
                var SubBeats = CreateSubBeatRecord(dtSubBeats);
                IEnumerable<DataRow> Row = from SubBeatTopic in SubBeats.AsEnumerable()
                                           where !string.IsNullOrEmpty(SubBeatTopic.Field<string>("SubBeatTopic"))
                                           select SubBeatTopic;

                DataTable dtTable = new DataTable();
                DataTable UniqueRecords = new DataTable();
                if (Row.Any())
                {
                  dtTable = Row.CopyToDataTable<DataRow>();
                  UniqueRecords= RemoveDuplicatesRecords(dtTable);
                }
                else
                {
                    dtTable = null;
                }

                DataSet subBeatItems = new DataSet();
                if (dtTable != null)
                {
                    if (UniqueRecords.Rows.Count > 0)
                    {
                        TrimData(UniqueRecords);

                        subBeatItems = ProcessingForSubBeats(UniqueRecords);
                    }
                }

                for (int i = 0; i < dtSubBeats.Rows.Count;i++)
                {
                    string subBeatTopic = dtSubBeats.Rows[i]["SubBeatTopic"].ToString();

                    if (!string.IsNullOrEmpty(subBeatTopic))
                    {
                        var matchItem = (from row in subBeatItems.Tables[0].AsEnumerable()
                                         where row.Field<string>("SubBeatTopic") == subBeatTopic
                                         select row.Field<int>("SubBeatId")).ToList();

                        if (matchItem.Any())
                        {
                            dtSubBeats.Rows[i]["SubBeatId"] = Convert.ToInt32(matchItem[0]);
                        }
                       
                    }
                    else
                    {
                        //This subbeat is belong to 'Other' subcategory in DB.
                        int defaultSubBeatId = GetDefaultSubBeatId();
                        dtSubBeats.Rows[i]["SubBeatId"] = defaultSubBeatId;
                    }

                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dtSubBeats;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        public static bool SaveArticles(DataTable dtArticle)
        {
            bool b = false;
            try
            {
                RemoveDuplicationFromArticle(dtArticle);
                if (dtArticle.Rows.Count > 0)
                {
                    string[] MappingColumns = { "PublicationDate", "ArticleTitle", "ArticleLink", "JournalistId" };
                    if (dataAccess.sqlExecute(dtArticle, "Article", MappingColumns))
                    {
                        b = true;
                    }
                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        public static bool SaveArticleSubBeats(DataTable dtSubBeats)
        {
            bool b = false;
            try
            {
                string[] MappingColumns = { "ArticleId", "SubBeatId" };
                if (dataAccess.sqlExecute(dtSubBeats, "ArticleSubbeat",MappingColumns))
                {
                    b = true;
                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="JournalistProfile"></param>
        /// <returns></returns>
        public static List<string> GetDistinctProfile(List<string> JournalistProfile)
        {
            try
            {
                if (JournalistProfile != null && JournalistProfile.Count > 0)
                {
                    var DuplicateElement = JournalistProfile.FindAll(x => x.StartsWith("email:"));
                    JournalistProfile = JournalistProfile.Except(DuplicateElement).ToList();
                    int DupNo = DuplicateElement.Count;

                    if (DupNo > 1)
                    {
                        DuplicateElement.RemoveRange(1, DupNo - 1);
                    }
                    JournalistProfile.Insert(0, DuplicateElement[0]);
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return JournalistProfile;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="JournalistProfile"></param>
        /// <returns></returns>
        public static bool IsEmailIdExist(List<string> JournalistProfile)
        {
            bool b = false;

            if (JournalistProfile != null || JournalistProfile.Count > 0)
            {
                int BaseLength = "email:".Length;
                int index = JournalistProfile.FindIndex(a => a.StartsWith("email:"));

                if (index != -1)
                {
                    var email = JournalistProfile.ElementAt(index);
                    int XLength = email.Length;

                    if (XLength > BaseLength)
                    {
                        b = true;
                    }
                    else
                    {
                        b = false;
                    }
                }
            }
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BeatName"></param>
        /// <returns></returns>
        public static int GetBeatId(string BeatName)
        {
            int Beatid = 0;
            DataAccess objDataAccess = new DataAccess();

            try
            {
                string query = "SELECT BeatIId FROM Beat WHERE BeatTopic='" + BeatName.Trim() + "'";
                DataSet dsBeatId = objDataAccess.getdata("", query);
                Beatid = Convert.ToInt32(dsBeatId.Tables[0].Rows[0]["BeatIId"]);
            }
            catch (Exception)
            { }

            return Beatid;
           // //int Beatid = 0;
           // var Beat = (Beats)Enum.Parse(typeof(Beats), BeatName);

           // switch (Beat)
           // {
           //     case Beats.Business: return Beatid = 1;
           //         break;
           //     case Beats.Entertainment: return Beatid = 2;
           //         break;
           //     case Beats.Sports: return Beatid = 3;
           //         break;
           //     case Beats.Technology: return Beatid = 4;
           //         break;
           //     case Beats.Science: return Beatid = 5;
           //         break;
           //     case Beats.Health: return Beatid = 6;
           //         break;
           //     case Beats.Spotlight: return Beatid = 7;
           //         break;
           //     case Beats.Elections: return Beatid = 8;
           //         break;
           //     case Beats.World: return Beatid = 9;
           //         break;
           //     case Beats.Local: return Beatid = 10;
           //         break;
           //     default:
           //         break;
           //}
           // return Beatid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BeatName"></param>
        /// <returns></returns>
        public static DataSet GetBeats()
        {
            DataAccess objDataAccess = new DataAccess();
            DataSet dsBeats = new DataSet();
            dsBeats.Clear();
           
            try
            {
                string query = "SELECT * FROM Beat";
                dsBeats = objDataAccess.getdata("", query);
                
            }
            catch (Exception)
            { }
            return dsBeats;
        }

        /// <summary>
        /// This example method generates a DataTable.
        /// </summary>
        public static DataTable GetTable()
        {
           
            DataTable table = new DataTable();

            table.Columns.Add("JournalistId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Designation", typeof(string));
            table.Columns.Add("Beat", typeof(string));
            table.Columns.Add("Tags", typeof(string));
            table.Columns.Add("state", typeof(string));
            table.Columns.Add("country", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("MediaLink", typeof(string));
            table.Columns.Add("LinkedInUrl", typeof(string));
            table.Columns.Add("Website", typeof(string));
            table.Columns.Add("Association", typeof(string));
            table.Columns.Add("AboutInfo", typeof(string));
            table.Columns.Add("ArticleId", typeof(int));
            table.Columns.Add("PublicationDate", typeof(DateTime));
            table.Columns.Add("ArticleTitle", typeof(string));
            table.Columns.Add("ArticleLink", typeof(string));
          
            table.PrimaryKey = new DataColumn[] { table.Columns["Email"] };
            return table;
        }

        /// <summary>
        /// This example method generates a DataTable.
        /// </summary>
        public static DataTable GetSubBeatTable()
        {
            DataTable table = new DataTable();

            table.Columns.Add("JournalistId", typeof(int));
            table.Columns.Add("ArticleId", typeof(int));
            table.Columns.Add("SubBeatId", typeof(int));
            table.Columns.Add("SubBeatTopic", typeof(string));
            table.Columns.Add("newsId", typeof(int));
            table.Columns.Add("BeatId", typeof(int));
            table.Columns.Add("guidText", typeof(string));
            table.Columns.Add("channelId", typeof(int));
            table.Columns.Add("BeatLink", typeof(string));
            table.Columns.Add("cid", typeof(string));
           
         
            return table;
        }
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string FetchEmaiId(string text)
        {
            string Email = string.Empty;
            const string MatchEmailPattern =
           @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
           + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
             + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
           + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";
            Regex rx = new Regex(MatchEmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Find matches.
            MatchCollection matches = rx.Matches(text);
            // Report the number of matches found.
            int noOfMatches = matches.Count;

            if (noOfMatches != 0)
            {
                // Report on each match.
                foreach (Match match in matches)
                {
                    Email = match.Value.ToString();
                }
            }
            return Email;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="HTMlString"></param>
        /// <returns></returns>
        public static string UnescapeHTMLValue(string HTMlString)
        {
            if (HTMlString == null)
            { }
            return HTMlString.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&").Replace("&nbsp;", "").Replace("&raquo;", "").Replace("\r", "").Replace("\n", "").Replace("'", "").Replace(",", "").Replace(".", "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static string EscapeXMLValue(string xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException("xmlString");

            return xmlString.Replace("'", "&apos;").Replace("\"", "&quot;").Replace(">", "&gt;").Replace("<", "&lt;").Replace("&", "&amp;");
        }

        /// <summary>
        ///  //NamedEnityRecognizer
        //To recognize the given text is a geographical name or person's name.
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static bool IsValidString(string Text)
        {
            bool b = false;
            try
            {

                string exe = Process.GetCurrentProcess().MainModule.FileName;
                string path = Path.GetDirectoryName(exe); 
               // string path = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
                CRFClassifier Classifier = CRFClassifier.getClassifierNoExceptions(path + @"\stanford-ner-2013-06-20\stanford-ner-2013-06-20\classifiers\english.all.3class.distsim.crf.ser.gz");

                var classified = Classifier.classifyToCharacterOffsets(Text).toArray();

                int length = classified.Length;

                //If the subbeat is a personal name or location ,the 'length' become 1.so length become 'zero' means this is a valid subbeat.
                if (length == 0)
                {

                    string[] format = new string[] { "yyyy-MM-dd HH:mm:ss" };
                    string value = Text;
                    DateTime datetime;

                    if (DateTime.TryParseExact(value, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                    {
                        b = false;
                    }
                    else
                    {
                        //valid subeat.
                        b = true;
                    }

                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return b;

        }

        /// <summary>
        /// This example method generates a DataTable.
        /// </summary>
        public static DataTable CreateJournalistRecord(DataTable dtCrawler)
        {
            DataTable dtJournalist = new DataTable();

            try
            {
                dtJournalist = dtCrawler.Copy();
                var toRemove = new string[] { "Beat", "PublicationDate", "ArticleTitle", "ArticleId", "ArticleLink" };

                foreach (string col in toRemove)
                    dtJournalist.Columns.Remove(col);

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
         
            return dtJournalist;
        }

        /// <summary>
        /// This example method generates a DataTable.
        /// </summary>
        public static DataTable CreateSubBeatRecord(DataTable dtSubBeat)
        {
            DataTable dtSubBeatTb = new DataTable();

            try
            {
                dtSubBeatTb = dtSubBeat.Copy();
                var toRemove = new string[] { "JournalistId", "ArticleId", "newsId", "guidText", "channelId", "BeatLink", "cid" };

                foreach (string col in toRemove)
                    dtSubBeatTb.Columns.Remove(col);

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return dtSubBeatTb;
        }
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtCrawler"></param>
        /// <returns></returns>
        public static DataTable CreateArticleRecords(DataTable dtCrawler,DataSet dsJournalistRec)
        {
            DataTable dtArticle = new DataTable();

            try
            {
                dtArticle = dtCrawler.DefaultView.ToTable(false, "ArticleId", "PublicationDate", "ArticleTitle", "ArticleLink");
                DataColumn newCol1 = new DataColumn("JournalistId", typeof(Int32));
                dtArticle.Columns.Add(newCol1);
               
                for (int i=0;i<dtArticle.Rows.Count;i++)
                {
                    dtArticle.Rows[i]["JournalistId"] = Convert.ToInt32(dsJournalistRec.Tables["Sorted Table"].Rows[i]["JournalistId"]);
                }
              
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
                
            return dtArticle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtSubbeats"></param>
        /// <returns></returns>
        public static DataTable CreateArticleSubbeatRecords(DataTable dtSubbeats)
        {
            DataTable dtArticleSubbeat = new DataTable();

            try
            {
                dtArticleSubbeat = dtSubbeats.DefaultView.ToTable(false, "ArticleId", "SubBeatId");
                DataColumn newCol = new DataColumn("ArticleSubbeatsId", typeof(Int32));
                dtArticleSubbeat.Columns.Add(newCol);
                newCol.SetOrdinal(0);
            }
            catch (Exception ex)
            {
              CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return dtArticleSubbeat;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clusterid"></param>
        /// <param name="beatUrl"></param>
        /// <returns></returns>
        public static List<string> GetSubBeats(string clusterid, string beatUrl)
        {
            List<string> SubBeatsList = new List<string>();
            SubBeatsList.Clear();
            
            try
            {
                if (!(string.IsNullOrEmpty(beatUrl))&& IsNumeric(clusterid)==true)
                {
                    var BeatPage = HtmlAgilityPackProxySettings().DownloadString(beatUrl);
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(BeatPage);

                    //HtmlWeb web1 = new HtmlWeb();
                    //var htmlDoc = web1.Load(beatUrl);

                    HtmlNode parentDivNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@cid='" + clusterid + "' and @class='story anchorman-blended-story esc esc-has-thumbnail '] ");

                    if (parentDivNode != null)
                    {
                        HtmlNodeCollection childDivCollection = parentDivNode.ChildNodes;

                        foreach (HtmlNode childDiv in childDivCollection)
                        {
                            if (childDiv.Attributes["class"].Value == "esc-inner esc-collapsed")
                            {
                                HtmlNodeCollection escbodyCollectionDiv = childDiv.ChildNodes;

                                foreach (HtmlNode escbodyDiv in escbodyCollectionDiv)
                                {
                                    if (escbodyDiv.Attributes["class"].Value == "esc-body")
                                    {

                                        HtmlNodeCollection WrapperNodes = escbodyDiv.ChildNodes;
                                        foreach (HtmlNode Wrappernode in WrapperNodes)
                                        {
                                            if (Wrappernode.Attributes["class"].Value == "esc-default-layout-wrapper esc-expandable-wrapper")
                                            {
                                                HtmlNode expandableWrapperNode = Wrappernode.SelectSingleNode("//div[@class='esc-default-layout-wrapper esc-expandable-wrapper']");

                                                if (expandableWrapperNode != null)
                                                {
                                                    HtmlNodeCollection expandableWrapperNodechilds = expandableWrapperNode.ChildNodes;
                                                    foreach (HtmlNode expandableWrapperNodechild in expandableWrapperNodechilds)
                                                    {
                                                        HtmlNode table = expandableWrapperNodechild;

                                                        if (table.Name == "table")
                                                        {
                                                            foreach (HtmlNode row in table.SelectNodes("//tr"))
                                                            {
                                                                HtmlNode cellNode = row.SelectSingleNode("//td[@class='esc-layout-article-cell']");
                                                                HtmlNodeCollection cellNodeChilds = cellNode.ChildNodes;

                                                                foreach (HtmlNode div in cellNodeChilds)
                                                                {
                                                                    // HtmlNode cellNode = div.SelectSingleNode("//td[@class='esc-layout-article-cell']");
                                                                    HtmlNode divNode = div.SelectSingleNode("//div[@class='esc-extension-wrapper']");

                                                                    if (divNode != null)
                                                                    {
                                                                        HtmlNodeCollection TopicWrapperChildNodes = divNode.ChildNodes;
                                                                        foreach (HtmlNode wrapperNode in TopicWrapperChildNodes)
                                                                        {
                                                                            HtmlNode escInlineTopicWrapperNode = wrapperNode.SelectSingleNode(@"//div[@class='esc-inline-topics-wrapper'] ");
                                                                            if (escInlineTopicWrapperNode != null)
                                                                            {

                                                                                HtmlNodeCollection InlineTopicWrapperChilds = escInlineTopicWrapperNode.ChildNodes;
                                                                                foreach (HtmlNode InlineTopicWrapperChild in InlineTopicWrapperChilds)
                                                                                {
                                                                                    HtmlNode TopicWrapperNode = InlineTopicWrapperChild.SelectSingleNode("//div[@class='esc-topics-wrapper']");

                                                                                    if (TopicWrapperNode != null)
                                                                                    {
                                                                                        HtmlNodeCollection TopicWrapperNodeChilds = TopicWrapperNode.ChildNodes;
                                                                                        foreach (HtmlNode TopicWrapperNodeChild in TopicWrapperNodeChilds)
                                                                                        {
                                                                                            if (TopicWrapperNodeChild.Attributes["class"].Value == "esc-topic-wrapper")
                                                                                            {
                                                                                                var topic = UnescapeHTMLValue(TopicWrapperNodeChild.InnerText);
                                                                                                var b = IsValidString(topic);

                                                                                                if (b == true)
                                                                                                {
                                                                                                    SubBeatsList.Add(topic.Trim());
                                                                                                }
                                                                                            }

                                                                                        }
                                                                                        break;
                                                                                    }
                                                                                }
                                                                                break;

                                                                            }

                                                                        }
                                                                        break;
                                                                    }

                                                                }
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return SubBeatsList;
        
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtSubBeats"></param>
        /// <returns></returns>
        public static DataTable FillSubBeat(DataTable dtSubBeats)
        {
            try
            {
                if (dtSubBeats.Rows.Count > 0)
                {
                    for (int i = 0; i < dtSubBeats.Rows.Count; i++)
                    {
                        var Guidtext = dtSubBeats.Rows[i]["guidText"].ToString();
                        if (Guidtext != null)
                        {
                            string[] Tokens = Guidtext.Split('=');
                            dtSubBeats.Rows[i]["cid"] = Tokens[1];
                        }

                        var SubBeatTopics = GetSubBeats(dtSubBeats.Rows[i]["cid"].ToString(), dtSubBeats.Rows[i]["BeatLink"].ToString());


                        if (SubBeatTopics.Count != 0)
                        {
                            foreach (var Topic in SubBeatTopics)
                            {
                              dtSubBeats.Rows[i]["SubBeatTopic"] = Topic;
                            }
                        }
                        //dtSubBeats.Rows[i]["JournalistId"] = JournalistIds[i];
                    }
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dtSubBeats;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtSubBeats"></param>
        /// <returns></returns>
        public static DataTable FillArticleId(DataTable Records, List<int> ArticleIds)
        {
            try
            {
                if (ArticleIds.Count > 0)
                {
                    if (Records.Rows.Count > 0)
                    {
                        for (int j = 0; j < Records.Rows.Count; j++)
                        {
                            Records.Rows[j]["ArticleId"] = Convert.ToInt32(ArticleIds[j]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return Records;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TbName"></param>
        /// <returns></returns>
        public static DataSet  GetJournalistTopIds()
        {
           DataSet recentIds = new DataSet();
           int topNo = 0;
            try
            {
                topNo = DataAccess.NoOfRowCopied;
               
                if (topNo > 0)
                {
                    string query = "SELECT TOP " + topNo + " JournalistId,Email FROM Journalist order by JournalistId desc";
                    recentIds = dataAccess.getdata("Journalist", query);

                }
                
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return recentIds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TbName"></param>
        /// <returns></returns>
        public static DataSet GetSubBeatTopIds()
        {
            DataSet recentIds = new DataSet();
            List<int> recentJournalistIds = new List<int>();
            recentJournalistIds.Clear();

            int topNo = 0;
            try
            {
                topNo = DataAccess.NoOfRowCopied;

                if (topNo > 0)
                {
                    string query = "SELECT TOP " + topNo + " SubBeatId , SubBeatTopic FROM SubBeat order by SubBeatId desc";
                    recentIds = dataAccess.getdata("SubBeat", query);

                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return recentIds;
        }

         /// <summary>
        /// 
        /// </summary>
        /// <param name="TbName"></param>
        /// <returns></returns>
        public static int GetDefaultSubBeatId()
        {
            int defaultSubBeatId = 0;

            string query = "SELECT SubBeatId FROM SubBeat WHERE SubBeatTopic='Other'";
            var subBeatRecord = dataAccess.getdata("SubBeat", query);

            if (subBeatRecord.Tables[0].Rows.Count > 0)
            {
                defaultSubBeatId = Convert.ToInt32(subBeatRecord.Tables[0].Rows[0]["SubBeatId"]);
            }
            return defaultSubBeatId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TbName"></param>
        /// <returns></returns>
        public static List<int> GetArticleTopIds()
        {
            List<int> recentJournalistIds = new List<int>();
            recentJournalistIds.Clear();

            int topNo = 0;
            try
            {
                topNo = DataAccess.NoOfRowCopied;

                if (topNo > 0)
                {
                    string query = "SELECT TOP " + topNo + " ArticleId  as Id FROM Article order by ArticleId desc";
                    var recentIds = dataAccess.getdata("SubBeat", query);

                    if (recentIds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow Row in recentIds.Tables[0].Rows)
                        {
                            recentJournalistIds.Add(Convert.ToInt32(Row["Id"]));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return recentJournalistIds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static  DataTable RemoveDuplicatesRecords(DataTable dataTable)
        {
            try
            {
                string[] Columns = { "SubBeatTopic", "BeatId" };
                dataTable = dataTable.DefaultView.ToTable(true, Columns);
            }
            catch (Exception ex)
            {
              CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dataTable;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dTable"></param>
        /// <returns></returns>
        public static DataTable TrimData(DataTable dTable)
        {
            try
            {
                foreach (DataRow dr in dTable.Rows)
                {
                    foreach (DataColumn col in dTable.Columns)
                    {
                        if (col.DataType == typeof(System.String))
                        {
                            object value=dr[col];
                            if (value!=DBNull.Value)
                            dr[col] = dr[col].ToString().Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dTable;
        }

        public static DataSet  ProcessingForSubBeats(DataTable SubBeatRecords)
        {
            DataSet subBeatItems = new DataSet();
            DataTable ExistingSubBeatDatatable = new DataTable();

            try
            {

               // ExistingSubBeatDatatable = SubBeatRecords.DefaultView.ToTable(false, "SubBeatTopic");
             
                DataColumn newColSubBeatId = new DataColumn("SubBeatId", typeof(int));
                DataColumn newColIsExist = new DataColumn("IsExist", typeof(bool));

                SubBeatRecords.Columns.Add(newColSubBeatId);
                SubBeatRecords.Columns.Add(newColIsExist);
                SubBeatRecords.Columns["SubBeatId"].SetOrdinal(0);

                var Topic = (from row in SubBeatRecords.AsEnumerable()

                             select row.Field<string>("SubBeatTopic")).ToList();

                bool IsExist = false;
                int existingSubBeatId = 0;

                if (Topic.Any())
                {
                    int TopicCount = Topic.Count;

                    for (int i = 0; i < TopicCount; i++)
                    {
                        string query = "select * from SubBeat where SubBeatTopic='" + Topic[i] + "'";
                        var recentIds = dataAccess.getdata("SubBeat", query);

                        if (recentIds.Tables[0].Rows.Count > 0)
                        {
                            IsExist = true;
                            existingSubBeatId = Convert.ToInt32(recentIds.Tables[0].Rows[0]["SubBeatId"]);
                        }
                        else
                        {
                            IsExist = false;
                            existingSubBeatId = 0;
                        }

                        SubBeatRecords.Rows[i]["IsExist"] = Convert.ToBoolean(IsExist);
                        SubBeatRecords.Rows[i]["SubBeatId"] = Convert.ToInt32(existingSubBeatId);
                    
                    }


                    IEnumerable<DataRow> ExistRow = from SubBeat in SubBeatRecords.AsEnumerable()
                                                    where SubBeat.Field<bool>("IsExist") == true
                                                    select SubBeat;

                    if (ExistRow.Any())
                    {
                        var existingSubBeatItems = ExistRow.CopyToDataTable<DataRow>();

                        if (existingSubBeatItems.Rows.Count > 0)
                        {
                            existingSubBeatItems.Columns.Remove("IsExist");
                            subBeatItems.Tables.Add(existingSubBeatItems);
                        }
                    }

                    IEnumerable<DataRow> NonExistRow = from SubBeat in SubBeatRecords.AsEnumerable()
                                                       where SubBeat.Field<bool>("IsExist") == false
                                                       select SubBeat;

                    if (NonExistRow.Any())
                    {
                        var NewSubBeatItems = NonExistRow.CopyToDataTable<DataRow>();

                        if (NewSubBeatItems.Rows.Count > 0)
                        {
                            SubBeatRecords.Rows.OfType<DataRow>().ToList().ForEach(r =>
                            {
                                r["SubBeatTopic"] = DBNull.Value.ToString();
                                
                            });

                            for (int i = 0; i < NewSubBeatItems.Rows.Count; i++)
                            {
                                SubBeatRecords.Rows[i]["SubBeatTopic"] = NewSubBeatItems.Rows[i]["SubBeatTopic"].ToString();
                            }

                            RemoveNullOrEmptyRows(SubBeatRecords);
                            SubBeatRecords.Columns.Remove("IsExist");
                           
                            if (SubBeatRecords.Rows.Count > 0)
                            {

                                string[] MappingColumns = { "SubBeatTopic", "BeatId" };
                                if (dataAccess.sqlExecute(SubBeatRecords, "SubBeat",MappingColumns))
                                {
                                    var NonExistSubBeatItems = GetSubBeatTopIds();
                                    subBeatItems.Tables.Add(NonExistSubBeatItems.Tables["SubBeat"].Copy());
                                }
                            }
                        }

                    }

                    if (subBeatItems.Tables.Count > 1)
                    {
                        subBeatItems.Tables[0].Merge(subBeatItems.Tables[1]);
                    }
                }

               
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return subBeatItems;
        }

        public static DataSet ProcessingForJournalist(DataTable JournalistRecord)
        {
          
            DataSet dsJournalistId = new DataSet();
          
            try
            {
                DataColumn newColOrderNo = new DataColumn("OrderNo", typeof(int));
                DataColumn newColIsExist = new DataColumn("IsExist", typeof(bool));
                JournalistRecord.Columns.Add(newColOrderNo);
                JournalistRecord.Columns.Add(newColIsExist);
              
                var Topic = (from row in JournalistRecord.AsEnumerable()
                              select row.Field<string>("Email")).ToList();

                bool IsExist = false;
                int existingJournalistId = 0;
               
                if (Topic.Any())
                {
                    int TopicCount = Topic.Count;

                    for (int i = 0; i < TopicCount; i++)
                    {
                        string query = "select * from Journalist where Email='" + Topic[i] + "'";
                       
                        var recentIds = dataAccess.getdata("Journalist", query);
                        
                        if (recentIds.Tables[0].Rows.Count > 0)
                        {
                            IsExist = true;
                            existingJournalistId = Convert.ToInt32(recentIds.Tables[0].Rows[0]["JournalistId"]);
                        }
                        else
                        {
                            IsExist = false;
                            existingJournalistId = 0;
                        }
                       
                        JournalistRecord.Rows[i]["OrderNo"] = i+1;
                        JournalistRecord.Rows[i]["IsExist"] = Convert.ToBoolean(IsExist);
                        JournalistRecord.Rows[i]["JournalistId"] = Convert.ToInt32(existingJournalistId);

                    }

                    IEnumerable<DataRow> ExistRow = from email in JournalistRecord.AsEnumerable()
                                                    where email.Field<bool>("IsExist") == true
                                                    select email;

                    if (ExistRow.Any())
                    {
                        var existingJournalistRec = ExistRow.CopyToDataTable<DataRow>();

                        if (existingJournalistRec.Rows.Count > 0)
                        {
                            existingJournalistRec.Columns.Remove("IsExist");
                            string[] ColumnsToCopy = { "OrderNo", "JournalistId" };

                            var dtExistJournalist = existingJournalistRec.DefaultView.ToTable(false, ColumnsToCopy);
                            dsJournalistId.Tables.Add(dtExistJournalist);
                          
                        }
                    }

                    IEnumerable<DataRow> NonExistRow = from email in JournalistRecord.AsEnumerable()
                                                       where email.Field<bool>("IsExist") == false
                                                       select email;

                    if (NonExistRow.Any())
                    {
                        var NewJournalistRec = NonExistRow.CopyToDataTable<DataRow>();

                        if (NewJournalistRec.Rows.Count > 0)
                        {
                            string[] tempColumns = { "OrderNo", "Email" };
                            var TempJournalistRec = NewJournalistRec.DefaultView.ToTable(false, tempColumns);
                            
                            NewJournalistRec.Columns.Remove("IsExist");
                            NewJournalistRec.Columns.Remove("OrderNo");
                           
                            string[] MappingColumns = { "Name", "Designation", "state", "country", "Email", "MediaLink", "LinkedInUrl", "Website", "Association", "AboutInfo" };
 
                            if (dataAccess.sqlExecute(NewJournalistRec, "Journalist",MappingColumns))
                                {
                                    //.............Write 'journalist' records to csv file...................//
                                    ExportToLogFile(NewJournalistRec);
                                   
                                    var NewJournalist = GetJournalistTopIds();
                                    var SortedTable = SortingNewJournalistIdsByTempRecords(NewJournalist.Tables[0], TempJournalistRec);
                                   
                                    string[] Columns = { "OrderNo", "JournalistId" };
                                    var dtNewJournalist = SortedTable.DefaultView.ToTable(false, Columns);
                                    dsJournalistId.Tables.Add(dtNewJournalist);
                                
                                }
                           
                        }

                    }
                    
                    JournalistRecord.Columns.Remove("IsExist");
                    JournalistRecord.Columns.Remove("OrderNo");
                    dsJournalistId=MergeAndSortExistingAndNonExistingRecords(dsJournalistId);
                }
               
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dsJournalistId;
        }

        public static DataTable SortingNewJournalistIdsByTempRecords(DataTable NewRecords,DataTable TempRecords)
        {
            try
            {
                DataColumn ColOrderNo = new DataColumn("OrderNo", typeof(int));//For sorting the journalistids 
                NewRecords.Columns.Add(ColOrderNo);    //in correct order.

                NewRecords.Rows.OfType<DataRow>().ToList().ForEach(r =>
                {
                    string email = r["Email"].ToString();
                    int orderNo = SelectOrderNoForEmail(TempRecords, email);
                    r["OrderNo"] = orderNo;
                });
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }

            return NewRecords;
        }

        public static int  SelectOrderNoForEmail(DataTable TempRecords,string Email)
        {
            int  orderNo = 0;
            try
            {
                if (TempRecords.Rows.Count > 0)
                {
                    orderNo = (from row in TempRecords.AsEnumerable()
                               where row.Field<string>("Email") == Email
                               select row.Field<int>("OrderNo")).FirstOrDefault();

                }
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return Convert.ToInt32(orderNo);
        }

        public static DataSet MergeAndSortExistingAndNonExistingRecords(DataSet dsJournalistId)
        {
            try
            {
                if (dsJournalistId.Tables.Count > 1)
                {
                    dsJournalistId.Tables[0].Merge(dsJournalistId.Tables[1]);

                }

                DataView view = new DataView(dsJournalistId.Tables[0]);
                view.Sort = "OrderNo asc";
                DataTable sortedTable = view.ToTable();
                sortedTable.TableName = "Sorted Table";
                dsJournalistId.Tables.Add(sortedTable);

            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dsJournalistId;
        }

        public static DataTable RemoveNullOrEmptyRows(DataTable dataTable)
        {
            string Topic = DBNull.Value.ToString();
            try
            {
                DataRow[] dataRow = dataTable.Select("SubBeatTopic='" + Topic + "' ");
                for (int i = 0; i < dataRow.Length; i++)
                    dataRow[i].Delete();
                dataTable.AcceptChanges();
            }
            catch (Exception ex)
            {
                CrawlerEventErrorLog(ex.InnerException.ToString());
            }
            return dataTable;
        }

        //public static void Test()
        //{
        //    HtmlWeb web = new HtmlWeb();
        //    var document1 = web.Load("http://www.wsj.com/articles/south-koreas-opposition-parties-move-to-impeach-president-park-geun-hye-1480759872");
        //    FetchNameFromHtmlNode(document1);
        //}

        public static bool IsNumeric(string number)
        {
            double n;
            bool isNumeric = double.TryParse(number, out n);
            return isNumeric;
        }

        public static void DeleteOlderFiles()
        {
            var files = new DirectoryInfo(@"C:\GNC\Log\").GetFiles("*.csv");
            DateTime expiryDate = DateTime.Now.AddDays(-3);
            foreach (var file in files)
            {
                if (file.LastWriteTime < expiryDate)
                    file.Delete();
            }

        }

        public static DataTable RemoveDuplicationFromArticle(DataTable articleRecords)
        {
            DataColumn newColIsExist = new DataColumn("IsExist", typeof(bool));
            articleRecords.Columns.Add(newColIsExist);
           
            articleRecords.Rows.OfType<DataRow>().ToList().ForEach(r =>
                            {
                                string query = "select * from Article where ArticleTitle='" + r["ArticleTitle"].ToString() + "' AND JournalistId=" +Convert.ToInt32( r["JournalistId"]) + "";

                                var recentIds = dataAccess.getdata("Journalist", query);

                                if (recentIds.Tables[0].Rows.Count > 0)
                                {
                                    r["IsExist"] = true;
                                }
                                else
                                {
                                    r["IsExist"] = false;
                                }
                               
                            });
            articleRecords.AsEnumerable().Where(r => r.Field<bool>("IsExist") ==true).ToList().ForEach(row => row.Delete());
            //IEnumerable<DataRow> NonExistRow = from row in articleRecords.AsEnumerable()
            //                                   where row.Field<bool>("IsExist") == true
            //                                   select row;


            return articleRecords;
        }
      
    }
}
