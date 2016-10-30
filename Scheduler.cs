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
        Thread _thread;
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
            System.Diagnostics.Debugger.Launch();
            // Start the crawler thread
            _thread = new Thread(DoWork);
            _thread.Start();

        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        {
            KillTheThread();
            // Signal worker to stop and wait until it does
            //StopRequest.Set(); 
            //_thread.Join();
        }

        public void DoWork(object arg)
        {

            try
            {

                while (true)
                {
                    Console.WriteLine("task started");
                    // First, execute scheduled task
                    Library.Crawler();

                    // Then, wait for certain time interval, in this case 1 hour
                    Console.WriteLine("task completed.");
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception)
            { }
        
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void KillTheThread()
        {
            _thread.Abort();
        }
    }


}
