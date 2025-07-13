using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Temp_Desktop
{
    internal static class Program
    {

        static string originaldesktop;
        static string currentdesktop;
        static ToolStripLabel currentdesktoplabel;
        static bool hideShortcuts;
        static bool resetOnExit = true;
        static NotifyIcon trayicon;
        static string registrySaveLocation = "SOFTWARE\\gip\\TempDesktop";

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            if (args.Contains("--self-restart"))
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registrySaveLocation, true))
                {
                    if (key != null)
                    {
                        originaldesktop = key.GetValue("originaldesktop").ToString();
                        Trace.WriteLine("Loaded from REGISTRY");
                    }
                    else
                    {
                        using (RegistryKey key2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true))
                        {
                            originaldesktop = key2.GetValue("Desktop").ToString();
                        }
                        key.SetValue("originaldesktop", originaldesktop);
                    }
                }
            } else
            {
                Trace.WriteLine("Loaded default windows location");
                using (RegistryKey key2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true))
                {
                    originaldesktop = key2.GetValue("Desktop").ToString();
                }
            }
            currentdesktop = originaldesktop;
            
            if (originaldesktop == null)
            {
                Application.Exit();
            }

            ContextMenuStrip menu = new ContextMenuStrip();

            currentdesktoplabel = new ToolStripLabel();
            currentdesktoplabel.Text = "Current Desktop: " + currentdesktop;
            menu.Items.Add(currentdesktoplabel);

            ToolStripMenuItem changeItem = new ToolStripMenuItem();
            changeItem.Text = "Change Desktop Folder";
            changeItem.Click += new System.EventHandler(changeClicked);
            menu.Items.Add(changeItem);

            ToolStripMenuItem resetItem = new ToolStripMenuItem();
            resetItem.Text = "Reset Desktop Folder";
            resetItem.ToolTipText = "Original Desktop: " + originaldesktop;
            resetItem.Click += new System.EventHandler(resetClicked);
            menu.Items.Add(resetItem);

            // Options menu
            ToolStripMenuItem optionsMenu = new ToolStripMenuItem("Options");
            ToolStripMenuItem hideShortcutsItem = new ToolStripMenuItem("Hide all desktop shortcuts (requires admin)");
            hideShortcutsItem.CheckOnClick = true;
            hideShortcutsItem.ToolTipText = "Should Public Desktop Shortcuts be hidden? This includes system shortcuts like Recycle Bin and Edge as well as Programs installed for all users. Default is no to allow program to run without admin privileges.";
            if (IsAdministrator())
            {
                hideShortcuts = true;
            } else
            {
                hideShortcuts = false;
            }
            hideShortcutsItem.Checked = hideShortcuts;
            hideShortcutsItem.CheckedChanged += hideShortcuts_CheckedChanged;

            ToolStripMenuItem resetOnCloseItem = new ToolStripMenuItem("Reset desktop on app close");
            resetOnCloseItem.CheckOnClick = true;
            resetOnCloseItem.Checked = resetOnExit;
            resetOnCloseItem.ToolTipText = "Should the desktop be reset when the program is closed? Default is yes to prevent users forgetting to switch back to the default and having issues.";
            resetOnCloseItem.CheckedChanged += resetExit_CheckedChanged;

            optionsMenu.DropDownItems.Add(hideShortcutsItem);
            optionsMenu.DropDownItems.Add(resetOnCloseItem);
            menu.Items.Add(optionsMenu);


            ToolStripMenuItem closeItem = new ToolStripMenuItem();
            closeItem.Text = "Exit";
            closeItem.Click += new System.EventHandler(exitClicked);
            menu.Items.Add(closeItem);

            IContainer container = new Container();

            trayicon = new NotifyIcon(container);
            string resourceName = "Temp_Desktop.desktop_folder.ico";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    Icon icon = new Icon(stream);
                    trayicon.Icon = icon;
                }
            }
            trayicon.Text = "Temporary Desktop";
            trayicon.Visible = true;
            trayicon.ContextMenuStrip = menu;

            Application.Run();
        }

        private static void changeClicked(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select the folder to use as your desktop";
            dialog.InitialDirectory = currentdesktop;
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SetDesktopRegKey(dialog.FileName);
                if (hideShortcuts)
                {
                    removeShortcuts();
                }
                refreshDesktop();
            }
        }

        private static void refreshDesktop()
        {
            // Find the taskbar window (Shell_TrayWnd)
            IntPtr hWnd = FindWindow("Shell_TrayWnd", null);

            // WM_USER + 436 is the message to close the Explorer shell
            const uint WM_USER = 0x0400;
            PostMessage(hWnd, WM_USER + 436, IntPtr.Zero, IntPtr.Zero);

            // Wait for Explorer to fully exit
            while (FindWindow("Shell_TrayWnd", null) != IntPtr.Zero)
            {
                Thread.Sleep(750);
            }

            // Restart Explorer
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
        }

        private static void SetDesktopRegKey(string newDesktop)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true))
            {
                if (key != null) 
                {
                    key.SetValue("Desktop", newDesktop);
                    currentdesktop = newDesktop;
                    currentdesktoplabel.Text = "Current Desktop: " + newDesktop;
                }
            }
        }

        private static void resetClicked(object sender, EventArgs e)
        {
            SetDesktopRegKey(originaldesktop);
            currentdesktop = originaldesktop;
            currentdesktoplabel.Text = "Current Desktop: " + originaldesktop;
            returnShortcuts();
            refreshDesktop();
        }

        private static void exitClicked(object sender, EventArgs e)
        {
            trayicon.Visible = false;
            if (resetOnExit && currentdesktop != originaldesktop) { resetClicked(sender, e); }
            Application.Exit();
        }

        private static void CopyAllFiles(string from, string to, bool deleteFrom = false)
        {
            DirectoryInfo from_dir = new DirectoryInfo(from);
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            foreach (FileInfo file in from_dir.GetFiles())
            {
                file.CopyTo(to + "\\" + file.Name);
                file.Delete();
            }
            if (deleteFrom)
            {
                from_dir.Delete();
            }
        }

        private static void removeShortcuts()
        {
            string shortcutDir = "C:\\Users\\Public\\Desktop";
            string shortcutBackupDir = "C:\\Users\\Public\\Desktop_backup";
            CopyAllFiles(shortcutDir, shortcutBackupDir);

        }

        private static void returnShortcuts()
        {
            string shortcutDir = "C:\\Users\\Public\\Desktop";
            string shortcutBackupDir = "C:\\Users\\Public\\Desktop_backup";
            if (Directory.Exists(shortcutBackupDir))
            {
                CopyAllFiles(shortcutBackupDir, shortcutDir, true);
            }
        }

        private static void hideShortcuts_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            hideShortcuts = item.Checked;
            if (hideShortcuts)
            {
                RestartAsAdmin();
            }
            if (!hideShortcuts)
            {
                returnShortcuts();
            }
        }

        private static void resetExit_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem reset = sender as ToolStripMenuItem;
            resetOnExit = reset.Checked;
            Trace.WriteLine(resetOnExit);
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RestartAsAdmin()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registrySaveLocation, true))
            {
                key.SetValue("originaldesktop", originaldesktop);
            }
            trayicon.Visible = false;
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--self-restart"
            };
            Process.Start(psi);
            Environment.Exit(0);
        }
    }
}