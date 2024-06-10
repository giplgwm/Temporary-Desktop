using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Temp_Desktop
{
    public partial class Form1 : Form
    {
        // Import the ExitWindowsEx function
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        string desktop_shortcut_backup_dir = "C:\\Users\\Public\\Public_Desktop_Backup\\";
        string desktop_shortcut_dir = "C:\\Users\\Public\\Desktop\\";
        public Form1()
        {

            InitializeComponent();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true))
            {
                this.label2.Text = key.GetValue("Desktop").ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select the folder to use as your desktop";
            dialog.InitialDirectory = "C:\\";
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CopyAllFiles(desktop_shortcut_dir, desktop_shortcut_backup_dir);
                SetDesktopRegKey(dialog.FileName);
                ExitWindowsEx(0, 0);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string desktop_string = "C:\\Users\\" + Environment.UserName + "\\Desktop";
            CopyAllFiles(desktop_shortcut_backup_dir, desktop_shortcut_dir, true);
            SetDesktopRegKey(desktop_string);
            ExitWindowsEx(0, 0);

        }

        private void SetDesktopRegKey(string newDesktop)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true)) // Must dispose key or use "using" keyword
            {
                if (key != null)  // Must check for null key
                {
                    key.SetValue("Desktop", newDesktop);
                    this.label2.Text = newDesktop;
                }
            }
        }

        private void CopyAllFiles(string from, string to, bool deleteFrom = false)
        {
            DirectoryInfo from_dir = new DirectoryInfo(from);
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            foreach (FileInfo file in from_dir.GetFiles())
            {
                file.CopyTo(to + file.Name);
                file.Delete();
            }
            if (deleteFrom)
            {
                from_dir.Delete();
            }
        }

    }
}
