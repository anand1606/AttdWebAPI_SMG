using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;
using AttdWebApi.Models;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Http.Headers;

namespace AttdWebApi.Controllers
{
    public class LeaveBalController : ApiController
    {


        [HttpGet]

        public string Get()
        {

            string response = "Welcome To Attendance LeavePost API";
            
            return response;
        }

        [HttpGet]

        public List<clsLeaveBal> Get(int Id,int year)
        {
            List<clsLeaveBal> t = AttdWebApi.Models.Utils.GetLeaveBal(Id, year,"");
            return t;
        }

        [HttpGet]

        public List<clsLeaveBal>  Leave(int Id, int year, string leavetype)
        {
            List<clsLeaveBal> t = AttdWebApi.Models.Utils.GetLeaveBal(Id, year, leavetype);
            return t;            
        }

        [HttpPost]
        public string SetDBConn(string server, string database, string userid, string password , string adminpass)
        {

            if (adminpass == "9737717776")
            {
                
                string str = "server=" + server + ";database=" + database + "; User ID=" + userid + "; Password=" + password + "";
               
                System.Configuration.Configuration Config1 = WebConfigurationManager.OpenWebConfiguration("~");
                ConnectionStringsSection conSetting = (ConnectionStringsSection)Config1.GetSection("connectionStrings");
                ConnectionStringSettings StringSettings = new ConnectionStringSettings("conn", "Data Source=" + server + ";Database=" + database + ";User ID=" + userid + ";Password=" + password + ";");
                conSetting.ConnectionStrings.Remove(StringSettings);
                conSetting.ConnectionStrings.Add(StringSettings);
                Config1.Save(ConfigurationSaveMode.Modified);

                return "dbconnection settled.";
            }
            else
            {
                return "Invalid Password";
            }
            
        }

    }
}
