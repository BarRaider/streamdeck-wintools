﻿using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualDesktop;
using WinTools.Wrappers;

namespace WinTools.Actions
{

    [PluginActionId("com.barraider.wintools.vdswitch")]
    public class VirtualDesktopSwitchAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Name = String.Empty,
                    Desktops = null,
                    CreateVirtualDesktop = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "desktops")]
            public List<VirtualDesktopInfo> Desktops { get; set; }

            [JsonProperty(PropertyName = "createVirtualDesktop")]
            public bool CreateVirtualDesktop { get; set; }
        }

        #region Private Members
        private readonly PluginSettings settings;

        #endregion

        public VirtualDesktopSwitchAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
            InitializeSettings();
        }

        #region Public Methods

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");
            if (!WindowsHelpers.IsSupportedVirtualDesktopVersion())
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed but invalid Virtual Desktop Version");
                await Connection.SetTitleAsync("Update\nWindows");
                await Connection.ShowAlert();
                return;
            }

            if (SwitchVirtualDesktop())
            {
                await Connection.ShowOk();
            }
            else
            {
                await Connection.ShowAlert();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            InitializeSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #endregion

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private bool SwitchVirtualDesktop()
        { 
            if (String.IsNullOrEmpty(settings.Name))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"SwitchVirtualDesktop: Desktop name is null!");
                return false;
            }

            try
            {
                // Check if there already is a desktop with that name
                int id = Desktop.SearchDesktop(settings.Name);
                if (id < 0)
                {
                    if (settings.CreateVirtualDesktop)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Virtual desktop with name {settings.Name} does not exist, creating new one");
                        var newDesktop = Desktop.Create();
                        newDesktop.SetName(settings.Name);
                        id = Desktop.SearchDesktop(settings.Name);
                    }
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Virtual desktop with name {settings.Name} does not exist");
                        return false;
                    }
                }

                var desktop = Desktop.FromIndex(id);
                desktop.MakeVisible();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"SwitchVirtualDesktop Exception: {ex}");
            }
            return false;
        }

        private void InitializeSettings()
        {
            if (!WindowsHelpers.IsSupportedVirtualDesktopVersion())
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Invalid Virtual Desktop Version");
                Connection.SetTitleAsync("Update\nWindows");
                return;
            }
            FetchAllVirtualDesktops();
        }

        private void FetchAllVirtualDesktops()
        {
            settings.Desktops = new List<VirtualDesktopInfo>();
            try
            {
                for (int currDesktop = 0; currDesktop < Desktop.Count; currDesktop++)
                {
                    settings.Desktops.Add(new VirtualDesktopInfo() { Name = Desktop.DesktopNameFromIndex(currDesktop) });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"FetchAllVirtualDesktops Exception: {ex}");
            }
            SaveSettings();
        }

        private void Connection_OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "refreshdesktops":
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"refreshDesktops called");
                        FetchAllVirtualDesktops();
                        break;
                }
            }
        }

        #endregion
    }

}
