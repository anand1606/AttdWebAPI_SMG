using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;

namespace AttdWebApi.Models
{
    public class clsLeaveBal
    {
        public string LeaveType { get; set; }
        public Decimal OpenBal { get; set; }
        public Decimal Availed { get; set; }
        public Decimal Balance { get; set; }
        public decimal Encashed { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class clsResult
    {
        public string Info { get; set; }
        public string Err { get; set; }
        public bool Result { get; set; }
    }

   
    public class clsLeavePost
    {
        public int AppID { get; set; }
        public string EmpUnqID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string LeaveTyp { get; set; }
        public bool HalfDay { get; set; }
        public bool PostedFlg { get; set; }
        public string AttdUser {get;set;}
        public string Remarks {get;set;}
        public string ERROR { get; set; }
    }


    public class LeaveDaysDetails
    {
        public decimal WoDays { get; set; }
        public decimal HLDays { get; set; }
        public decimal TotDays { get; set; }
        public decimal LeaveDays { get; set; }
        public bool WOEntReq { get; set; }
        public bool IsHalf { get; set; }

        public LeaveDaysDetails()
        {
            WoDays = 0;
            HLDays = 0;
            TotDays = 0;
            LeaveDays = 0;
            WOEntReq = false;
            IsHalf = false;

        }
    }

    public class Utils
    {
        public static string cnstr = "Data Source=172.16.12.47; Initial Catalog=Attendance;User ID=sa;Password=testomonials@123";
        //public static string cnstr = "Data Source=172.16.12.14; Initial Catalog=KOSI_Attendance;User ID=sa;Password=testomonials@123";
        
        public static List<clsLeaveBal> GetLeaveBal(int EmpUnqID, int tYear , string tLeaveType)
        {
            List<clsLeaveBal> t = new List<clsLeaveBal>();

            string err = string.Empty;
            
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = cnstr;
                try
                {
                    cn.Open();
                    string sql = string.Empty;

                    if (!string.IsNullOrEmpty(tLeaveType))
                    {
                        sql = "Select Count(*) From MastLeave where CompCode = '01' and WrkGrp = 'COMP' AND LeaveTyp = '" + tLeaveType + "'";

                        string err2 = string.Empty;
                        int tcnt = Convert.ToInt32(Utils.GetDescription(sql, cnstr, out err2));
                        if (tcnt <= 0)
                        {
                            return t;
                        }
                        
                    }

                    

                    sql = "Select LeaveTyp, Opn,avl,Enc from [LeaveBal] where tyear = " + tYear.ToString() + " and EmpUnqID ='" + EmpUnqID.ToString() + "' Order By LeaveTyp";
                    
                    if (!string.IsNullOrEmpty(tLeaveType))
                    {
                        sql = "Select LeaveTyp, Opn,avl,Enc from [LeaveBal] where tyear = " + tYear.ToString() + " and EmpUnqID ='" + EmpUnqID.ToString() + "' and LeaveTyp = '" + tLeaveType + "' Order By LeaveTyp";
                   
                    }

                     
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    SqlDataReader rdr ;
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        clsLeaveBal t1 = new clsLeaveBal();            
                        t1.LeaveType = rdr["LeaveTyp"].ToString();
                        t1.OpenBal = Convert.ToDecimal(rdr["Opn"].ToString());
                        t1.Availed = Convert.ToDecimal(rdr["avl"].ToString());
                        t1.Encashed = Convert.ToDecimal(rdr["enc"].ToString());
                        t1.Balance = t1.OpenBal - (t1.Availed + t1.Encashed);
                        t1.IsBalanced = true;
                        t.Add(t1);

                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {

                }

                if (t.Count > 0)
                {
                    foreach (clsLeaveBal tmp in t)
                    {
                        string sql = "Select KeepBal From MastLeave where CompCode = '01' and WrkGrp = 'COMP' AND LeaveTyp = '" + tmp.LeaveType + "'";
                        string err2 = string.Empty;
                        Boolean tcnt = Convert.ToBoolean(Utils.GetDescription(sql, cnstr, out err2));
                        tmp.IsBalanced = tcnt;
                    }
                }
                else
                {
                    clsLeaveBal t1 = new clsLeaveBal();
                    t1.LeaveType = tLeaveType;
                    t1.OpenBal = 0;
                    t1.Availed = 0;
                    t1.Encashed = 0;
                    t1.Balance = 0;
                    t1.IsBalanced = false;
                    t.Add(t1);
                }
                
            }

            return t;
        }

        public static DataSet GetData(string sql, string ConnectionString, out string err)
        {
            err = string.Empty;
            DataSet Result = new DataSet();
            if (string.IsNullOrEmpty(sql))
            {
                err = "Query is not defined";
                return Result;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                err = "Connection String is not defined";
                return Result;
            }


            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlCommand command = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            SqlDataAdapter da = new SqlDataAdapter();


            try
            {
                conn.Open();
                command.ExecuteNonQuery();
                da.SelectCommand = command;
                da.Fill(Result, "RESULT");
                conn.Close();
            }
            catch (SqlException ex) { err = ex.Message.ToString(); }
            catch (Exception ex) { err = ex.Message.ToString(); }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }

            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql">Select Single Column with single row result</param>
        /// <param name="ConnectionString">sql connection</param>
        /// <returns>string</returns>
        public static string GetDescription(string sql, string ConnectionString, out string err)
        {
            object result;
            err = string.Empty;

            string returndesc = string.Empty;
            if (string.IsNullOrEmpty(sql))
            {
                return returndesc;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                return returndesc;
            }

            if (sql.Contains("insert"))
            {
                return returndesc;
            }
            if (sql.Contains("update"))
            {
                return returndesc;
            }
            if (sql.Contains("delete"))
            {
                return returndesc;
            }

            if (!sql.Contains("TOP 1"))
            {
                sql = sql.ToUpper().Replace("SELECT", "SELECT TOP 1 ");
            }

            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlCommand command = new SqlCommand(sql, conn) { CommandType = CommandType.Text };


            try
            {
                conn.Open();
                command.CommandTimeout = 1500;
                result = command.ExecuteScalar();

                if (result != null)
                    returndesc = Convert.ToString(result);

                conn.Close();
            }
            catch (SqlException ex) { err = ex.Message.ToString(); }
            catch (Exception ex) { err = ex.Message.ToString(); }
            finally
            {
                conn.Close();
            }

            return returndesc;
        }

        public static string LeaveDataValidate(clsLeavePost t)
        {
           
            string err = string.Empty;

            if (string.IsNullOrEmpty(t.EmpUnqID.ToString()))
            {
                err = err + "EmpUnqID required" + Environment.NewLine;
                return err;
            }

            string sql = "Select Active from MastEmp where CompCode = '01' and WrkGrp = 'Comp' and EmpUnqID ='" + t.EmpUnqID + "'";
            string result = GetDescription(sql, cnstr,out err);
            if (!string.IsNullOrEmpty(err))
            {
                return err;
            }

            if (string.IsNullOrEmpty(result))
            {
                err += "Invalid Employee : in Data Validate";
                return err;
            }

            if (t.FromDate == DateTime.MinValue)
            {
                err += "Invalid From Date : in Data Validate";
                return err;
            }

            if (t.ToDate == DateTime.MinValue)
            {
                err += "Invalid To Date : in Data Validate " ;
                return err;
            }

            if (t.FromDate > t.ToDate)
            {
                err += "Invalid Date Range : in Data Validate";
                return err;
            }

            if (t.ToDate.Year != t.FromDate.Year)
            {
                err += "Invalid Date Range : Cross Year Date Posting not allowed";
                return err;
            }

            
            #region Chk_AlreadyPosted
            sql = "Select * from LeaveEntry Where " +
           " compcode = '01'" +
           " and WrkGrp ='COMP'" +
           " And tYear ='" + t.FromDate.Year + "'" +
           " And EmpUnqID='" + t.EmpUnqID + "'" +          
           " And (     FromDt between '" + t.FromDate.ToString("yyyy-MM-dd") + "' And '" + t.ToDate.ToString("yyyy-MM-dd") + "' " +
           "  OR       ToDt Between '" + t.FromDate.ToString("yyyy-MM-dd") + "'   And '" + t.ToDate.ToString("yyyy-MM-dd") + "' " +
           "  OR '" + t.FromDate.ToString("yyyy-MM-dd") + "' Between FromDt And ToDt " +
           "  OR '" + t.ToDate.ToString("yyyy-MM-dd") + "' Between FromDt And ToDt " +
           "     ) ";

            string err2;
            DataSet ds = Utils.GetData(sql, Utils.cnstr,out err2);
            bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            err += err2;
            if (hasRows)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                err += "Some Leave Already Posted : in Data Validate";                
                return err;
            }
            #endregion


            #region Chk_ValidLeaveTyp

            sql = "Select * from MastLeave where " +
               " compcode = '01'" +
               " and WrkGrp ='comp'" +
               " and LeaveTyp ='" + t.LeaveTyp + "'";

            ds = Utils.GetData(sql, Utils.cnstr,out err2);
            err += err2;
            hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);

            if (!hasRows)
            {
                err += "Invalid Leave Type : in Data Validate";
                return err;
            }


            List<clsLeaveBal> tlstbal = Utils.GetLeaveBal(Convert.ToInt32(t.EmpUnqID), t.FromDate.Year, t.LeaveTyp);
            foreach (clsLeaveBal tbal in tlstbal)
            {
                if (tbal.IsBalanced && tbal.Balance <= 0)
                {
                    err += "Balance not found : in Data Validate";
                    return err;
                }
            }

            


            #endregion


            return err;
        
        }

