using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace DiscordQuestCompleterInstaller
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isSilent = false;
            foreach (var arg in args)
            {
                if (arg.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/silent", StringComparison.OrdinalIgnoreCase))
                {
                    isSilent = true;
                }
            }

            if (isSilent)
            {
                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiscordQuestCompleter");
                InstallEngine.DoInstall(targetDir, true, true, false, null);
                return;
            }

            Application.Run(new MainForm());
        }
    }

    public static class InstallEngine
    {
        public static void DoInstall(string installDir, bool createDesktop, bool createStartMenu, bool launchAfter, Action<int, string> progressCallback)
        {
            if (progressCallback != null) progressCallback(5, "Preparing directory...");
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            // Close existing process if running
            try
            {
                foreach (var proc in Process.GetProcessesByName("discord-quest-completer"))
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
            }
            catch { }

            if (progressCallback != null) progressCallback(15, "Extracting application binaries...");

            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream("DiscordQuestCompleter.app.zip"))
            {
                if (stream == null)
                {
                    throw new Exception("Embedded payload 'DiscordQuestCompleter.app.zip' not found in installer.");
                }

                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    int total = archive.Entries.Count;
                    int count = 0;
                    foreach (var entry in archive.Entries)
                    {
                        count++;
                        string destPath = Path.Combine(installDir, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destPath);
                            continue;
                        }

                        string parent = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                        {
                            Directory.CreateDirectory(parent);
                        }

                        entry.ExtractToFile(destPath, true);
                        int pct = 15 + (int)(((double)count / total) * 70);
                        if (progressCallback != null) progressCallback(pct, "Extracting " + entry.Name + "...");
                    }
                }
            }

            string exePath = Path.Combine(installDir, "discord-quest-completer.exe");
            string iconPath = Path.Combine(installDir, "resources", "icon.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = exePath;
            }

            if (progressCallback != null) progressCallback(90, "Creating shortcuts...");

            if (createDesktop)
            {
                string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcut(Path.Combine(desktopFolder, "Discord Quest Completer.lnk"), exePath, installDir, iconPath, "Discord Quest Completer");
            }

            if (createStartMenu)
            {
                string startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                CreateShortcut(Path.Combine(startMenuFolder, "Discord Quest Completer.lnk"), exePath, installDir, iconPath, "Discord Quest Completer");
            }

            // Write uninstaller script
            string uninstallBat = Path.Combine(installDir, "Uninstall.cmd");
            string uninstallContent = "@echo off\r\n" +
                "echo Uninstalling Discord Quest Completer...\r\n" +
                "taskkill /f /im discord-quest-completer.exe >nul 2>&1\r\n" +
                "del /f /q \"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Discord Quest Completer.lnk") + "\" >nul 2>&1\r\n" +
                "del /f /q \"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Discord Quest Completer.lnk") + "\" >nul 2>&1\r\n" +
                "cd /d \"%TEMP%\"\r\n" +
                "rmdir /s /q \"" + installDir + "\"\r\n" +
                "echo Done! Discord Quest Completer uninstalled.\r\n" +
                "pause\r\n";
            File.WriteAllText(uninstallBat, uninstallContent);

            if (progressCallback != null) progressCallback(100, "Installation completed!");

            if (launchAfter && File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = installDir
                });
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconLocation, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.IconLocation = iconLocation;
                shortcut.Description = description;
                shortcut.Save();
            }
            catch { }
        }
    }

    public class MainForm : Form
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkLaunch;
        private Button btnInstall;
        private Button btnCancel;
        private ProgressBar progressBar;
        private Label lblStatus;
        private bool isFinished = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Discord Quest Completer Setup";
            this.Size = new Size(540, 440);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(20, 22, 31);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Try load icon
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var iconStream = asm.GetManifestResourceStream("DiscordQuestCompleter.icon.ico"))
                {
                    if (iconStream != null)
                    {
                        this.Icon = new Icon(iconStream);
                    }
                }
            }
            catch { }

            // Header Banner Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(14, 16, 23)
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "Discord Quest Completer",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Setup & Installation Wizard • Automated Quest Completion",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 155, 164),
                Location = new Point(25, 48),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Body Area
            Panel pnlBody = new Panel
            {
                Location = new Point(24, 98),
                Size = new Size(476, 240),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlBody);

            Label lblDest = new Label
            {
                Text = "Installation Folder:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(219, 222, 225),
                Location = new Point(0, 8),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblDest);

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiscordQuestCompleter");
            txtPath = new TextBox
            {
                Text = defaultPath,
                Location = new Point(0, 32),
                Size = new Size(380, 26),
                BackColor = Color.FromArgb(30, 31, 38),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlBody.Controls.Add(txtPath);

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(390, 31),
                Size = new Size(86, 28),
                BackColor = Color.FromArgb(43, 45, 49),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(70, 72, 80);
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = fbd.SelectedPath;
                    }
                }
            };
            pnlBody.Controls.Add(btnBrowse);

            // Checkboxes
            chkDesktop = new CheckBox
            {
                Text = "Create Desktop Shortcut",
                Checked = true,
                Location = new Point(4, 76),
                AutoSize = true,
                ForeColor = Color.FromArgb(219, 222, 225)
            };
            pnlBody.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox
            {
                Text = "Create Start Menu Shortcut",
                Checked = true,
                Location = new Point(4, 106),
                AutoSize = true,
                ForeColor = Color.FromArgb(219, 222, 225)
            };
            pnlBody.Controls.Add(chkStartMenu);

            chkLaunch = new CheckBox
            {
                Text = "Launch Discord Quest Completer after install",
                Checked = true,
                Location = new Point(4, 136),
                AutoSize = true,
                ForeColor = Color.FromArgb(219, 222, 225)
            };
            pnlBody.Controls.Add(chkLaunch);

            // Progress bar and status
            lblStatus = new Label
            {
                Text = "Ready to install.",
                Location = new Point(0, 175),
                Size = new Size(476, 20),
                ForeColor = Color.FromArgb(148, 155, 164),
                Font = new Font("Segoe UI", 8.5F)
            };
            pnlBody.Controls.Add(lblStatus);

            progressBar = new ProgressBar
            {
                Location = new Point(0, 198),
                Size = new Size(476, 16),
                Style = ProgressBarStyle.Blocks,
                Value = 0
            };
            pnlBody.Controls.Add(progressBar);

            // Footer Bottom
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(14, 16, 23)
            };
            this.Controls.Add(pnlFooter);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(286, 14),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(43, 45, 49),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 72, 80);
            btnCancel.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCancel);

            btnInstall = new Button
            {
                Text = "Install",
                Location = new Point(398, 14),
                Size = new Size(104, 32),
                BackColor = Color.FromArgb(88, 101, 242),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(114, 137, 218);
            btnInstall.Click += BtnInstall_Click;
            pnlFooter.Controls.Add(btnInstall);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (isFinished)
            {
                this.Close();
                return;
            }

            string target = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Please choose a valid destination directory.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            btnBrowse.Enabled = false;
            txtPath.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkLaunch.Enabled = false;

            bool desk = chkDesktop.Checked;
            bool start = chkStartMenu.Checked;
            bool launch = chkLaunch.Checked;

            Thread t = new Thread(() =>
            {
                try
                {
                    InstallEngine.DoInstall(target, desk, start, launch, (pct, status) =>
                    {
                        this.Invoke(new Action(() =>
                        {
                            progressBar.Value = Math.Min(100, Math.Max(0, pct));
                            lblStatus.Text = status;
                        }));
                    });

                    this.Invoke(new Action(() =>
                    {
                        progressBar.Value = 100;
                        lblStatus.Text = "Installation finished successfully!";
                        lblStatus.ForeColor = Color.FromArgb(35, 165, 90);
                        btnInstall.Text = "Finish";
                        btnInstall.BackColor = Color.FromArgb(35, 165, 90);
                        btnInstall.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
                        btnInstall.Enabled = true;
                        btnCancel.Visible = false;
                        isFinished = true;
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "Error: " + ex.Message;
                        lblStatus.ForeColor = Color.FromArgb(242, 63, 67);
                        btnInstall.Enabled = true;
                        btnCancel.Enabled = true;
                        btnBrowse.Enabled = true;
                        txtPath.Enabled = true;
                        MessageBox.Show("Installation failed:\n" + ex.Message, "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
            t.IsBackground = true;
            t.Start();
        }
    }
}
