﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Paladins_Presence
{
    public partial class Presence : ServiceBase
    {
        Timer invokeTimer = new Timer();
        Timer exeCheckTimer = new Timer();
        Timer statusCheckTimer = new Timer();
        DiscordRPC.DiscordRpcClient client;
        private int playerId = 9152847;

        public Presence()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteLog(string.Format("[{0}] Paladins Presence is starting.", DateTime.Now));
            WriteLog(string.Format("Build {0}", 16));

            // Set Intervals
            exeCheckTimer.Interval = 1000; // 120000
            statusCheckTimer.Interval = 30000;
            invokeTimer.Interval = 150;

            // Set Handlers
            exeCheckTimer.Elapsed += new ElapsedEventHandler(onExeCheckTime);
            statusCheckTimer.Elapsed += new ElapsedEventHandler(onStatusCheckTime);
            invokeTimer.Elapsed += InvokeTimer_Elapsed;

            invokeTimer.Enabled = true;
            exeCheckTimer.Enabled = true;

            client = new DiscordRPC.DiscordRpcClient("552259697126670355");
            client.OnError += (sender, e) =>
            {
                WriteLog(e.ToString());
            };

            client.OnReady += (sender, e) =>
            {
                WriteLog(string.Format("Ready from user {0}", e.User.Username));
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                WriteLog(string.Format("Update gotten: {0}", e.Presence));
            };

            client.Initialize();
        }

        private void InvokeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            client.Invoke();
        }

        protected override void OnStop()
        {
            WriteLog(string.Format("[{0}] Paladins Presence is stopping.", DateTime.Now));
            client.Dispose();
        }

        private void onExeCheckTime(object source, ElapsedEventArgs e)
        {
            // WriteLog("Checking for Paladins");
            bool running = isPaladinsRunning();
            if (running && !statusCheckTimer.Enabled)
            {
                WriteLog("Starting status check timer.");
                this.updateStatus();
                statusCheckTimer.Enabled = true;
            }
            else if(!running && statusCheckTimer.Enabled)
            {
                WriteLog("Stopping status check timer.");
                client.ClearPresence();
                statusCheckTimer.Enabled = false;
            }
        }

        private void onStatusCheckTime(object source, ElapsedEventArgs e)
        {
            WriteLog("Getting status");

            this.updateStatus();
        }

        private void updateStatus()
        {
            string url = "http://api.paladinspresence.com/" + playerId;
            var wc = new WebClient { Proxy = null };
            var resp = wc.DownloadString(url);
            JToken json = JValue.Parse(resp);

            // WriteLog(Property);
            // WriteLog(json.SelectToken("rich").ToString());
            WriteLog("Updating presence");
            WriteLog(json.SelectToken("rich").SelectToken("details").ToString());

            client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = json.SelectToken("rich").SelectToken("details").ToString(),
                State = json.SelectToken("rich").SelectToken("state").ToString(),

                Assets = new DiscordRPC.Assets()
                {
                    LargeImageKey = json.SelectToken("rich").SelectToken("large_image_key").ToString(),
                    LargeImageText = json.SelectToken("rich").SelectToken("large_image_text").ToString(),
                    SmallImageKey = json.SelectToken("rich").SelectToken("small_image_key").ToString(),
                    SmallImageText = json.SelectToken("rich").SelectToken("small_image_text").ToString()
                }
            });
        }

        private bool isPaladinsRunning()
        {
            return Process.GetProcessesByName("EasyAntiCheat Launcher").Length > 0 || Process.GetProcessesByName("Paladins").Length > 0;
        }

        public void WriteLog(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_PaladinsPresence_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}