        public static LeaveDaysDetails GetLeaveDaysDetails(clsLeavePost tPost)
        {
            LeaveDaysDetails t1 = new LeaveDaysDetails();

            DateTime FromDt = tPost.FromDate, ToDt = tPost.ToDate;
            decimal TotDays = 0, WODayNo = 0, HLDay = 0;
            bool halfflg = false;

            halfflg = tPost.HalfDay;

            TimeSpan ts = (tPost.ToDate - tPost.FromDate);
            TotDays = ts.Days + 1;

            string err = string.Empty;
            string WOSql = "Select Count(*) From AttdData where CompCode = '01' and WrkGrp = 'Comp' and ScheduleShift ='WO' and  tDate between '" + tPost.FromDate.ToString("yyyy-MM-dd") + "' And '" + ToDt.ToString("yyyy-MM-dd") + "'" +
            " And EmpUnqID='" + tPost.EmpUnqID + "'";

            WODayNo = Convert.ToDecimal(Utils.GetDescription(WOSql, Utils.cnstr,out err));


            string hlsql = "Select tDate from HoliDayMast Where " +
             " CompCode = '01' " +
             " And WrkGrp ='COMP'" +
             " And tDate between '" + tPost.FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDt.ToString("yyyy-MM-dd") + "' ";

            //'check hlDay on WeekOff...

            HLDay = 0;
            DataSet ds = Utils.GetData(hlsql, Utils.cnstr,out err);
            bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRows)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //Get  from AttdData Table , if ScheduleShift = "WO" on Holiday
                    WOSql = "Select ScheduleShift from AttdData Where EmpUnqId ='" + tPost.EmpUnqID + "' and tDate ='" + Convert.ToDateTime(dr["tDate"]).ToString("yyyy-MM-dd") + "'";
                    string WODay = Utils.GetDescription(WOSql, Utils.cnstr,out err);
                    if (WODay == "WO")
                    {
                        WODayNo -= 1;
                    }
                    HLDay += 1;
                }
            }

            t1.TotDays = TotDays;
            if (tPost.LeaveTyp.Trim() == "AB" || tPost.LeaveTyp.Trim() == "LW" || tPost.LeaveTyp.Trim() == "SP" || tPost.LeaveTyp.Trim() == "OH")
            {
                WODayNo = 0;
                HLDay = 0;
            }

            t1.WoDays = WODayNo;
            t1.HLDays = HLDay;

            if (halfflg)
            {
                t1.IsHalf = true;
                t1.LeaveDays = (TotDays - (WODayNo + HLDay)) / 2;
            }
            else
            {
                t1.IsHalf = false;
                t1.LeaveDays = (TotDays - (WODayNo + HLDay));
            }

            return t1;
        }

        public static bool GetWrkGrpRights(int Formid, string WrkGrp, string EmpUnqID, string GUserID)
        {
            bool returnval = false;

            DataSet ds = new DataSet();
            string err2;
            if (EmpUnqID != "")
            {
                
                WrkGrp = Utils.GetDescription("Select WrkGrp From MastEmp Where EmpUnqID ='" + EmpUnqID + "'", Utils.cnstr,out err2);
            }

            if (WrkGrp == "" && EmpUnqID == "")
            {
                return false;
            }


            string wkgsql = "Select * from UserSpRight where UserID = '" + GUserID + "' and FormID = '" + Formid.ToString() + "' and WrkGrp = '" + WrkGrp + "' and Active = 1";


            ds = Utils.GetData(wkgsql, Utils.cnstr,out err2);
            bool hasRows = ds.Tables.Cast<DataTable>()
                           .Any(table => table.Rows.Count != 0);

            if (hasRows)
            {
                returnval = true;
            }
            else
            {
                returnval = false;
            }

            return returnval;
        }

        public static DateTime GetSystemDateTime()
        {
            DateTime dt = new DateTime();
            string err2;
            DataSet ds = new DataSet();
            string sql = "Select GetDate() as CurrentDate ";
            ds = Utils.GetData(sql, Utils.cnstr,out err2);
            bool hasRows = ds.Tables.Cast<DataTable>()
                           .Any(table => table.Rows.Count != 0);

            dt = DateTime.Now;
            if (hasRows)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    dt = (DateTime)dr["CurrentDate"];
                }

            }

            return dt;
        }

        public static bool ReconsileLeaveBal(string tEmpUnqID, string tLeaveTyp, string PostBy, int tYear,out string err)
        {
            bool result = false;


            string sql = "Select * from LeaveBal Where tYear = '" + tYear.ToString() + "' and EmpUnqID ='" + tEmpUnqID.Trim() + "' and LeaveTyp ='" + tLeaveTyp + "'";

            //string sql = "Select * from LeaveBal Where tYear= 2018 and CompCode = '01' and WrkGrp = 'Comp' and EmpUnqID in (Select EmpUnqID From MastEmp Where Active = 1 and WrkGrp = 'Comp' and CompCode = '01')";
            err = string.Empty;

            DataSet ds = Utils.GetData(sql, Utils.cnstr, out err);

            if (!string.IsNullOrEmpty(err))
            {
                return result;
            }


            bool hasrow = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            
            if (hasrow)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {

                    double LeaveHalf = 0;
                    double LeaveFull = 0;
                    double LeaveAVL = 0;

                    string sql2 = "Select Count(*) from AttdData Where LeaveTyp ='" + dr["LeaveTyp"].ToString() + "' " +
                        " And tYear = '" + dr["tYear"].ToString() + "' And EmpUnqID ='" + dr["EmpUnqID"].ToString() + "'" +
                        " And CompCode = '" + dr["CompCode"].ToString() + "' And WrkGrp ='" + dr["WrkGrp"].ToString() + "' and LeaveHalf = 0";

                    LeaveFull = Convert.ToDouble(Utils.GetDescription(sql2, Utils.cnstr, out err));
                    if (!string.IsNullOrEmpty(err))
                    {
                        return result;
                    }

                    sql2 = "Select Count(*) from AttdData Where LeaveTyp ='" + dr["LeaveTyp"].ToString() + "' " +
                       " And tYear = '" + dr["tYear"].ToString() + "' And EmpUnqID ='" + dr["EmpUnqID"].ToString() + "'" +
                       " And CompCode = '" + dr["CompCode"].ToString() + "' And WrkGrp ='" + dr["WrkGrp"].ToString() + "' and LeaveHalf = 1";

                    LeaveHalf = Convert.ToDouble(Utils.GetDescription(sql2, Utils.cnstr, out err));
                    if (!string.IsNullOrEmpty(err))
                    {
                        return result;
                    }

                    if (LeaveHalf > 0)
                    {
                        LeaveHalf = LeaveHalf / 2;
                    }

                    LeaveAVL = LeaveFull + LeaveHalf;

                    using (SqlConnection cn = new SqlConnection(Utils.cnstr))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            try
                            {
                                cn.Open();
                                sql = "Update LeaveBal Set AVL ='" + LeaveAVL.ToString() + "', UpdDt = GetDate(), UpdID ='" + PostBy + "' Where " +
                                    " EmpUnqID = '" + dr["EmpUnqID"].ToString() + "' and " +
                                    " tYear ='" + dr["tYear"].ToString() + "' and " +
                                    " WrkGrp ='" + dr["WrkGrp"].ToString() + "' And " +
                                    " LeaveTyp='" + dr["LeaveTyp"].ToString() + "' and " +
                                    " CompCode ='" + dr["CompCode"].ToString() + "'";

                                cmd.Connection = cn;
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = sql;
                                cmd.ExecuteNonQuery();
                                result = true;

                            }
                            catch (Exception ex)
                            {
                                err = ex.Message;
                            }
                        }
                    }

                }//foreach
            }//if

            return result;
        }
    }

}