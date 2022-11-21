using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagingModule;

namespace TestMessaging
{
    class Program
    {
        static void Main(string[] args)
        {
            Messaging currMSGSystem = new Messaging();
            currMSGSystem.SendMail(new Messaging.Mail() { Body = "test", From = new Messaging.MailRecipient() { Address = "info@my-shidduch.com", Title = "nadav" }, Subject = "test shidduch", To = new Messaging.MailRecipient() { Address = "nadavgrinberg@gmail.com", Title = "Nadav" } });
            currMSGSystem.SendNotifications();
        }
    }
}
