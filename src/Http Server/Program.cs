﻿using System;
using System.Windows.Forms;
using HttpServer.Properties;
using System.Net;

namespace HttpServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            
            InitializeVersionChecking();

            StartUI();
            CheckFirstLoad();
            StartServer();

            Application.Run();
        }
        
        private static NotificationsManager NotificationsManager { get; } = new NotificationsManager("HTTP Server", Resources.Icon, ClickedNotification);

        private static SettingsForm SettingsForm { get; set; }

        private static FileSystemServer Server { get; set; }

        private static void ClickedNotification(object sender, EventArgs e)
        {
            if (SettingsForm == null || SettingsForm.IsDisposed)
            {
                SettingsForm = new SettingsForm(DidUpdateSettings, ShowFolderBrowser);
            }
            if (!SettingsForm.Visible)
            {
                SettingsForm.Show();
            }
            else
            {
                SettingsForm.Hide();
            }
        }

        private static void ShowFolderBrowser(object sender, EventArgs e) => ShowFolderBrowser(false);

        private static void ShowFolderBrowser(bool save)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.RootFolder = Environment.SpecialFolder.MyComputer;
                folderBrowser.SelectedPath = Settings.Default.Root;
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    if (save)
                    {
                        Settings.Default.Root = folderBrowser.SelectedPath;
                        Settings.Default.Save();
                        StartServer();
                    }
                    else
                    {
                        SettingsForm.rootTextBox.Text = folderBrowser.SelectedPath;
                        SettingsForm.Changed(null, null);
                    }
                }
            }
        }

        private static void DidUpdateSettings(object sender, EventArgs e) => StartServer();

        private static void InitializeVersionChecking()
        {
            if (!Settings.Default.CheckForUpdates)
            {
                return;
            }
            if (!VersionChecker.CurrentVersionIsUpToDate())
            {
                ShowNotification("An update is available", "A new version of HTTP Server is available. Open Settings to download the new version.", 1000, true);
            }
        }

        private static void StartUI()
        {
            NotificationsManager.AddContextMenuItem("Exit", (sender, e) => 
            {
                NotificationsManager.Dispose();
                Application.Exit();
            });
        }

        private static void CheckFirstLoad()
        {
            if (Settings.Default.UsedBefore)
            {
                return;
            }
            Settings.Default.Root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Settings.Default.UsedBefore = true;
            Settings.Default.Save();
        }

        private static void StartServer()
        {
            Server = new FileSystemServer(Settings.Default.Root, Settings.Default.Port, "+", ServerAuthenticator.None());

            Server.DidUpdateState += (sender, e) => {
                if (Server.State == HttpServerState.Started)
                {
                    ShowNotification("Server Started", "Server started on " + Server.Prefix, 1500);
                }
                else if (Server.State == HttpServerState.Error)
                {
                    if (Server.Error is HttpListenerException)
                    {
                        Settings.Default.Port = 8000;
                        Settings.Default.Save();
                        Server.Restart();
                    }
                    else
                    {
                        MessageBox.Show("Error", "Could not start server.");
                    }
                }
            };

            Server.Start();
        }

        private static void ShowNotification(string title, string text, int duration, bool ignoreSettings = false)
        {
            if (!Settings.Default.NotifyMe && !ignoreSettings)
            {
                return;
            }
            NotificationsManager.ShowBalloonToolTip(title, text, duration);
        }
    }
}