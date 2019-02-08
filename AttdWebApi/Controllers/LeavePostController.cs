using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using System.Data.SqlClient;
using AttdWebApi.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Configuration;

namespace AttdWebApi.Controllers
{
    public class LeavePostController : ApiController
    {
        [HttpGet]

        public string Get()
        {

            string response = "Welcome To Attendance LeavePost API";

            return response;
        }


        [HttpPost]
        public HttpResponseMessage PostLeave([FromBody] object leaveposting)
        {
            clsLeavePost tPost = JsonConvert.DeserializeObject<clsLeavePost>(leaveposting.ToString());
           
            tPost.ERROR = Utils.LeaveDataValidate(tPost);
            if (!string.IsNullOrEmpty(tPost.ERROR))
            {
                tPost.PostedFlg = false;
                return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                
            }

            LeaveDaysDetails LeaveDays = Utils.GetLeaveDaysDetails(tPost);

            string Location = tPost.Location;
            string strEmp = tPost.EmpUnqID.ToString();
            string strerr = string.Empty;
            string cnstr = string.Empty;
            
            try
            {
                cnstr = ConfigurationManager.ConnectionStrings["cn" + Location].ConnectionString;
            }
            catch (Exception ex)
            {
                tPost.ERROR = "Could not build Location Connection.." + ex.Message;
                return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
            }



            //bool trecosile = Utils.ReconsileLeaveBal(strEmp, tPost.LeaveTyp, "LeaveAPI", tPost.FromDate.Year, out strerr);

            List<clsLeaveBal> tlstbal = Utils.GetLeaveBal(Convert.ToInt32(tPost.EmpUnqID), tPost.FromDate.Year, tPost.LeaveTyp,Location);
            if (tlstbal.Count > 0)
            {
                clsLeaveBal tBal = tlstbal[0];
                if (tBal.IsBalanced && (tBal.Balance - LeaveDays.LeaveDays) < 0)
                {
                    tPost.ERROR += "InSufficient Balance...";
                    tPost.PostedFlg = false;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                }
            }
            else
            {
                tPost.ERROR += "Leave Balance Record not found...";
                tPost.PostedFlg = false;
                return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
            }

            #region Chk_WoReqEnt
            switch (tPost.LeaveTyp)
            {
                case "LW":
                    LeaveDays.WOEntReq = false;
                    break;
                case "AB":
                    LeaveDays.WOEntReq = false;
                    break;
                case "SP":
                    LeaveDays.WOEntReq = false;
                    break;
                case "OH":
                    LeaveDays.WOEntReq = false;
                    break;
                default:
                    LeaveDays.WOEntReq = true;
                    break;
            }
            #endregion

            #region MainProc
            using (SqlConnection cn = new SqlConnection(cnstr))
            {
                try
                {
                    cn.Open();
                }
                catch (Exception ex)
                {
                    tPost.ERROR += ex.Message.ToString();
                    tPost.PostedFlg = false;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                }

                SqlTransaction tr = cn.BeginTransaction();
                string sql = "Select * from LeaveEntry Where " +
                   " compcode = '01'" +
                   " and WrkGrp ='COMP'" +
                   " and LeaveTyp ='" + tPost.LeaveTyp + "'" +
                   " And tYear ='" + tPost.FromDate.Year + "'" +
                   " And EmpUnqID='" + tPost.EmpUnqID + "'" +
                   " And FromDt ='" + tPost.FromDate.ToString("yyyy-MM-dd") + "'" +
                   " and ToDt ='" + tPost.ToDate.ToString("yyyy-MM-dd") + "'";

                string err2 = string.Empty;
                DataSet ds = Utils.GetData(sql, cnstr,out err2);

                if (!string.IsNullOrEmpty(err2))
                {
                    tPost.ERROR += err2;
                    tPost.PostedFlg = false;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                }

                err2 = string.Empty;

                //advance leave not allowed thru api..

                bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (!hasRows)
                {
                    try
                    {
                        string remark = ((tPost.Remarks.Trim().Length > 100) ? tPost.Remarks.Trim().Substring(0, 100) : tPost.Remarks.Trim());
                        remark = remark.Replace("'", "");
                        remark = remark.Replace('"', ' ');
                        remark = remark.Replace('%', ' ');
                        remark = remark.Replace('&', ' ');

                        string insertsql = "insert into LeaveEntry (CompCode,WrkGrp,tYear,EmpUnqID,FromDt,ToDt," +
                            " LeaveTyp,TotDay,WoDay,PublicHL,LeaveDed,LeaveADV,LeaveHalf,Remark,AddID,AddDt,DelFlg) " +
                            " Values ('01','COMP','" + tPost.FromDate.Year.ToString() + "','" + tPost.EmpUnqID + "','" + tPost.FromDate.ToString("yyyy-MM-dd") + "','" + tPost.ToDate.ToString("yyyy-MM-dd") + "', " +
                            " '" + tPost.LeaveTyp + "','" + LeaveDays.TotDays.ToString() + "','" + LeaveDays.WoDays.ToString() + "','" + LeaveDays.HLDays.ToString() + "'," +
                            " '" + LeaveDays.LeaveDays.ToString() + "','0','" + (LeaveDays.IsHalf ? 1 : 0) + "','" + remark + "'," +
                            " '" + tPost.AttdUser + "',GetDate(),0)";

                        SqlCommand cmd = new SqlCommand(insertsql, cn, tr);
                        cmd.ExecuteNonQuery();

                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        tPost.ERROR += ex.Message.ToString();
                        tPost.PostedFlg = false;
                        return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
 
                    }
                }
                //gettting timeout
                SqlCommand cmd1 = new SqlCommand(sql, cn, tr);
                SqlDataReader sdr = cmd1.ExecuteReader();
                if (sdr.HasRows)
                {

                    //close datareader
                    if (sdr != null)
                    {
                        sdr.Close();
                    }


                    string datestr = "Select * from V_LV_USE where CAL_Date between '" + tPost.FromDate.ToString("yyyy-MM-dd") + "' and '" + tPost.ToDate.ToString("yyyy-MM-dd") + "'";

                    DataSet trsLV = Utils.GetData(datestr, cnstr,out err2);



                    //used for leave days count
                    decimal tmpleavecons = 0;
                    #region UpdateSchLeave
                    foreach (DataRow trslr in trsLV.Tables[0].Rows)
                    {
                        DateTime CalDt = Convert.ToDateTime(trslr["Cal_Date"]);

                        string sqlSch = "Select * from MastLeaveSchedule Where EmpUnqID ='" + tPost.EmpUnqID + "' And tdate ='" + CalDt.ToString("yyyy-MM-dd") + "'";

                        //create data adapter
                        DataSet dsSchLv = new DataSet();
                        SqlDataAdapter daSchLv = new SqlDataAdapter(new SqlCommand(sqlSch, cn, tr));
                        SqlCommandBuilder cmdbSchLv = new SqlCommandBuilder(daSchLv);

                        daSchLv.InsertCommand = cmdbSchLv.GetInsertCommand();
                        daSchLv.InsertCommand.Transaction = tr;
                        daSchLv.UpdateCommand = cmdbSchLv.GetUpdateCommand();
                        daSchLv.UpdateCommand.Transaction = tr;
                        daSchLv.DeleteCommand = cmdbSchLv.GetDeleteCommand();
                        daSchLv.DeleteCommand.Transaction = tr;
                        daSchLv.AcceptChangesDuringUpdate = false;

                        daSchLv.Fill(dsSchLv, "MastLeaveSchedule");
                        hasRows = dsSchLv.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);

                        if (hasRows)
                        {

                            try
                            {


                                string sqldel = "Delete From MastLeaveSchedule Where " +
                                " EmpUnqID='" + tPost.EmpUnqID + "' " +
                                " And tDate ='" + CalDt.ToString("yyyy-MM-dd") + "'" +
                                " And SchLeave='" + tPost.LeaveTyp + "' and WrkGrp ='COMP' AND  ConsInTime is null and ConsOutTime is null " +
                                " And ConsOverTime is null and ConsShift is null ";

                                SqlCommand cmd = new SqlCommand(sqldel, cn, tr);
                                int t = (int)cmd.ExecuteNonQuery();

                                sqldel = "Update MastLeaveSchedule Set SchLeave = null, SchLeaveHalf = 0,SchLeaveAdv = 0 Where " +
                                " EmpUnqID='" + tPost.EmpUnqID + "' " +
                                " And tDate ='" + CalDt.ToString("yyyy-MM-dd") + "'" +
                                " And SchLeave='" + tPost.LeaveTyp + "' and WrkGrp ='COMP' ";

                                SqlCommand cmd2 = new SqlCommand(sqldel, cn, tr);
                                t = (int)cmd2.ExecuteNonQuery();




                            }
                            catch (Exception ex)
                            {
                                tr.Rollback();
                                tr.Dispose();
                                tPost.ERROR += ex.Message.ToString();
                                tPost.PostedFlg = false;
                                return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                               
                            }
                        }

                        dsSchLv.Clear();
                        daSchLv.Fill(dsSchLv, "MastLeaveSchedule");

                        DataRow drSch;
                        hasRows = dsSchLv.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                        if (!hasRows)
                        {
                            try
                            {
                                string tsql3 = "Insert into MastLeaveSchedule (EmpUnqId,WrkGrp,tDate,AddDt,AddID) Values (" +
                                    "'" + tPost.EmpUnqID + "','COMP','" + CalDt.ToString("yyyy-MM-dd") + "',GetDate(),'" + tPost.AttdUser + "')";
                                SqlCommand cmd = new SqlCommand(tsql3, cn, tr);
                                cmd.ExecuteNonQuery();

                                dsSchLv.Clear();
                                daSchLv.Fill(dsSchLv, "MastLeaveSchedule");
                                drSch = dsSchLv.Tables["MastLeaveSchedule"].Rows[0];
                            }
                            catch (Exception ex)
                            {
                                tr.Rollback();
                                tr.Dispose();
                                tPost.ERROR += ex.Message.ToString();
                                tPost.PostedFlg = false;
                                return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                            }

                        }
                        else
                        {
                            drSch = dsSchLv.Tables["MastLeaveSchedule"].Rows[0];
                        }

                        string PublicHLTyp = string.Empty;
                        string SchShift = string.Empty;
                        string tsql = string.Empty;
                        string err3 = string.Empty;
                        err2 = string.Empty;
                        err3 = string.Empty;

                        tsql = "Select PublicHLTyp from HolidayMast Where WrkGrp ='COMP' and tDate ='" + CalDt.ToString("yyyy-MM-dd") + "'";


                        PublicHLTyp = Utils.GetDescription(tsql, cnstr, out err2);
                        if(!string.IsNullOrEmpty(err2))
                        {
                            tr.Rollback();
                            tr.Dispose();
                            tPost.ERROR += err2;
                            tPost.PostedFlg = false;
                            return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                        }

                        tsql = "Select ScheduleShift from AttdData where  EmpUnqID ='" + tPost.EmpUnqID + "' " +
                            " And CompCode = '01' And WrkGrp = 'COMP' " +
                            " And tYear = '" + CalDt.Year.ToString() + "' And tDate ='" + CalDt.ToString("yyyy-MM-dd") + "'";

                        err2 = string.Empty;
                        SchShift = Utils.GetDescription(tsql, cnstr,out err2);
                        if (!string.IsNullOrEmpty(err2))
                        {
                            tr.Rollback();
                            tr.Dispose();
                            tPost.ERROR += err2;
                            tPost.PostedFlg = false;
                            return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                        }

                        if (LeaveDays.WOEntReq == false)
                        {
                            string sqldel = "Update MastLeaveSchedule Set SchLeave = null, SchLeaveHalf = 0,SchLeaveAdv = 0 Where " +
                                " EmpUnqID='" + tPost.EmpUnqID + "' " +
                                " And tDate ='" + CalDt.ToString("yyyy-MM-dd") + "'" +
                                " And SchLeave in ('WO','HL') and WrkGrp ='COMP' ";

                            SqlCommand cmd2 = new SqlCommand(sqldel, cn, tr);
                            int t = (int)cmd2.ExecuteNonQuery();


                            drSch["schLeave"] = tPost.LeaveTyp;
                            drSch["AddId"] = tPost.AttdUser;
                            drSch["AddDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdId"] = tPost.AttdUser;
                            tmpleavecons += 1;
                        }
                        else if (LeaveDays.WOEntReq && PublicHLTyp != "")
                        {
                            drSch["schLeave"] = PublicHLTyp;
                            drSch["AddId"] = "HLCal";
                            drSch["AddDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdId"] = tPost.AttdUser;
                        }
                        else if (LeaveDays.WOEntReq && PublicHLTyp == "" && drSch["SchLeave"].ToString() == "WO")
                        {
                            drSch["schLeave"] = "WO";
                            drSch["AddId"] = "ShiftSch";
                            drSch["UpdDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdId"] = tPost.AttdUser;
                        }
                        else if (LeaveDays.WOEntReq && PublicHLTyp == "" && SchShift == "WO")
                        {
                            drSch["schLeave"] = "WO";
                            drSch["AddId"] = "ShiftSch";
                            drSch["UpdDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdId"] = tPost.AttdUser;

                        }
                        else
                        {
                            drSch["schLeave"] = tPost.LeaveTyp;
                            drSch["AddId"] = tPost.AttdUser;
                            drSch["AddDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdDt"] = Utils.GetSystemDateTime(cnstr);
                            drSch["UpdId"] = tPost.AttdUser;
                            tmpleavecons += 1;
                        }

                        

                        drSch["SchLeaveHalf"] = (tPost.HalfDay ? 1 : 0);
                        //dsSchLv.Tables["MastLeaveSchedule"].Rows.Add(drSch);
                        dsSchLv.AcceptChanges();

                        try
                        {
                            string sql2 = "Update MastLeaveSchedule Set AddId ='" + drSch["AddId"].ToString() + "'," +
                                " AddDt='" + Convert.ToDateTime(drSch["AddDt"]).ToString("yyyy-MM-dd HH:mm:ss") + "'," +
                                " UpdDt={0},UpdID={1}, SchLeave='" + drSch["SchLeave"].ToString() + "'," +
                                " SchLeaveAdv='" + (Convert.ToBoolean(drSch["SchLeaveAdv"]) ? 1 : 0).ToString() + "'," +
                                " SchLeaveHalf ='" + (tPost.HalfDay ? 1 : 0).ToString() + "' Where SanId = '" + drSch["SanID"].ToString() + "'";

                            sql2 = string.Format(sql2,
                                ((drSch["UpdDt"] == DBNull.Value) ? " null " : "'" + Convert.ToDateTime(drSch["UpdDt"]).ToString("yyyy-MM-dd HH:mm") + "'"),
                                (drSch["UpdID"] == DBNull.Value) ? " null " : "'" + tPost.AttdUser + "'"
                                );

                            SqlCommand cmd = new SqlCommand(sql2, cn, tr);
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            tr.Rollback();
                            tr.Dispose();
                            tPost.ERROR += ex.Message;
                            tPost.PostedFlg = false;
                            return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                        }

                    }//foreach dateloop - trsLV
                    #endregion


                    
                }

                try
                {


                    #region UpdateLeaveBal

                    using (SqlCommand cmd20 = new SqlCommand())
                    {
                        try
                        {
                            string tsql1 = "Update LeaveBal " +
                                " Set AVL = AVL + '" + LeaveDays.LeaveDays.ToString() + "'" +
                                " ,ADV = 0 " +
                                " ,UPDDT = GetDate(),UPDID = '" + tPost.AttdUser + "' " +
                                " Where " +
                                " CompCode ='01'" +
                                " And WrkGrp='COMP'" +
                                " And tYear ='" + tPost.FromDate.Year.ToString() + "'" +
                                " And EmpUnqID ='" + tPost.EmpUnqID + "'" +
                                " And LeaveTyp='" + tPost.LeaveTyp + "'";

                            cmd20.CommandType = CommandType.Text;
                            cmd20.CommandText = tsql1;
                            cmd20.Connection = new SqlConnection(cnstr);
                            cmd20.Connection.Open();
                            cmd20.ExecuteNonQuery();
                            cmd20.Connection.Close();
                        }
                        catch (Exception ex)
                        {
                           
                            tr.Rollback();
                            tr.Dispose();
                            tPost.ERROR += ex.Message;
                            tPost.PostedFlg = false;
                            return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                            
                        }
                    }

                    #endregion

                    tr.Commit();
                    tr.Dispose();

                }catch(Exception ex){

                    tr.Rollback();
                    tr.Dispose();
                    tPost.ERROR += ex.Message.ToString();
                    tPost.PostedFlg = false;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                }


                try
                {
                    //string err = string.Empty;
                    //bool t = ReconsileLeaveBal(tPost.EmpUnqID, tPost.LeaveTyp, tPost.AttdUser, out err);

                    #region ProcessData
                    string sql1 = "Insert into AttdWorker  ( EmpUnqId,FromDt,ToDt,WorkerId,DoneFlg,PushFlg,addid,ProcessType ) " +
                         " values ('" + tPost.EmpUnqID + "','" + tPost.FromDate.ToString("yyyy-MM-dd") + "','" + tPost.ToDate.ToString("yyyy-MM-dd") + "'," +
                         " '" + tPost.AttdUser + "',0,0,'" + tPost.AttdUser + "','ATTD' )";

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = sql1;
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }

                    sql1 = "Insert into AttdWorker  ( EmpUnqId,FromDt,ToDt,WorkerId,DoneFlg,PushFlg,addid,ProcessType ) " +
                         " values ('" + tPost.EmpUnqID + "','" + tPost.FromDate.ToString("yyyy-MM-dd") + "','" + tPost.ToDate.ToString("yyyy-MM-dd") + "'," +
                         " '" + tPost.AttdUser + "',0,0,'" + tPost.AttdUser + "','LUNCHINOUT' )";
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = sql1;
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }

                    #endregion

                    tPost.PostedFlg = true;
                }
                catch (Exception ex)
                {
                    tPost.ERROR += ex.Message.ToString();
                    tPost.PostedFlg = false;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, tPost);
                }
               
            }//end sqlcon using
            #endregion

            return Request.CreateResponse(HttpStatusCode.OK, tPost);
        }

       

    }
}
