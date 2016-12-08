using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Net;
using System.IO;
using System.Threading;
using System.Security.Permissions;

namespace GNCService
{
    public partial class Scheduler : ServiceBase
    {
        Thread thread;
        AutoResetEvent StopRequest = new AutoResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        public Scheduler()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
          
            Library.CrawlerEventInfoLog("GNC Service started. ");
            thread = new Thread(DoWork);
            thread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        { 
            KillTheThread();
            Library.CrawlerEventInfoLog("GNC Service stopped. ");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void DoWork(object arg)
        {
            CrawlingProcess:
            try
            {
                
                while (true)
                {
                    Library.Crawler();
                    Library.CrawlerEventInfoLog("Crawling paused for 10 sec. ");
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10)); // Then, wait for certain time interval, in this case 10 sec
                }
            }
            catch (Exception ex)
            {
                Library.CrawlerEventErrorLog("Crawling failed. " + ex.InnerException);
                goto CrawlingProcess;
            }
        
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void KillTheThread()
        {
            thread.Abort();
        }
    }
}
