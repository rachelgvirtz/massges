using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using System.Configuration;
using System.Data;

namespace MessagingModule
{
    public class Messaging
    {
        static Configuration config = null;
        static private string URLApi;
        static private string SendMessageQuery = "<PALO><HEAD><FROM>{0}</FROM><APP USER=\"{1}\" PASSWORD=\"{2}\"/><CMD>sendtextmt</CMD></HEAD><BODY><CONTENT>{3}</CONTENT><DEST_LIST><TO>{4}</TO></DEST_LIST></BODY><OPTIONAL><CALLBACK>{5}</CALLBACK></OPTIONAL></PALO>";
        static private string Company;
        static private string User;
        static private string Password;
        static private string SenderName;
        DAL currDAL = null;

        public Messaging()
        {
            string exeConfigPath = this.GetType().Assembly.Location;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            }
            catch (Exception ex)
            {
                //handle errror here.. means DLL has no sattelite configuration file.
            }
            if (config != null)
            { 
                currDAL = /*new DAL(GetAppSetting(config, "connectionString"));*/ new DAL(ConfigurationManager.AppSettings["connectionString"].ToString());
                URLApi = /*GetAppSetting(config, "SMSApi");*/ ConfigurationManager.AppSettings["SMSApi"].ToString();
                Company = /*GetAppSetting(config, "Company"); */ ConfigurationManager.AppSettings["Company"].ToString();
                User = /*GetAppSetting(config, "User"); */ ConfigurationManager.AppSettings["User"].ToString();
                Password = /*GetAppSetting(config, "Password"); */ ConfigurationManager.AppSettings["Password"].ToString();
                SenderName = /*GetAppSetting(config, "SenderName");*/ ConfigurationManager.AppSettings["SenderName"].ToString();
            }
        }

        ~Messaging()
        {
            currDAL = null;
        }

        public class PushNotification
        {
            string pushTitle;
            string pushBody;
            string pushLink;

            public string PushTitle { get => pushTitle; set => pushTitle = value; }
            public string PushBody { get => pushBody; set => pushBody = value; }
            public string PushLink { get => pushLink; set => pushLink = value; }
        }

        public class Mail
        {
            MailRecipient from = new MailRecipient();
            MailRecipient to = new MailRecipient();
            string subject;
            string body;

            public MailRecipient From { get => from; set => from = value; }
            public MailRecipient To { get => to; set => to = value; }
            public string Subject { get => subject; set => subject = value; }
            public string Body { get => body; set => body = value; }
        }

        public class MailRecipient
        {
            string address;
            string title;

            public string Address { get => address; set => address = value; }
            public string Title { get => title; set => title = value; }
        }

        static string GetAppSetting(Configuration config, string key)
        {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }



        static private string HTTPPost(string url, string data)
        {
            string response = string.Empty;

            //Query
            WebRequest webRequest = (WebRequest.Create(url));
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";

            //Write content to the query
            byte[] byteArray = Encoding.ASCII.GetBytes(data);
            webRequest.ContentLength = byteArray.Length;
            Stream dataStream = webRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            //Get the response
            WebResponse webResponse = webRequest.GetResponse();
            if (webResponse != null)
            {
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                response = sr.ReadToEnd().Trim();
                sr.Close();
            }

            return response;
        }

        public void UpdateNotification(int notificationID)
        {

        }

