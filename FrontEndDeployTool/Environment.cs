using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeployFrontEnd
{
    public class EnvironmentSettings
    {
        public static List<string> EnvironmentTypes = new List<string> { "QA", "Staging", "Gold", "Training", "Production" };
        public static List<string> Branches = new List<string> { "qa", "staging", "master", "development" };

        public static void DisplayEnvironmentVariables(EnvironmentSettings environmentSettings, RichTextBox richTextBoxOutput)
        {
            richTextBoxOutput.Clear();
            richTextBoxOutput.AppendText("Environment\t" + environmentSettings.Environment + "\n");
            richTextBoxOutput.AppendText("Git Branch\t" + environmentSettings.Branch + "\n");
            richTextBoxOutput.AppendText("App Version\t" + environmentSettings.AppVersion + "\n");
            richTextBoxOutput.AppendText("Released?\t" + ((environmentSettings.IsQA() ? "N/A" : environmentSettings.Released.ToString()) + "\n"));
            richTextBoxOutput.AppendText("Enviro Prefix\t" + environmentSettings.GetEnvPrefix() + "\n");
            richTextBoxOutput.AppendText("FolderVersion\t" + environmentSettings.FolderVersion + "\n");
            richTextBoxOutput.AppendText("Server Path\t" + environmentSettings.GetServer() + "\n");
            richTextBoxOutput.AppendText("Client ID\t\t" + environmentSettings.GetClientID() + "\n\n");

            richTextBoxOutput.AppendText("Url\t\t" + @"http://" + environmentSettings.GetServer() + @"/" + environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion + "\n\n");

            richTextBoxOutput.AppendText("Physical Path\t" + environmentSettings.GetPhysicalPath() + "\n\n");
        }

        public EnvironmentSettings() { }

        public EnvironmentSettings(string environment, string version, bool released, string branch)
        {
            Environment = environment;
            Version = version;
            Released = released;
            Branch = branch;
        }

        public string Environment { get; set; }
        public string Version { get; set; }
        public bool Released { get; set; }
        public bool LocalDirectory { get; set; }
        public string Branch { get; set; }

        public string CurrentVersion = ConfigurationManager.AppSettings["CurrentVersion"];

        private string ServerQA = ConfigurationManager.AppSettings["ServerQA"];
        private string ServerStaging = ConfigurationManager.AppSettings["ServerStaging"];
        private string ServerGold = ConfigurationManager.AppSettings["ServerGold"];
        private string ServerTraining = ConfigurationManager.AppSettings["ServerTraining"];
        private string ServerProduction = ConfigurationManager.AppSettings["ServerProduction"];

        private string EnvPrefix = ConfigurationManager.AppSettings["EnvPrefix"];
        private string EnvPrefixGold = ConfigurationManager.AppSettings["EnvPrefixGold"];
        private string EnvPrefixTraining = ConfigurationManager.AppSettings["EnvPrefixTraining"];

        private string ClientIDQA = ConfigurationManager.AppSettings["ClientIDQA"];
        private string ClientIDStaging = ConfigurationManager.AppSettings["ClientIDStaging"];
        private string ClientIDGold = ConfigurationManager.AppSettings["ClientIDGold"];
        private string ClientIDTraining = ConfigurationManager.AppSettings["ClientIDTraining"];
        private string ClientIDProduction = ConfigurationManager.AppSettings["ClientIDProdution"];

        private string appversion;

        public string AppVersion
        {
            get
            {
                appversion = Version;
                string rtnVersion;

                switch (Environment)
                {
                    case "QA":
                        rtnVersion = "QA"; break;
                    case "Gold":
                    case "Training":
                        if (Released)
                            rtnVersion = appversion = "";
                        else
                            rtnVersion = appversion;
                        break;
                    case "Staging":
                    case "Production":
                        if (Released)
                            rtnVersion = appversion.Split(new char[] { '.' })[0];
                        else
                            rtnVersion = appversion;
                        break;
                    default:
                        rtnVersion = appversion; break;
                }
                return rtnVersion.ToLower();
            }
            set { appversion = value; }
        }

        public string FolderVersion
        {
            get { return Version; }
        }

        public string GetServer()
        {
            switch (Environment)
            {
                case "QA":
                    return ServerQA;
                case "Staging":
                    return ServerStaging;
                case "Gold":
                    return ServerGold;
                case "Training":
                    return ServerTraining;
                case "Production":
                    return ServerProduction; 
                default:
                    return "http://localhost";
            }
        }

        public string GetEnvPrefix()
        {
            switch (Environment)
            {
                case "Gold":
                    return EnvPrefixGold;
                case "Training":
                    return EnvPrefixTraining;
                default:
                    return EnvPrefix;
            }
        }

        public string GetClientID()
        {
            switch (Environment)
            {
                case "QA":
                    return ClientIDQA;
                case "Staging":
                    return ClientIDStaging;
                case "Gold":
                    return ClientIDGold;
                case "Training":
                    return ClientIDTraining;
                case "Production":
                    return ClientIDProduction;
                default:
                    return ClientIDQA;
            }
        }

        public string GetBranch()
        {
            return Branch;
        }

        public string GetPhysicalPath()
        {
            //for local testing
            //LocalDirectory = true;
            if (LocalDirectory)
            {
                return @"C:\tmp" + @"\Portal\" + GetEnvPrefix() + FolderVersion;
            }
            return @"\\" + GetServer() + @"\Portal\" + GetEnvPrefix() + FolderVersion;
        }

        public Boolean IsQA()
        {
            return Environment == "QA";
        }

        /*
         * include \Portal in path name
         * 
         * Physical path =  server + "\" envPrefix  + "\" + folderVersion
         * base href (index.html) = "/" + envPrefix + "/" + appVersion
         * baseUrl (app.values.js) =  "http://" + server + "/" + envPrefix + appVersion
         * serviceUrl = "http://" + server + "/" + envPrefix + "Services" + folderVersion
        */

    }
}
