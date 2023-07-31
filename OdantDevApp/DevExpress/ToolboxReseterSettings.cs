// Decompiled with JetBrains decompiler
// Type: DevExpress.ProjectUpgrade.Package.ToolboxReseterSettings
// Assembly: DevExpress.ProjectUpgrade.Package.Async.2022, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a
// MVID: E043B518-C45C-4005-9918-F43EDCB8C9DE
// Assembly location: C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\DevExpress\ProjectConverter\DevExpress.ProjectUpgrade.Package.Async.2022.dll

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DevExpress.ProjectUpgrade.Package
{
    [Serializable]
    public class ToolboxReseterSettings : ISerializable
    {
        public bool IsAlreadyResetedToolbox;
        public bool IsRequestResetToolbox;
        private const string SettingsDirectoryName = "Toolbox Reseter";
        private const string SettingsFileName = "ToolboxReseterSettings.xml";

        private ToolboxReseterSettings()
          : this(false, false, string.Empty)
        {
        }

        private ToolboxReseterSettings(
          bool isAlreadyResetedToolbox,
          bool isShowResetMessage,
          string resetedToolboxVSVersion)
        {
            this.IsRequestResetToolbox = isShowResetMessage;
            this.IsAlreadyResetedToolbox = isAlreadyResetedToolbox;
        }

        public ToolboxReseterSettings(SerializationInfo info, StreamingContext context)
          : this(info.GetBoolean(nameof(IsAlreadyResetedToolbox)), false, info.GetString("ResetedToolboxVSVersion"))
        {
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("IsAlreadyResetedToolbox", this.IsAlreadyResetedToolbox);
        }

        public static ToolboxReseterSettings Load()
        {
            string settingsFilePath = ToolboxReseterSettings.GetSettingsFilePath();
            if (!File.Exists(settingsFilePath))
                return new ToolboxReseterSettings();
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ToolboxReseterSettings));
                FileStream fileStream = new FileStream(settingsFilePath, FileMode.Open);
                ToolboxReseterSettings toolboxReseterSettings = xmlSerializer.Deserialize((Stream)fileStream) as ToolboxReseterSettings;
                fileStream.Close();
                return toolboxReseterSettings;
            }
            catch
            {
                return new ToolboxReseterSettings();
            }
        }

        public static string GetSettingsDirectory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), SettingsDirectoryName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public void Save()
        {
            string settingsFilePath = ToolboxReseterSettings.GetSettingsFilePath();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ToolboxReseterSettings));
            FileStream fileStream = new FileStream(settingsFilePath, FileMode.Create);
            xmlSerializer.Serialize((Stream)fileStream, (object)this);
            fileStream.Close();
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(GetSettingsDirectory(), SettingsFileName);
        }
    }
}
