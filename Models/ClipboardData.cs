using System;
using System.Text.Json.Serialization;

namespace BluetoothClipboard.Models
{
    public enum ClipboardDataType
    {
        Text,
        Image,
        Files
    }

    public class ClipboardData
    {
        public ClipboardDataType Type { get; set; }
        public string? Text { get; set; }
        public byte[]? ImageData { get; set; }
        public string[]? FilePaths { get; set; }

        [JsonIgnore]
        public bool IsValid => Type switch
        {
            ClipboardDataType.Text => !string.IsNullOrEmpty(Text),
            ClipboardDataType.Image => ImageData != null && ImageData.Length > 0,
            ClipboardDataType.Files => FilePaths != null && FilePaths.Length > 0,
            _ => false
        };

        [JsonIgnore]
        public string Hash => Type switch
        {
            ClipboardDataType.Text => Text?.GetHashCode().ToString() ?? string.Empty,
            ClipboardDataType.Image => ImageData != null ? 
                BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(ImageData)) : string.Empty,
            ClipboardDataType.Files => FilePaths != null ? 
                string.Join("|", FilePaths).GetHashCode().ToString() : string.Empty,
            _ => string.Empty
        };
    }
} 