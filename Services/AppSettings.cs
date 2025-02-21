using System;
using System.IO;
using System.Text.Json;

namespace BluetoothClipboard.Services
{
    public class AppSettings
    {
        private const string SettingsFile = "settings.json";
        
        public string? LastConnectedDeviceAddress { get; set; }
        public string? LastConnectedDeviceName { get; set; }
        
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                // 如果读取失败，返回默认设置
            }
            
            return new AppSettings();
        }
        
        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception)
            {
                // 保存失败时忽略错误
            }
        }
    }
} 