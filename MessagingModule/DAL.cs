using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagingModule;

namespace MessagingModule
{
    class DAL
    {
        private static string conn, strSql;
        private static SqlCommand sqlCommand;
        

        public DAL(string connectionString)
        {
            //conn = Getconnectionstring();
            conn = connectionString;
        }

        ~DAL()
        {
            //sqlAdapter.Dispose();
        }

        //public string Getconnectionstring()
        //{
        //    return GetAppSetting(config, "connectionString");// ConfigurationManager.AppSettings["connectionString"];
        //}

        public DataTable GetNotification(int interval)
        {
            string strSql = string.Format(@"Select * From Notifications Where Status = 0 and SendingTime >= '" + 
                DateTime.Now.AddMinutes(interval * -1).ToString("MM/dd/yyyy HH:mm") + "' and SendingTime <= '"  + DateTime.Now.ToString("MM/dd/yyyy HH:mm") + "'");
            return GetData(strSql).Tables[0];
        }

        public void UpdateNotification(int notificationID)
        {
            string strSql = string.Format(@"Update Notifications Set Status = 1 Where NotificationID = " + notificationID);
            GetData(strSql);
            return;
        }

        public void UpdateNotificationError(int notificationID,string err)
        {
            string strSql = string.Format(@"Update Notifications Set Status = 2 and Error='" + err + "' Where NotificationID = " + notificationID);
            GetData(strSql);
            return;
        }

        public DataSet GetData(string strSql)
        {
            DataSet ds = new DataSet();
            using (SqlDataAdapter adapter = new SqlDataAdapter(strSql, conn))
            {
                adapter.Fill(ds);
            }
            //SqlDataAdapter sqlAdapter = new SqlDataAdapter(strSql, conn);
            //sqlAdapter.Fill(ds);
            return ds;
        }
    }

    
}
