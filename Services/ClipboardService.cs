using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using System.Linq;
using BluetoothClipboard.Models;
using Serilog;

namespace BluetoothClipboard.Services
{
    public class ClipboardService
    {
        private readonly DispatcherTimer _timer;
        private string _lastDataHash = string.Empty;
        private string _lastImageHash = string.Empty;
        private bool _isUpdatingProgrammatically = false;

        public event EventHandler<ClipboardData>? ClipboardUpdated;

        public ClipboardService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
        }

        public void StartMonitoring()
        {
            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            try
            {
                var data = GetClipboardData();
                if (data != null && data.IsValid)
                {
                    if (data.Hash != _lastDataHash)
                    {
                        _lastDataHash = data.Hash;
                        ClipboardUpdated?.Invoke(this, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "访问剪贴板时出错");
            }
        }

        private ClipboardData? GetClipboardData()
        {
            if (Clipboard.ContainsText())
            {
                return new ClipboardData
                {
                    Type = ClipboardDataType.Text,
                    Text = Clipboard.GetText()
                };
            }
            
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    using var stream = new MemoryStream();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                    
                    return new ClipboardData
                    {
                        Type = ClipboardDataType.Image,
                        ImageData = stream.ToArray()
                    };
                }
            }
            
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                return new ClipboardData
                {
                    Type = ClipboardDataType.Files,
                    FilePaths = files.Cast<string>().ToArray()
                };
            }
            
            return null;
        }

        public void UpdateClipboardWithoutNotification(ClipboardData data)
        {
            try
            {
                _isUpdatingProgrammatically = true;
                
                // 如果是图片，检查哈希值
                if (data.Type == ClipboardDataType.Image && data.ImageData != null)
                {
                    var hash = ComputeHash(data.ImageData);
                    if (hash == _lastImageHash)
                    {
                        return;
                    }
                    _lastImageHash = hash;
                }
                
                _lastDataHash = data.Hash;
                switch (data.Type)
                {
                    case ClipboardDataType.Text when !string.IsNullOrEmpty(data.Text):
                        Clipboard.SetText(data.Text);
                        break;
                    case ClipboardDataType.Image when data.ImageData != null:
                        var image = LoadImage(data.ImageData);
                        Clipboard.SetImage(image);
                        break;
                    case ClipboardDataType.Files when data.FilePaths != null:
                        var collection = new StringCollection();
                        foreach (var path in data.FilePaths)
                        {
                            collection.Add(path);
                        }
                        Clipboard.SetFileDropList(collection);
                        break;
                }
            }
            finally
            {
                _isUpdatingProgrammatically = false;
            }
        }

        private BitmapImage LoadImage(byte[] imageData)
        {
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public void UpdateClipboard(string content)
        {
            if (content == _lastDataHash) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _lastDataHash = content;
                    Clipboard.SetText(content);
                }
                catch (Exception ex)
                {
                    // 记录剪贴板更新错误
                    Log.Error(ex, "更新剪贴板内容时发生错误");
                }
            });
        }

        private string ComputeHash(byte[] data)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}