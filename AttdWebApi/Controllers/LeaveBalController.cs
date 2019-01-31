using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;
using AttdWebApi.Models;


namespace AttdWebApi.Controllers
{
    public class LeaveBalController : ApiController
    {


        [HttpGet]

        public string Get()
        {
            return "Welcome To Attendance Web API";
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

        

    }
}
