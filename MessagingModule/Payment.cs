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
using System.Net.Http;
using System.Collections.Specialized;

namespace MessagingModule
{
    public class Payment
    {
        private Dictionary<string, string> vars = new Dictionary<string, string>();
        private string originalResponse { get; set; }
        private static HttpClient httpClient = new HttpClient();
        private string RedirectToUrl { get; set; }

        public class CardComResponse
        {
            public int ResponseCode { get; set; }
            public string Description { get; set; }
            public string LowProfileCode { get; set; }
            public string DealResponse { get; set; }
            public string OperationResponse { get; set; }
            public Int64 InternalDealNumber { get; set; }
            public string InvoiceNumber { get; set; }
            public string ReturnValue { get; set; }
            public string UniqueID { get; set; }
            public string AddLangSuffix { get; set; }
            public string message { get; set; }
            public string Errors { get; set; }
        }

        public class MemberPayment
        {
            public int UserId { get; set; }
            public bool IsDolars { get; set; }
            public int memberType { get; set; }
            public string ExpDate { get; set; }
            public DateTime expDateD { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Language { get; set; }
            public string cred_type { get; set; }
            public string retlang { get; set; }
            public string Currency { get; set; }
            public string AddLangSuffix { get; set; }
            public string mtp { get; set; }
            public string uid { get; set; }
            public string sum { get; set; }
            public CardComParams CardComParams { get; set; }
            public string SuccessURL { get; set; }
            public string ErrorURL { get; set; }
            public string IndicatorURL { get; set; }
        }

        public enum MembershipType
        {
            Trial = 0,
            HalfYear = 1,
            Year = 2,
            Silver = 3,
            Gold = 4
        }

        public class CardComParams
        {
            public double TransactionSum { get; set; }
            public int PaymentsNumber { get; set; }
            public MembershipType MembershipType { get; set; }
            public bool ShowPaymentsRange { get; set; }
            public int Is12Payments { get; set; }
            public int NumberOfPayments { get; set; }
            public int IsPaymentLocked { get; set; }
            public string ItemDescription { get; set; }
        }

        private static async Task<string> PostDic(Dictionary<string, string> dic, string PostRequestToURL)
        {
            // Create Post string ( TerminalNumber=1000&UserName=xXXX .... )
            StringBuilder RequstString = new StringBuilder(1024);
            foreach (KeyValuePair<string, string> keyValuePair in dic)
            {
                RequstString.AppendFormat("{0}={1}&", keyValuePair.Key, System.Web.HttpUtility.UrlEncode(keyValuePair.Value, Encoding.UTF8));
            }
            RequstString.Remove(RequstString.Length - 1, 1); // Remove the &
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            StringContent contant = new StringContent(RequstString.ToString());
            HttpResponseMessage response = await httpClient.PostAsync(PostRequestToURL, contant);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetPaymentURL(MemberPayment model)
        {
            vars["TerminalNumber"] = ConfigurationManager.AppSettings["CardComTerminalNum"].ToString(); 
            vars["UserName"] = ConfigurationManager.AppSettings["CardComUserName"].ToString();

            vars["APILevel"] = "10";
            vars["codepage"] = "65001"; 
            vars["Operation"] = "1";

            // Deal Information vars
            vars["Language"] = model.Language; // page languge he- hebrew , en - english , ru , ar
            vars["CoinID"] = "1"; // billing coin , 1- NIS , 2- USD other , article :  http://kb.cardcom.co.il/article/AA-00247/0
            vars["SumToBill"] = model.CardComParams.TransactionSum.ToString(); // Sum To Bill 
            vars["ProductName"] = model.CardComParams.ItemDescription; // Product Name 

            // redirect urls
            vars["SuccessRedirectUrl"] = model.SuccessURL;
            vars["ErrorRedirectUrl"] = model.ErrorURL;
            vars["IndicatorUrl"] = model.IndicatorURL;

            // Other Optional vars :
            vars["ReturnValue"] = "1899"; // value that will be return and save in CardCom system
            //if (model.CardComParams.NumberOfPayments == 1)
            //{
                vars["MaxNumOfPayments"] = model.CardComParams.NumberOfPayments.ToString();
                vars["MinNumOfPayments"] = "1";
                vars["DefaultNumOfPayments"] = model.CardComParams.NumberOfPayments.ToString() ;
            //}
            //if (model.CardComParams.NumberOfPayments == 12)
            //{
            //    vars["MaxNumOfPayments"] = "12";
            //    vars["MinNumOfPayments"] = "1";
            //    vars["DefaultNumOfPayments"] = "12";
            //}
            //if (model.CardComParams.NumberOfPayments == 5)
            //{
            //    vars["MaxNumOfPayments"] = "12";
            //    vars["MinNumOfPayments"] = "1";
            //    vars["DefaultNumOfPayments"] = "5";
            //}
            vars["CardOwnerEmail"] = model.Email;
            vars["CardOwnerName"] = model.FullName;
            vars["CardOwnerPhone"] = model.Phone;

            vars["ShowInvoiceHead"] = "true";

            //invoice
            vars["InvoiceHead.CustName"] = model.FullName; // customer name
            vars["InvoiceHead.SendByEmail"] = "true"; // will the invoice be send by email to the customer
            vars["InvoiceHead.Language"] = model.Language; // he or en only
            vars["InvoiceHead.Email"] = model.Email; // value that will be return and save in CardCom system

            // products info 

            // Line 1
            vars["InvoiceLines1.Description"] = model.CardComParams.ItemDescription;
            vars["InvoiceLines1.Price"] = model.CardComParams.TransactionSum.ToString();
            vars["InvoiceLines1.Quantity"] = "1";
            //invoice


            originalResponse = await PostDic(vars, ConfigurationManager.AppSettings["CardComApi"].ToString());

            string FullResponce = HttpUtility.UrlDecode(originalResponse); // UI update
            var parseResponse = new NameValueCollection(System.Web.HttpUtility.ParseQueryString(originalResponse));

            if (parseResponse["ResponseCode"] == "0") // request was ok !
            {
                // get LPC , LPC is a Unique cardcom 'order' id. save it on your site order for reference!
                string LowProfileCode = parseResponse["LowProfileCode"]; // UI Update
                                                                         // url is the addres you need to redirect the customer ( or set it in a iFrame src attribute)
                RedirectToUrl = parseResponse["url"]; // UI Update
            }
            else // Error In development 
            {
                // Email Developer originalResponse vars Dictionary 
                string error = parseResponse["ResponseCode"] + " " + parseResponse["Description"];
            }
            return RedirectToUrl;
        }
    }
}
