using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace DeployFrontEnd
{
    static class Git
    {

        static private ProcessStartInfo GitPSStartInfo(string path, string cmdArg)
        {
            ProcessStartInfo _processStartInfo = new ProcessStartInfo();

            _processStartInfo.WorkingDirectory = path;
            _processStartInfo.FileName = @"git.exe";
            _processStartInfo.Arguments = cmdArg;
            _processStartInfo.CreateNoWindow = true;
            _processStartInfo.UseShellExecute = false;
            _processStartInfo.RedirectStandardOutput = true;
            _processStartInfo.RedirectStandardError = true;

            return _processStartInfo;
        }

        static private string GitProcessInput(ProcessStartInfo _processStartInfo)
        {
            Cursor.Current = Cursors.WaitCursor;

            Process _process = Process.Start(_processStartInfo);
            string stdout = _process.StandardOutput.ReadToEnd();
            string stderr = _process.StandardError.ReadToEnd();
            _process.WaitForExit();
            int exitCode = _process.ExitCode;
            _process.Close();

            Cursor.Current = Cursors.Default;

            return stdout + "\n" + stderr + "\n\n";
        }

        static private void GitProcess(string path, string arg, RichTextBox richTextBox)
        {
            ProcessStartInfo _processStartInfo = GitPSStartInfo(path, arg);
            richTextBox.AppendText("Command: git " + arg + "\n\n");
            richTextBox.Refresh();

            string gitProccessOutput = GitProcessInput(_processStartInfo);
            richTextBox.AppendText("Output: " + gitProccessOutput);
            richTextBox.Refresh();
        }

        static public void CreateSparseCheckoutFile(string path, RichTextBox richTextBox)
        {
            string sparseCheckoutPath = System.IO.Path.Combine(path, ".git\\info\\sparse-checkout");

            using (StreamWriter sw = File.CreateText(sparseCheckoutPath))
            {
                sw.WriteLine("#exclude these");
                sw.WriteLine("!ProjectName/*");
                sw.WriteLine("!.nuget/*");
                sw.WriteLine("!ProjectName.sln");
                sw.WriteLine("!/ProjectName/node_modules/*");
                sw.WriteLine("#include everything else");
            }

            string arg = "config core.sparsecheckout true";
            GitProcess(path, arg, richTextBox);
        }

        //Git commands
        static public void Checkout(string path, string branch, RichTextBox richTextBox)
        {
            string arg = "checkout " + branch;
            GitProcess(path, arg, richTextBox);
        }

        static public void Clone(string path, string cloneUrl, RichTextBox richTextBox)
        {
            string arg = "clone -n " + cloneUrl + " .";
            GitProcess(path, arg, richTextBox);
        }

        static public void Merge(string path, string branch, RichTextBox richTextBox)
        {
            string arg = "merge " + branch;
            GitProcess(path, arg, richTextBox);
        }

        static public void Pull(string path, string branch, RichTextBox richTextBox)
        {
            string arg = "pull origin " + branch;
            GitProcess(path, arg, richTextBox);
        }

        static public void Push(string path, RichTextBox richTextBox)
        {
            string arg = "push";
            GitProcess(path, arg, richTextBox);
        }

        static public void Stash(string path, RichTextBox richTextBox)
        {
            string arg = "stash clear";
            GitProcess(path, arg, richTextBox);

            arg = "stash";
            GitProcess(path, arg, richTextBox);
        }

        static public void Tag(string path, string tagname, RichTextBox richTextBox)
        {
            //string tagname = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string arg = "tag " + tagname;
            GitProcess(path, arg, richTextBox);
        }


        static public string Messages(string path, DateTime dt)
        {
            string date = dt.ToString("yyyy-MM-dd");
            string arg = "log --after=" + date + " --no-merges";
            ProcessStartInfo startInfo = GitPSStartInfo(path, arg);

            Cursor.Current = Cursors.WaitCursor;
            Process messageProcess = Process.Start(startInfo);
            string stdout = messageProcess.StandardOutput.ReadToEnd();
            messageProcess.WaitForExit();
            int exitCode = messageProcess.ExitCode;
            messageProcess.Close();
            Cursor.Current = Cursors.Default;
            if (String.IsNullOrEmpty(stdout))
            {
                stdout = "No new commits after " + date;
            }
            return stdout;
        }
    }
}
