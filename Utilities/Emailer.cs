using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

namespace Utilities
{
    public static class Emailer
    {
        public static async Task sendEmail_async(string subject, string body, bool isHtml = false)
        {
            var fromAddress = new MailAddress(ConfigManager.GetString("smtpFromAddress"),
                ConfigManager.GetString("smtpDisplayFrom"));
            var toAddress = new MailAddress(ConfigManager.GetString("smtpToAddress"),
                ConfigManager.GetString("smtpDisplayTo"));
            string fromPassword = ConfigManager.GetString("smtpFromPw");
            using (SmtpClient client = new System.Net.Mail.SmtpClient())
            {
                client.Host = ConfigManager.GetString("smtpHost");
                client.Port = ConfigManager.GetInt("smtpPort");
                client.EnableSsl = ConfigManager.GetBool("smtpEnableSsl");
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(fromAddress.Address, fromPassword);
                using (var mailMessage = new MailMessage(fromAddress, toAddress))
                {
                    if (isHtml) mailMessage.IsBodyHtml = true;
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }
}
