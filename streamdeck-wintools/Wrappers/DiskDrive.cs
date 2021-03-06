﻿using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTools.Wrappers
{
    class DiskDrive
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; private set; }

        public DiskDrive(DriveInfo driveInfo)
        {
            try
            {
                if (driveInfo != null)
                {

                    Name = driveInfo?.Name;
                    DisplayName = $"{Name} ({driveInfo?.VolumeLabel})";
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"DiskDrive ctor Exception {ex}");
            }
        }
    }
}
