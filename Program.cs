using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GNCService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            DataAccess objDataAccess = new DataAccess();
            String str = System.Configuration.ConfigurationManager.ConnectionStrings["const"].ToString();
            objDataAccess.setSqlconnectionstring(str);
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new Scheduler() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
