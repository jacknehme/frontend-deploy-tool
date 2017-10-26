using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace DeployFrontEnd
{
    public static class Utils
    {
        public static void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            var content = string.Empty;
            using (StreamReader reader = new StreamReader(filePath))
            {
                content = reader.ReadToEnd();
                reader.Close();
            }

            content = Regex.Replace(content, searchText, replaceText);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(content);
                writer.Close();
            }
        }

        public static string ReadFile(string path)
        {
            int counter = 0;
            string line;
            string file_contents = "";
            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                file_contents = file_contents + line;
                counter++;
            }

            file.Close();
            return file_contents;
        }

        public static void WriteFile(string path, string contents)
        {
            // Write the string to a file.
            System.IO.StreamWriter file = new System.IO.StreamWriter(path, false);//overwrite existing file
            file.WriteLine(contents);

            file.Close();
        }

        public static string Email(string from, string to, string subject, string body)
        {
            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.Host = "*mail.com*";
            //client.EnableSsl = true;
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //client.UseDefaultCredentials = false;
            //client.Credentials = new System.Net.NetworkCredential("*username@email.com*","password");

            MailMessage mailMessage = new MailMessage(from, to, subject, body);
            mailMessage.BodyEncoding = UTF8Encoding.UTF8;
            mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            try
            {
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                return ex.Message;
                
            }
            return "";
            
        }

        public static bool DirectoryCheck(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                // Initializes the variables to pass to the MessageBox.Show method.
                string message = "Directory does not exist at: " + path + "\n\n";
                string caption = "Error Detected";
                MessageBoxButtons buttons = MessageBoxButtons.OK;

                // Displays the MessageBox.
                DialogResult result = MessageBox.Show(message, caption, buttons);

                return false;
            }
            
        }

        public static DateTime ReadPublishTime(string path)
        {
            path = System.IO.Path.Combine(path, "publish.ini");
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
            string lines = Utils.ReadFile(path);
            if (String.IsNullOrEmpty(lines))
            {
                Utils.WriteFile(path, DateTime.Now.ToString("yyyy-MM-dd"));
                lines = DateTime.Now.ToString("yyyy-MM-dd");
            }
            DateTime dt = Convert.ToDateTime(lines);

            return dt;
        }

        public static void UpdatePublishTime(string path)
        {
            path = System.IO.Path.Combine(path, "publish.ini");
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
            Utils.WriteFile(path, DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
}
