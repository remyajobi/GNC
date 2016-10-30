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

namespace GNCService
{
    class Library
    {
        //News categories.
        enum Beats { Business, Entertainment, Sports, Technology, Science, Health, Spotlight, Elections, World, Local };
        //enum Beats { Business, Entertainment};
      
        //GoogleNews rss output 'Api url'
        public static string GoogleNewsApiUrl = "https://news.google.com/news?pz=1&cf=all&ned=us&hl=en&cf=all&as_drrb=b:as_mind:D2&scoring=d&num=50&output=rss";
        public static List<string> NewsTopics = new List<string>();

        public static void Msg()
        {
            Console.WriteLine("Hello");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> GetNewsTopics()
        {
            try
            {
                NewsTopics.Clear();
                foreach (Beats Beat in Enum.GetValues(typeof(Beats)))
                {
                    switch (Beat)
                    {
                        case Beats.Business: NewsTopics.Add(GoogleNewsApiUrl + "&topic=b");
                            break;
                        case Beats.Entertainment: NewsTopics.Add(GoogleNewsApiUrl + "&topic=e");
                            break;
                        case Beats.Sports: NewsTopics.Add(GoogleNewsApiUrl + "&topic=s");
                            break;
                        case Beats.Technology: NewsTopics.Add(GoogleNewsApiUrl + "&topic=tc");
                            break;
                        case Beats.Science: NewsTopics.Add(GoogleNewsApiUrl + "&topic=snc");
                            break;
                        case Beats.Health: NewsTopics.Add(GoogleNewsApiUrl + "&topic=m");
                            break;
                        case Beats.Spotlight: NewsTopics.Add(GoogleNewsApiUrl + "&topic=ir");
                            break;
                        case Beats.Elections: NewsTopics.Add(GoogleNewsApiUrl + "&topic=el");
                            break;
                        case Beats.World: NewsTopics.Add(GoogleNewsApiUrl + "&topic=w");
                            break;
                        case Beats.Local: NewsTopics.Add(GoogleNewsApiUrl + "&geo=detect_metro_area");
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception)
            { }


            return NewsTopics;
        }

        /// <summary>
        /// Core function for this program.All crawling process should done here.
        /// </summary>
        public static void Crawler()
        {
           
            DataSet ds = new DataSet();
            ds.Tables.Clear();
            GetNewsTopics();
            
            Task[] tasks1 = NewsTopics
                          .Select(url => Task.Factory.StartNew(
                              state =>
                              {
                                  using (var client = new WebClient())
                                  {
                                      var u = (string)state;
                                      
                                      client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                                      Stream data = client.OpenRead(u);
                                      StreamReader reader = new StreamReader(data);
                                      string s = reader.ReadToEnd();
                                      // Console.WriteLine(s);
                                      data.Close();
                                      reader.Close();
                                      return s;
                                      //var u = (string)state;
                                      ////Console.WriteLine("starting to download {0}", u);
                                      //string result = client.DownloadString(u);
                                      //return result;
                                      // Console.WriteLine("finished downloading {0}", u);
                                  }
                              }, url).ContinueWith(x =>
                              {
                                  StringReader reader1 = new StringReader(x.Result);

                                  ds.ReadXml(reader1);

                              }

                          )).ToArray();
          
            try
            {
                Task.WaitAll(tasks1);
            }

            catch (AggregateException ae)
            {
                Exception e = ae.Flatten().InnerException;
                ae.Handle((x) =>
                    {
                        if (x is UnauthorizedAccessException) // This we know how to handle.
                        {
                            Console.WriteLine("You do not have permission to access all folders in this path.");
                            Console.WriteLine("See your network administrator or try another path.");
                            return true;
                        }
                        return false; // Let anything else stop the application.
                    });
            }
          

            DataTable dtGetNews = new DataTable();
            dtGetNews.Clear();
            DataTable dtGuid = new DataTable();
            dtGuid.Clear();
            DataTable dtCrawler = new DataTable();
            dtCrawler.Clear();
            DataTable dtSubBeats = new DataTable();
            dtSubBeats.Clear();
            if (ds.Tables.Count > 3)
            {
                dtGuid = ds.Tables["guid"];
                dtGetNews = ds.Tables["item"];
                dtCrawler = GetTable();
                dtSubBeats = GetSubBeatTable();
                DataRow drCrawler;
                DataRow drSubBeats;
                Task[] tasks2 = new Task[dtGetNews.Rows.Count];
                try
                {
                    Parallel.For(0, dtGetNews.Rows.Count, row =>
                    {
                        tasks2[row] = Task.Factory.StartNew(() =>
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
                                                 string[] loc = item.Split(',').ToArray();
                                                 drCrawler["state"] = loc[0];
                                                 drCrawler["country"] = loc[1];
                                             }

                                             if (item.Contains("Media link:"))
                                             {
                                                 item = item.Replace("Media link:", "");
                                                 drCrawler["MediaLink"] = item;
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

                                             drCrawler["Beat"] = dtGetNews.Rows[row]["category"];
                                            
                                             drCrawler["Association"] = Publisher;
                                             drCrawler["PublicationDate"] = dtGetNews.Rows[row]["pubdate"];
                                             drCrawler["ArticleTitle"] = UnescapeHTMLValue(dtGetNews.Rows[row]["title"].ToString());

                                             drSubBeats["BeatIId"] = GetBeatId(drCrawler["Beat"].ToString());
                                             drSubBeats["newsId"] = Convert.ToInt32(dtGetNews.Rows[row]["item_Id"]);
                                             drSubBeats["channelId"] = Convert.ToInt32(dtGetNews.Rows[row]["channel_Id"]);

                                             try
                                             {

                                                 drSubBeats["guidText"] = (from DataRow dr in dtGuid.Rows.AsParallel()
                                                                          where (int)dr["item_Id"] == Convert.ToInt32(drSubBeats["newsId"])
                                                                 select (string)dr["guid_Text"]).FirstOrDefault();


                                                 drSubBeats["BeatLink"] = (from DataRow dr in ds.Tables["channel"].Rows
                                                                  where (int)dr["channel_Id"] == Convert.ToInt32(drSubBeats["channelId"])
                                                                  select (string)dr["link"]).FirstOrDefault();

                                                
                                             }
                                             catch (Exception)
                                             { }
                                            
                                         });
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        dtCrawler.Rows.Add(drCrawler);
                                        dtSubBeats.Rows.Add(drSubBeats);
                                    }
                                    catch (Exception)
                                    { }


                                }

                            },// close first Action

                                  () =>
                                  {

                                  }//close second Action

                                );

                        });

                    });


                }
                catch (Exception)
                { }


                try
                {
                    Task.WaitAll(tasks2);

                }
                catch (AggregateException ae)
                {
                    Exception e = ae.Flatten().InnerException;
                    ae.Handle((x) =>
                        {
                            if (x is UnauthorizedAccessException) // This we know how to handle.
                            {
                                Console.WriteLine("You do not have permission to access all folders in this path.");
                                Console.WriteLine("See your network administrator or try another path.");
                                return true;
                            }
                            return false; // Let anything else stop the application.
                        });
                }
               
                try
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
                                    if (SubBeatTopics.Count < 1)
                                        dtSubBeats.Rows[i]["SubBeatTopic"] = Topic + ",";
                                    else
                                        dtSubBeats.Rows[i]["SubBeatTopic"] = Topic;
                                }
                            }
                       

                    }
                    
                   var  JournalistRec= CreateJournalistRecord(dtCrawler);
                   DataAccess objDataAccess = new DataAccess(); 
                   if (SaveJournalist(JournalistRec))
                   {
                       var JournalistIds = objDataAccess.IdsOfJournalists.ToList();
                       JournalistIds.Sort();
                   
                       dtSubBeats = CreateSubBeatRecord(dtSubBeats);
                       if (SaveSubBeat(dtSubBeats))
                       {
                           var SubBeatIds = objDataAccess.IdsOfJournalists.ToList();
                           SubBeatIds.Sort();

                           var ArticleRec = CreateArticleRecords(dtCrawler, JournalistIds, SubBeatIds, dtSubBeats);

                       }
                   }
                   
                //   ExportToLogFile(dtCrawler);
                }
                catch (Exception)
                { }
             
            }


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

            try
            {

                HtmlWeb web1 = new HtmlWeb();
                var document1 = web1.Load(PublisherUrl);
                HtmlNode AnchorTag1 = document1.DocumentNode.SelectSingleNode("//a[@rel='author']");
                HtmlNode AnchorTag2 = document1.DocumentNode.SelectSingleNode("//a[@class='author-twitter']");

                if (AnchorTag1 != null)
                {
                    //Case 1: Author bio icon directly append to author name.
                    //eg:http://www.wsj.com/articles/salesforce-won-t-pursue-bid-for-twitter-1476468050
                    //List of author bio icons.

                    HtmlNode ListIconNode = document1.DocumentNode.SelectSingleNode("//ul[@class='author-info']");

                    if (ListIconNode != null)
                    {
                        HtmlNode EmailIcon = ListIconNode.SelectSingleNode("//a[@class='author icon email']");
                        if (EmailIcon != null)
                        {
                            var email = FetchEmaiId(EmailIcon.Attributes["href"].Value);
                            b = true;
                            var name = AnchorTag1.InnerText;

                            if (email != null)
                            { JournalistProfile.Add("email:" + email); }
                            else
                            { JournalistProfile.Add("email:" + null); }

                            if (name != null)
                            { JournalistProfile.Add("name:" + name); }
                            else
                            { JournalistProfile.Add("name:" + null); }
                        }
                        HtmlNode TwitterIcon = ListIconNode.SelectSingleNode("//a[@class='author icon twitter']");

                        if (TwitterIcon != null)
                        {
                            var TwitterUrl = TwitterIcon.Attributes["href"].Value;
                            JournalistProfile.Add("Media link:" + TwitterUrl);
                            JournalistProfile.AddRange(GetTwitterProfile(TwitterUrl));
                        }
                        else { JournalistProfile.Add("Media link:" + null); }

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

                            HtmlWeb web2 = new HtmlWeb();
                            var document2 = web2.Load(authorBioInfoUrl);
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

                                            if (ContactItem.Contains("twitter.com"))
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
                                            else
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
                        HtmlNode EmailIcon = ListIconNode.SelectSingleNode("//a[@class='author icon email']");
                        if (EmailIcon != null)
                        {
                            var email = FetchEmaiId(EmailIcon.Attributes["href"].Value);
                            JournalistProfile.Add("email:" + email);
                            b = true;
                        }
                        else { JournalistProfile.Add("email:" + null); }

                        HtmlNode TwitterIcon = ListIconNode.SelectSingleNode("//a[@class='author icon twitter']");

                        if (TwitterIcon != null)
                        {
                            var TwitterUrl = TwitterIcon.Attributes["href"].Value;
                            JournalistProfile.AddRange(GetTwitterProfile(TwitterUrl));
                            JournalistProfile.Add("Media link:" + TwitterUrl);
                        }
                        else
                        { JournalistProfile.Add("Media link:" + null); }


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

        /// <summary>
        /// //FETCH THE "twitter" PROFILE INFO OF A REPORTER.
        /// </summary>
        /// <param name="twitterAcUrl"></param>
        /// <returns></returns>
        public static List<string> GetTwitterProfile(string twitterAcUrl)
        {
            List<string> twitterProfiles = new List<string>();
            twitterProfiles.Clear();
            HtmlWeb Twitter = new HtmlWeb();

            try
            {

                var Twitterdocument = Twitter.Load(twitterAcUrl);

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
                    if (BioInfoNode.Name == "a")
                    {
                        var nameInfo = BioInfoNode.SelectSingleNode("//a[@class='ProfileHeaderCard-nameLink u-textInheritColor js-nav']");
                        var name = nameInfo.InnerText;

                        if (name != null)
                        { twitterProfiles.Add("name:" + name); }
                        else
                        { twitterProfiles.Add("name:" + null); }

                    }

                    if (BioInfoNode.Name == "div")
                    {
                        if (BioInfoNode.Attributes["class"].Value == "ProfileHeaderCard-location")
                        {
                            var Location = BioInfoNode.ChildNodes[3].InnerText;
                            if (Location != null)
                            { twitterProfiles.Add("Loc:" + Location); }
                            else
                            { twitterProfiles.Add("Loc:" + null); }

                        }
                    }
                }
            }
            catch (Exception)
            { }


            return twitterProfiles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datatableReorter"></param>
        public static void ExportToLogFile(DataTable datatableReorter)
        {
            StreamWriter sw = null;
            try
            {
                string fileName = "news_" + DateTime.Today.ToString("d MMM yyyy") + ".log";

                var dir = @"D:\GNCTest\";  // folder location

                if (!Directory.Exists(dir))  // if it doesn't exist, create
                    Directory.CreateDirectory(dir);

                //if (!File.Exists(fileName))
                {
                    using (FileStream fs = new FileStream(dir + fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {

                        if (sw != null)
                        {
                            sw.Close();
                        }
                        using (sw = new StreamWriter(fs))
                        {
                            int i;

                            sw.Write(Environment.NewLine);
                            foreach (DataRow row in datatableReorter.Rows)
                            {
                                object[] array = row.ItemArray;
                                for (i = 0; i < array.Length - 1; i++)
                                {
                                    if (string.IsNullOrEmpty(array[i].ToString()))
                                    {
                                        array[i] = "-";
                                    }
                                    sw.Write(array[i].ToString().PadRight(25, ' '));
                                }
                                sw.WriteLine(array[i].ToString());
                                // sw.WriteLine("{0} \t {1} \t {2}", array[i].ToString().PadRight(10, ' '));


                                sw.Flush();

                                //fs.Close();
                            }
                            sw.Close();
                        }
                    }

                }
            }
            catch (Exception)
            { }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        public static bool SaveJournalist(DataTable dtJournalist)
        {
            bool b = false;
            try
            {
                DataAccess objDataAccess = new DataAccess();

                // string query = "INSERT INTO Journalist(JournalistName,JournalistTitle,JournalistBeats,JournalistSubBeats,JournalistLocation,JournalistEmail,JournalistSMLinks,JournalistWebsite,JournalistAssociation,JournalistBioInfo,PublicationDate,PublicationTitle) VALUES('" + dtJournalist.Rows[0]["name"] + "','" + dtJournalist.Rows[0]["ReporterTitle"] + "','" + dtJournalist.Rows[0]["Beat"] + "','" + dtJournalist.Rows[0]["SubBeat"] + "','" + dtJournalist.Rows[0]["Location"] + "','" + dtJournalist.Rows[0]["Email"] + "','" + dtJournalist.Rows[0]["MediaLinks"] + "','" + dtJournalist.Rows[0]["Website"] + "','" + dtJournalist.Rows[0]["Accosiation"] + "','" + dtJournalist.Rows[0]["AboutInfo"] + "','" + dtJournalist.Rows[0]["PubDate"] + "','" + dtJournalist.Rows[0]["NewsTitle"] + "')";
                if (objDataAccess.sqlExecute(dtJournalist, "Journalist"))
                {
                    b = true;
                }

            }
            catch (Exception)
            { }
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Beat"></param>
        /// <param name="subBeats"></param>
        public static bool SaveSubBeat(DataTable dtSubBeat)
        {
            bool b = false;
            try
            {
                DataAccess objDataAccess = new DataAccess();

                if (objDataAccess.sqlExecuteSubBeat(dtSubBeat, "SubBeat"))
                {
                    b = true;
                }

            }
            catch (Exception)
            { }
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
                var DuplicateElement = JournalistProfile.FindAll(x => x.StartsWith("email:"));
                JournalistProfile = JournalistProfile.Except(DuplicateElement).ToList();
                int DupNo = DuplicateElement.Count;

                if (DupNo > 1)
                {
                    DuplicateElement.RemoveRange(1, DupNo - 1);
                }
                JournalistProfile.Insert(0, DuplicateElement[0]);
            }
            catch (Exception)
            { }

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
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BeatName"></param>
        /// <returns></returns>
        public static int GetBeatId(string BeatName)
        {
            //int Beatid = 0;
            //DataAccess objDataAccess = new DataAccess();

            //try
            //{
            //    string query = "SELECT BeatIId FROM Beat WHERE BeatTopic='" + BeatName + "'";
            //    DataSet dsBeatId = objDataAccess.getdata("", query);
            //    Beatid = Convert.ToInt32(dsBeatId.Tables[0].Rows[0]["BeatIId"]);
            //}
            //catch (Exception)
            //{ }
        
           // return Beatid;
            int Beatid = 0;
            var Beat = (Beats)Enum.Parse(typeof(Beats), BeatName);

            switch (Beat)
            {
                case Beats.Business: return Beatid = 1;
                    break;
                case Beats.Entertainment: return Beatid = 2;
                    break;
                case Beats.Sports: return Beatid = 3;
                    break;
                case Beats.Technology: return Beatid = 4;
                    break;
                case Beats.Science: return Beatid = 5;
                    break;
                case Beats.Health: return Beatid = 6;
                    break;
                case Beats.Spotlight: return Beatid = 7;
                    break;
                case Beats.Elections: return Beatid = 8;
                    break;
                case Beats.World: return Beatid = 9;
                    break;
                case Beats.Local: return Beatid = 10;
                    break;
                default:
                    break;
           }
            return Beatid;
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
            table.Columns.Add("Website", typeof(string));
            table.Columns.Add("Association", typeof(string));
            table.Columns.Add("AboutInfo", typeof(string));
            table.Columns.Add("ArticleId", typeof(int));
            table.Columns.Add("PublicationDate", typeof(DateTime));
            table.Columns.Add("ArticleTitle", typeof(string));
            
          
            table.PrimaryKey = new DataColumn[] { table.Columns["Email"] };
            return table;
        }

        /// <summary>
        /// This example method generates a DataTable.
        /// </summary>
        public static DataTable GetSubBeatTable()
        {
            DataTable table = new DataTable();

           // table.Columns.Add("JournalistId", typeof(int));
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
            return HTMlString.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&").Replace("&nbsp;", "").Replace("&raquo;", "").Replace("\r", "").Replace("\n", "").Replace("'", "");
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
        public static bool IsValidSubBeat(string Text)
        {
            bool b = false;
            try
            {
                string path = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
                CRFClassifier Classifier = CRFClassifier.getClassifierNoExceptions(path + @"\stanford-ner-2013-06-20\stanford-ner-2013-06-20\classifiers\english.all.3class.distsim.crf.ser.gz");

                var classified = Classifier.classifyToCharacterOffsets(Text).toArray();

                int length = classified.Length;

                //If the subbeat is a personal nameor location ,the 'length' become 1.so length become 'zero' means this is a valid subbeat.
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
            catch (Exception)
            { }

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
                var toRemove = new string[] { "Beat", "PublicationDate", "ArticleTitle", "ArticleId" };

                foreach (string col in toRemove)
                    dtJournalist.Columns.Remove(col);

            }
            catch (Exception)
            { }
         
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
                var toRemove = new string[] { "JournalistId", "newsId", "guidText", "channelId", "BeatLink", "cid" };

                foreach (string col in toRemove)
                    dtSubBeatTb.Columns.Remove(col);

            }
            catch (Exception)
            { }

            return dtSubBeatTb;
        }
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtCrawler"></param>
        /// <returns></returns>
        public static DataTable CreateArticleRecords(DataTable dtCrawler, List<int> JournalistIds, List<int> SubBeatIds,DataTable dtSubBeats)
        {
            DataTable dtArticle = new DataTable();

            try
            {
                dtArticle = dtCrawler.Copy();
                var toRemoveFromCrawler = new string[] { "JournalistId", "Name", "Designation", "Beat", "Tags", "state", "country", "Email", "MediaLink", "Website", "Association", "AboutInfo", };

                foreach (string col in toRemoveFromCrawler)
                    dtArticle.Columns.Remove(col);
                dtArticle = dtSubBeats.Copy();

                var toRemoveFromSubBeat = new string[] { "SubBeatTopic", "newsId", "BeatId", "guidText", "channelId", "BeatLink", "cid" };
                foreach (string col in toRemoveFromSubBeat)
                    dtArticle.Columns.Remove(col);
            }
            catch (Exception)
            { }
                
            return dtArticle;
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
                HtmlWeb web = new HtmlWeb();
                var document = web.Load(beatUrl);
                HtmlNode node = document.DocumentNode.SelectSingleNode("//div[@cid='" + clusterid + "' and @class='story anchorman-blended-story esc esc-has-thumbnail '] ");
                HtmlNodeCollection child1nodeCollection = node.ChildNodes;



                foreach (HtmlNode child1 in child1nodeCollection)
                {
                    //HtmlNode child2Node=child1.SelectSingleNode("//div");
                    HtmlNodeCollection child2nodeCollection = child1.ChildNodes;

                    foreach (HtmlNode child3 in child2nodeCollection)
                    {
                        // HtmlNode child3Node=child3.SelectSingleNode("//div[@class='esc-body']");
                        HtmlNodeCollection WrapperNodes = child3.ChildNodes;
                        foreach (HtmlNode Wrappernode in WrapperNodes)
                        {
                            HtmlNode expandableWrapperNode = Wrappernode.SelectSingleNode("//div[@class='esc-default-layout-wrapper esc-expandable-wrapper']");
                            HtmlNodeCollection expandableWrapperNodechilds = expandableWrapperNode.ChildNodes;
                            foreach (HtmlNode expandableWrapperNodechild in expandableWrapperNodechilds)
                            {
                                HtmlNode table = expandableWrapperNodechild;

                                foreach (HtmlNode row in table.SelectNodes("//tr"))
                                {
                                    HtmlNode cellNode = row.SelectSingleNode("//td[@class='esc-layout-article-cell']");
                                    HtmlNodeCollection cellNodeChilds = cellNode.ChildNodes;

                                    foreach (HtmlNode div in cellNodeChilds)
                                    {
                                        // HtmlNode cellNode = div.SelectSingleNode("//td[@class='esc-layout-article-cell']");
                                        HtmlNode divNode = div.SelectSingleNode("//div[@class='esc-extension-wrapper']");
                                        HtmlNodeCollection TopicWrapperChildNodes = divNode.ChildNodes;


                                        foreach (HtmlNode wrapperNode in TopicWrapperChildNodes)
                                        {
                                            HtmlNode mediaStripNode = wrapperNode.SelectSingleNode(@"//div[@cid='" + clusterid + "' and @class='media-strip'] ");

                                            if (mediaStripNode != null)
                                            {
                                                HtmlNode InlineTopicWrapperNode = mediaStripNode.PreviousSibling;

                                                // HtmlNode InlineTopicWrapperNode = wrapperNode.SelectSingleNode("//div[@class='esc-inline-topics-wrapper']");

                                                //Have SubBeats
                                                if (InlineTopicWrapperNode != null)
                                                {
                                                    HtmlNodeCollection InlineTopicWrapperChilds = InlineTopicWrapperNode.ChildNodes;

                                                    foreach (HtmlNode InlineTopicWrapperChild in InlineTopicWrapperChilds)
                                                    {
                                                        HtmlNode TopicWrapperNode = InlineTopicWrapperChild.SelectSingleNode("//div[@class='esc-topics-wrapper']");
                                                        HtmlNodeCollection TopicWrapperNodeChilds = TopicWrapperNode.ChildNodes;

                                                        foreach (HtmlNode TopicWrapperNodeChild in TopicWrapperNodeChilds)
                                                        {
                                                            // HtmlNodeCollection spanCollection = TopicWrapperNodeChild.SelectNodes("//span[@class='esc-topic-wrapper']");
                                                            if (TopicWrapperNodeChild.Attributes["class"].Value == "esc-topic-wrapper")
                                                            {
                                                                var topic = UnescapeHTMLValue(TopicWrapperNodeChild.InnerText);
                                                                var b = IsValidSubBeat(topic);

                                                                if (b == true)
                                                                {
                                                                    SubBeatsList.Add(topic);
                                                                }

                                                            }

                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            //break;
                                        }
                                        break;
                                    }
                                    break;
                                }
                                break;
                            }
                            break;
                        }
                        break;
                    }
                    break;
                }

            }
            catch (Exception)
            { }

            return SubBeatsList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TbName"></param>
        /// <returns></returns>
        public static int GetIdsOfInsertedRecord(string TbName)
        {
            int LastRecId=0 ;
           
            DataAccess objDataAccess=new DataAccess();

            try
            {
                string query = "SELECT IDENT_CURRENT('" + TbName + "') as Id";
                var item = objDataAccess.getdata(TbName, query);
                if (item != null)
                {
                    LastRecId = Convert.ToInt32(item.Tables[0].Rows[0]["Id"]);
                }

            }
            catch (Exception)
            { }
           
            return LastRecId;
        }
      
    }
}