        public  void SendNotifications()
        {
            DataTable dt;

            try
            {

                //dt = currDAL.GetNotification(int.Parse(GetAppSetting(config, "SendingInterval")));
                dt = currDAL.GetNotification(int.Parse(ConfigurationManager.AppSettings["SendingInterval"].ToString()));
                for (int currNotification = 0; currNotification < dt.Rows.Count; currNotification++)
                {
                    switch (int.Parse(dt.Rows[currNotification]["Type"].ToString()))
                    {
                        case 1: //email
                            {
                                Mail currMail = new Mail();
                                currMail.From.Address = /*GetAppSetting(config, "EmailFrom"); */ ConfigurationManager.AppSettings["EmailFrom"].ToString();
                                currMail.From.Title = /*GetAppSetting(config, "EmailFromTitle"); */ ConfigurationManager.AppSettings["EmailFromTitle"].ToString();
                                currMail.To.Address = dt.Rows[currNotification]["RecipientTo"].ToString();
                                currMail.To.Title = dt.Rows[currNotification]["RecipientTo"].ToString();
                                currMail.Body = dt.Rows[currNotification]["Body"].ToString();
                                currMail.Subject = dt.Rows[currNotification]["Subject"].ToString();

                                string response = SendMail(currMail);
                                if(response.ToLower() == "ok")
                                {
                                    currDAL.UpdateNotification(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()));
                                }
                                else
                                {
                                    currDAL.UpdateNotificationError(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()),response);
                                }

                                break;
                            }
                        case 2: // SMS
                            {
                                string message = dt.Rows[currNotification]["Subject"].ToString();
                                string phone = dt.Rows[currNotification]["RecipientMobile"].ToString();

                                string response = SendSMS(message,phone);
                                if (response.ToLower() == "ok")
                                {
                                    currDAL.UpdateNotification(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()));
                                }
                                else
                                {
                                    currDAL.UpdateNotificationError(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()), response);
                                }

                                break;
                            }
                        case 3: // Push
                            {
                                PushNotification currPush = new PushNotification();
                                currPush.PushTitle = dt.Rows[currNotification]["Subject"].ToString();
                                currPush.PushBody = dt.Rows[currNotification]["Body"].ToString();
                                currPush.PushLink = dt.Rows[currNotification]["PushLink"].ToString();
                                string fireID = dt.Rows[currNotification]["RecipientFireId"].ToString();

                                string response = SendPush(currPush, fireID);
                                if (response.ToLower() == "ok")
                                {
                                    currDAL.UpdateNotification(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()));
                                }
                                else
                                {
                                    currDAL.UpdateNotificationError(int.Parse(dt.Rows[currNotification]["NotificationID"].ToString()), response);
                                }

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            finally
            {

            }
             
        }

        public  string SendMail(Mail mail)
        {
            try
            {
                MailMessage msgMail = new MailMessage();

                MailMessage myMessage = new MailMessage();
                myMessage.From = new MailAddress(mail.From.Address, mail.From.Title);
                myMessage.To.Add(new MailAddress(mail.To.Address, mail.To.Title));
                myMessage.Subject = mail.Subject;
                myMessage.IsBodyHtml = true;
                myMessage.ReplyTo = new MailAddress(mail.From.Address);
                myMessage.Body = mail.Body;

                SmtpClient mySmtpClient = new SmtpClient();
                //System.Net.NetworkCredential myCredential = new System.Net.NetworkCredential(GetAppSetting(config, "EmailFrom"), GetAppSetting(config, "EmailKey"));
                System.Net.NetworkCredential myCredential = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["EmailFrom"].ToString(), ConfigurationManager.AppSettings["EmailKey"].ToString());
                mySmtpClient.Host = /*GetAppSetting(config, "EmailSMTP"); */ ConfigurationManager.AppSettings["EmailSMTP"].ToString();
                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.Credentials = myCredential;
                mySmtpClient.ServicePoint.MaxIdleTime = 1;
                mySmtpClient.Port = /*int.Parse(GetAppSetting(config, "EmailPort"));*/ int.Parse(ConfigurationManager.AppSettings["EmailPort"].ToString());
                mySmtpClient.EnableSsl = true;
                mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                mySmtpClient.Send(myMessage);
                myMessage.Dispose();
                
                return "Ok";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

   

         public string SendPush(PushNotification message , string FireId)
        {
            if (string.IsNullOrEmpty(FireId))
            {
                return "Empty FireId";
            }

            string applicationID = /*GetAppSetting(config, "PushApplicationID");*/ ConfigurationManager.AppSettings["PushApplicationID"].ToString();
            string senderId = /*GetAppSetting(config, "PushSenderID");*/ ConfigurationManager.AppSettings["PushSenderID"].ToString();
            string deviceId = FireId;
            string str = "ok";

            try
            {
                //WebRequest tRequest = WebRequest.Create(GetAppSetting(config, "PushURL"));
                WebRequest tRequest = WebRequest.Create(ConfigurationManager.AppSettings["PushURL"].ToString());
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                string json = string.Empty;
                
                var data = new
                {
                    to = deviceId,
                    priority = "high",
                    content_available = true,
                    data = new
                    {
                        title = message.PushTitle,
                        body = message.PushBody,
                        link = message.PushLink
                    }
                };
                var serializer = new JavaScriptSerializer();
                json = serializer.Serialize(data);
                
                
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add(string.Format("Authorization: key={0}", applicationID));
                tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));
                tRequest.ContentLength = byteArray.Length;

                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                str = sResponseFromServer;
                            }
                        }
                    }
                }

                return "ok";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

         public string SendSMS(string Message, string Phone)
        {
            string BLMJ = string.Empty;

            try
            {
                //Query
                string SendMessageQueryData = string.Format(SendMessageQuery, Company, User, Password, Message, Phone, SenderName);

                //Get Response
                string Response = HTTPPost(URLApi, "XMLString=" + HttpUtility.UrlEncode(SendMessageQueryData));

                //Load response
                if (Response != string.Empty)
                {
                    XmlDocument xmlResponse = new XmlDocument();
                    xmlResponse.LoadXml(Response);
                    if (int.Parse(xmlResponse.DocumentElement.SelectSingleNode("/RESPONSE/RESULTCODE").InnerText) == 0)
                    {
                        BLMJ = xmlResponse.DocumentElement.SelectSingleNode("/RESPONSE/BLMJ").InnerText;
                    }

                    // אם שליחת סמסים ע"י ספק הסמסים נכשלת אזי נשלח ללקוח (יצחק שטיינברג) מייל אזהרה
                    if (xmlResponse.DocumentElement.SelectSingleNode("/RESPONSE/RESULTMESSAGE").InnerText != "Success")
                    {
                        return "ttpStatusCode.Unauthorized";
                        string mailBodyText = "שליחת סמס על ידי הספק:\n" + "cellact \n" + " נכשלה. אנא פנה לספק או למפתח של המערכת. פירטי ההזדהות בהתחברות לספק הסמסים הם:\n" + "שם החברה: " + Company + "\n" + "שם משתמש: " + User + "\n" + "סיסמה: " + Password + "\n" + "קוד השגיאה שחזר מהקריאה לשירות: " + "\n" + URLApi + "\nהוא:\n" + xmlResponse.DocumentElement.SelectSingleNode("/RESPONSE/RESULTMESSAGE").InnerText;
                      System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage("info@maayan-hatahara.com", "levyshachar2@msn.com", "שליחת סמס נכשלה", mailBodyText);

                        System.Web.Mail.SmtpMail.SmtpServer = "smtp.012.net.il";
                        System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
                        smtp.Host = "maayan-hatahara.com";
                        smtp.Credentials = new System.Net.NetworkCredential("info@maayan-hatahara.com", "hWj56p*1");

                       smtp.Send(msg);

                        msg = new System.Net.Mail.MailMessage("info@maayan-hatahara.com", "rav.itzchaksteinberg@gmail.com", "שליחת סמס נכשלה", mailBodyText);
                        smtp.Send(msg);
                    }
                }


            }
            catch (Exception ex)
            {
               string mailBodyText = "שליחת סמס על ידי הספק:\n" + "cellact \n" + " נכשלה. אנא פנה לספק או למפתח של המערכת. פירטי ההזדהות בהתחברות לספק הסמסים הם:\n" + "שם החברה: " + Company + "\n" + "שם משתמש: " + User + "\n" + "סיסמה: " + Password + "\n" + "קוד השגיאה שחזר מהקריאה לשירות: " + "\n" + URLApi + "\nהוא:\n" + ex.Message+"\n"+ex.InnerException;
                System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage("info@maayan-hatahara.com", "levyshachar2@msn.com", "שליחת סמס נכשלה", mailBodyText);

                System.Web.Mail.SmtpMail.SmtpServer = "smtp.012.net.il";
                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
               smtp.Host = "maayan-hatahara.com";
                smtp.Credentials = new System.Net.NetworkCredential("info@maayan-hatahara.com", "hWj56p*1");

                smtp.Send(msg);

                msg = new System.Net.Mail.MailMessage("info@maayan-hatahara.com", "rav.itzchaksteinberg@gmail.com", "שליחת סמס נכשלה", mailBodyText);
                smtp.Send(msg);
            }

            return BLMJ != string.Empty ? "ok" : "Error";
        }
    }
}
