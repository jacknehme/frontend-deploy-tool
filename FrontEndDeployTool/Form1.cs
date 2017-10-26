using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeployFrontEnd
{
    public partial class Form1 : Form
    {
        private EnvironmentSettings environmentSettings = null;



        private void richTextBoxOutput_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        public Form1()
        {
            InitializeComponent();
            InitializeComboBoxes();
            InitializeEnvironment();
        }

        private void InitializeComboBoxes()
        {
            comboBoxEnvironment.DataSource = EnvironmentSettings.EnvironmentTypes;
            comboBoxBranch.DataSource = EnvironmentSettings.Branches;
        }

        private void InitializeEnvironment()
        {
            environmentSettings = new EnvironmentSettings(EnvironmentSettings.EnvironmentTypes[comboBoxEnvironment.SelectedIndex], textBoxVersion.Text, checkBoxReleased.Checked, EnvironmentSettings.Branches[comboBoxBranch.SelectedIndex]);
            environmentSettings.LocalDirectory = checkBoxLocalDirectory.Checked;
            EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
        }

        #region DeploymentButtonEvents

        // MAIN Deployment Step
        private void buttonMain_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.Clear();
            buttonCreateDir_Click(null, null);
            buttonGit_Click(null, null);
            buttonUpdateIndexHtml_Click(null, null);
            buttonUpdateWebConfig_Click(null, null);
            buttonUpdateAppValues_Click(null, null);
            buttonUpdatePasServiceWebConfig_Click(null, null);
            if (!environmentSettings.IsQA())
            {
                buttonUpdateAppJs_Click(null, null);
                buttonUpdateUserServiceJs_Click(null, null);
            }

            //read last publish time
            string path = environmentSettings.GetPhysicalPath();
            DateTime dt = Utils.ReadPublishTime(path);

            //git log --after="2013-11-12 00:00" --before="2013-11-12 23:59
            //grab git commits comments where > publish time

            string git_messages = Git.Messages(path, dt);
            //email comments to team 
            Utils.Email("Publish@email.com", "Email-1@email.com,Email-2@email.com,Email-3@email.com", environmentSettings.Environment + " updated", git_messages);
            //update publish timeg
            Utils.UpdatePublishTime(path);

            //GitTag(path, "staging");

            richTextBoxOutput.AppendText(@"Application deployed to " + environmentSettings.Environment + "\t");
            richTextBoxOutput.AppendText("Url: " + @"http://" + environmentSettings.GetServer() + @"/" + environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion + "\n\n");
            richTextBoxOutput.AppendText("Physical Path:\t" + environmentSettings.GetPhysicalPath() + "\n\n");
        }

        // Step 1 - Create directory if not exist
        private void buttonCreateDir_Click(object sender, EventArgs e)
        {
            string branch = environmentSettings.GetBranch();
            string path = environmentSettings.GetPhysicalPath();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                string cloneUrl = ConfigurationManager.AppSettings["CloneUrl"];
                Git.Clone(path, cloneUrl, richTextBoxOutput);
                Git.CreateSparseCheckoutFile(path, richTextBoxOutput);
                Git.Checkout(path, branch, richTextBoxOutput);
            }
            else
            {
                richTextBoxOutput.AppendText("Directory already exists at: " + path + "\n\n");
                richTextBoxOutput.Refresh();

            }
        }

        // Step 2 Run Git
        private void buttonGit_Click(object sender, EventArgs e)
        {
            string branch = environmentSettings.GetBranch();
            string path = environmentSettings.GetPhysicalPath();

            richTextBoxOutput.AppendText("using directory " + path + "\n\n");
            richTextBoxOutput.Refresh();
            if (Directory.Exists(path))
            {
                Git.Stash(path, richTextBoxOutput);
                if (environmentSettings.Environment == "QA")
                {
                    QaMirrorDevelopment(path);
                }
                Git.Pull(path, branch, richTextBoxOutput);
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        // Step 2.1 Run Git for qa branch
        private void QaMirrorDevelopment(string path)
        {
            Git.Checkout(path, "qa", richTextBoxOutput);
            Git.Pull(path, "qa", richTextBoxOutput);

            Git.Checkout(path, "development", richTextBoxOutput);
            Git.Pull(path, "development", richTextBoxOutput);

            Git.Checkout(path, "qa", richTextBoxOutput);
            Git.Merge(path, "development", richTextBoxOutput);
            Git.Push(path, richTextBoxOutput);
        }

        // Step 3 - Update Index.html file
        private void buttonUpdateIndexHtml_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE INDEX.html --------------------\n\n");

            string version = environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion.ToLower();
            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            richTextBoxOutput.AppendText("Version: " + version + "\n");
            richTextBoxOutput.AppendText("Path: " + path + "\n");

            richTextBoxOutput.AppendText("\n");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "index.html");
                string when = DateTime.Now.ToString("yyyyMMdd");

                Utils.ReplaceInFile(path, @"href=""/projectname" + @"\w*\d*\.{0,1}\d*", @"href=""/" + version);
                richTextBoxOutput.AppendText(@"REPLACED href=""/projectname WITH " + @"href=""/" + version + "\n");

                Utils.ReplaceInFile(path, @"\d{8}", when);
                richTextBoxOutput.AppendText("REPLACED old date WITH " + when + "\n");

                richTextBoxOutput.AppendText("\n");

                //Utils.ReplaceInFile(path, "localhost", environmentSettings.GetServer());
                //richTextBoxOutput.AppendText("CHANGED localhost TO " + environmentSettings.GetServer() + "\n");

                Utils.ReplaceInFile(path, "xx.xx.xx.xx", environmentSettings.GetServer());
                richTextBoxOutput.AppendText("CHANGED xx.xx.xx.xx TO " + environmentSettings.GetServer() + "\n");

                Utils.ReplaceInFile(path, "projectnameServicesDev", environmentSettings.GetEnvPrefix() + "Services" + environmentSettings.FolderVersion);
                richTextBoxOutput.AppendText("CHANGED serviceUrls FROM projectnameServicesDev TO " + environmentSettings.GetEnvPrefix() + "Services" + environmentSettings.FolderVersion + "\n");

                richTextBoxOutput.AppendText("\n");

                if (!environmentSettings.IsQA())
                {
                    Utils.ReplaceInFile(path, "<!-- Remove Script -->", "");
                    richTextBoxOutput.AppendText("REMOVED <!-- Remove Script -->" + "\n");

                    Utils.ReplaceInFile(path, "<script src=\"scripts/script-to-remove.js\"></script>", "");
                    richTextBoxOutput.AppendText("REMOVED <script src=\"scripts/script-to-remove.js\"></script>" + "\n");

                    string str = "<script src=\"app/app.script-to-remove.config.js\\?v=" + when + "\"></script>";

                    Utils.ReplaceInFile(path, str, "");
                    richTextBoxOutput.AppendText("REMOVED <script src=\"app/app.script-to-remove.config.js?v=" + when + "\"></script>" + "\n");
                }

                richTextBoxOutput.AppendText("\n");
                richTextBoxOutput.Refresh();
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        // Step 4 - Update Web.config file
        private void buttonUpdateWebConfig_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE Web.config --------------------\n\n");

            string version = environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion.ToLower();
            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "Web.config");

                Utils.ReplaceInFile(path, "/projectname/", "/" + version + "/");
                richTextBoxOutput.AppendText("REPLACED /projectname/ WITH /" + version + "/" + "\n\n");
                richTextBoxOutput.Refresh();
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        // Step 5 - Update AppValues.js file
        private void buttonUpdateAppValues_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE AppValues.js --------------------\n\n");

            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "app\\values\\app.values.js");

                Utils.ReplaceInFile(path, "localhost", environmentSettings.GetServer());
                richTextBoxOutput.AppendText("CHANGED localhost TO " + environmentSettings.GetServer() + "\n");

                if (environmentSettings.Environment != "Gold")
                {
                    Utils.ReplaceInFile(path, "Authorization2", "Authorization");
                    richTextBoxOutput.AppendText("CHANGED Authorization2 TO Authorization\n");
                }

                richTextBoxOutput.AppendText("\n");

                Utils.ReplaceInFile(path, @".*environment:.*", "\t\t\t" + @"environment: '" + environmentSettings.Environment + "',");
                richTextBoxOutput.AppendText("REPLACED environment: WITH " + @"environment: " + environmentSettings.Environment + ",\n");

                Utils.ReplaceInFile(path, @".*baseUrl:.*", "\t\t\t" + @"baseUrl: ""http://" + environmentSettings.GetServer() + @"/" + environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion.ToLower() + "\",");
                richTextBoxOutput.AppendText(@"REPLACED baseUrl: ""http://localhost/projectname"" WITH " + @"baseUrl: ""http://" + environmentSettings.GetServer() + @"/" + environmentSettings.GetEnvPrefix().ToLower() + environmentSettings.AppVersion.ToLower() + "\",\n");

                richTextBoxOutput.AppendText("\n");

                Utils.ReplaceInFile(path, "value-to-remove", environmentSettings.GetClientID());
                richTextBoxOutput.AppendText("CHANGED clientID FROM value-to-remove TO " + environmentSettings.GetClientID() + "\n");

                Utils.ReplaceInFile(path, "xx.xx.xx.xx", environmentSettings.GetServer());
                richTextBoxOutput.AppendText("CHANGED FROM xx.xx.xx.xx TO " + environmentSettings.GetServer() + "\n");

                Utils.ReplaceInFile(path, "projectnameServicesDev", environmentSettings.GetEnvPrefix() + "Services" + environmentSettings.FolderVersion);
                richTextBoxOutput.AppendText("CHANGED serviceUrls FROM projectnameServicesDev TO " + environmentSettings.GetEnvPrefix() + "Services" + environmentSettings.FolderVersion + "\n");

                richTextBoxOutput.AppendText("\n");
                richTextBoxOutput.Refresh();

            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        // Step 6 - Update Pas-service\web.config file
        private void buttonUpdatePasServiceWebConfig_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE service Web.config --------------------\n\n");

            string version = environmentSettings.AppVersion.ToLower();
            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "service");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
                return;
            }

            //Add Web.config file to directory
            path = System.IO.Path.Combine(path, "Web.config");

            if (!File.Exists(path))
            {
                string webconfigString = ConfigResources.pas_service_web_config.ToString();

                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(webconfigString);
                }
            }

            Utils.ReplaceInFile(path, "localhost", @"http://" + environmentSettings.GetServer());
            richTextBoxOutput.AppendText("REPLACED localhost WITH " + @"http://" + environmentSettings.GetServer() + "\n");

            Utils.ReplaceInFile(path, @"projectname" + @"\w*\d*\.{0,1}\d*", environmentSettings.GetEnvPrefix().ToLower() + version);
            richTextBoxOutput.AppendText("REPLACED projectname WITH " + environmentSettings.GetEnvPrefix().ToLower() + version + "\n\n");

            richTextBoxOutput.Refresh();
        }

        // Step 7 - Update app.js file
        private void buttonUpdateAppJs_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE App.js --------------------\n\n");

            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "app\\app.js");

                Utils.ReplaceInFile(path, "\'RemoveMeModule\',", "");
                richTextBoxOutput.AppendText("REMOVED \'RemoveMeModule\', FROM app.js" + "\n\n");

                richTextBoxOutput.Refresh();
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        // Step 8 - Update user.service.js file
        private void buttonUpdateUserServiceJs_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectionColor = Color.Red;
            richTextBoxOutput.AppendText("-- UPDATE USER.SERVICE.js --------------------\n\n");

            string path = System.IO.Path.Combine(environmentSettings.GetPhysicalPath(), "ProjectName");

            if (Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "app\\services\\user.service.js");

                Utils.ReplaceInFile(path, @"RemoveMeService,", "");
                richTextBoxOutput.AppendText("REMOVED RemoveMeService" + "\n");

                Utils.ReplaceInFile(path, @"'RemoveMeService',", "");
                richTextBoxOutput.AppendText("REMOVED 'RemoveMeService'," + "\n");

                Utils.ReplaceInFile(path, "appInsights,", "");
                richTextBoxOutput.AppendText("REMOVED appInsights," + "\n\n");
                richTextBoxOutput.Refresh();
            }
            else
            {
                richTextBoxOutput.AppendText("Directory does NOT exist at: " + path + "\n\n");
                richTextBoxOutput.Refresh();
            }
        }

        #endregion

        #region OtherControlEvents

        private void checkBoxReleased_CheckedChanged(object sender, EventArgs e)
        {
            environmentSettings.Released = checkBoxReleased.Checked;
            EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
        }

        private void buttonDisplayEnvironment_Click(object sender, EventArgs e)
        {
            EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
            richTextBoxOutput.AppendText("Date Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n");
            richTextBoxOutput.Refresh();
        }

        private void checkBoxLocalDirectory_CheckedChanged(object sender, EventArgs e)
        {
            environmentSettings.LocalDirectory = checkBoxLocalDirectory.Checked;
            EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
        }

        private void textBoxVersion_TextChanged(object sender, EventArgs e)
        {
            environmentSettings.Version = textBoxVersion.Text;
            environmentSettings.AppVersion = textBoxVersion.Text;
            EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
        }

        private void comboBoxEnvironment_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (environmentSettings != null)
            {
                environmentSettings.Environment = EnvironmentSettings.EnvironmentTypes[comboBoxEnvironment.SelectedIndex];
                textBoxVersion.Enabled = true;
                checkBoxReleased.Visible = true;
                buttonUpdateAppJs.Enabled = true;
                buttonUpdateUserServiceJs.Enabled = true;
                switch (comboBoxEnvironment.SelectedIndex)
                {
                    case 0: // QA
                        comboBoxBranch.SelectedIndex = 0;
                        textBoxVersion.Text = "QA";
                        textBoxVersion.Enabled = false;
                        checkBoxReleased.Visible = false;
                        buttonUpdateAppJs.Enabled = false;
                        buttonUpdateUserServiceJs.Enabled = false;
                        break;
                    case 1: // Staging
                    case 2: // DevTest
                    case 3: // OpsTest
                    case 4: // Training
                        comboBoxBranch.SelectedIndex = 1;
                        textBoxVersion.Text = environmentSettings.CurrentVersion;
                        break;
                    case 5: // Production
                        comboBoxBranch.SelectedIndex = 2;
                        textBoxVersion.Text = environmentSettings.CurrentVersion;
                        break;
                    default:
                        comboBoxBranch.SelectedIndex = 0;
                        textBoxVersion.Text = environmentSettings.CurrentVersion;
                        break;
                }
                EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
            }
        }

        private void comboBoxBranch_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (environmentSettings != null)
            {
                environmentSettings.Branch = EnvironmentSettings.Branches[comboBoxBranch.SelectedIndex];
                EnvironmentSettings.DisplayEnvironmentVariables(environmentSettings, richTextBoxOutput);
            }
        }

        private void checkBoxAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            buttonCreateDir.Visible = checkBoxAdvanced.Checked;
            buttonGit.Visible = checkBoxAdvanced.Checked;
            buttonUpdateIndexHtml.Visible = checkBoxAdvanced.Checked;
            buttonUpdateWebConfig.Visible = checkBoxAdvanced.Checked;
            buttonUpdateAppValues.Visible = checkBoxAdvanced.Checked;
            buttonUpdatePasServiceWebConfig.Visible = checkBoxAdvanced.Checked;
            buttonUpdateAppJs.Visible = checkBoxAdvanced.Checked;
            buttonUpdateUserServiceJs.Visible = checkBoxAdvanced.Checked;
        }


        #endregion

    }
}
