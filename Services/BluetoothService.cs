using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using InTheHand.Net.Sockets;
using System.Text;
using Serilog;
using System.Text.Json;
using BluetoothClipboard.Models;

namespace BluetoothClipboard.Services
{
    public enum BluetoothRole
    {
        Client,
        Server
    }

    public class BluetoothService
    {
        private readonly BluetoothRole _role;
        private BluetoothClient? _client;
        private BluetoothListener? _listener;
        private Stream? _stream;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Guid serviceGuid = new("A7E27F52-290D-468F-9558-49F9F130B6E9");
        private BluetoothClient? connectedClient;
        private const int ChunkSize = 1024 * 64; // 64KB chunks
        private string _lastImageHash = string.Empty;

        // Fixed UUID for the application. Both client and server must use this.
        public static readonly Guid AppServiceUuid = new Guid("00112233-4455-6677-8899-aabbccddeeff");

        public event EventHandler<ClipboardData>? DataReceived;
        public event EventHandler<string>? ConnectionStatusChanged;
        public event EventHandler<string>? LogMessage;
        public event EventHandler<(int current, int total)>? ImageTransferProgress;

        public BluetoothService(BluetoothRole role)
        {
            _role = role;
            _cancellationTokenSource = new CancellationTokenSource();
            LogInfo($"Initialized as {_role}");
        }

        private void LogInfo(string message)
        {
            var logMessage = $"[{DateTime.Now:HH:mm:ss}] [INFO] {message}";
            Log.Information(message);
            LogMessage?.Invoke(this, logMessage);
        }

        private void LogError(string message, Exception? ex = null)
        {
            var logMessage = $"[{DateTime.Now:HH:mm:ss}] [ERROR] {message}";
            if (ex != null)
            {
                Log.Error(ex, message);
                logMessage += $"\n详细错误: {ex.Message}";
            }
            else
            {
                Log.Error(message);
            }
            LogMessage?.Invoke(this, logMessage);
        }

        public async Task<List<BluetoothDeviceInfo>> ScanDevicesAsync()
        {
            try
            {
                _client = new BluetoothClient();
                var devices = await Task.Run(() => _client.DiscoverDevices());
                LogMessage?.Invoke(this, "设备扫描完成");
                return new List<BluetoothDeviceInfo>(devices);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"扫描设备时出错: {ex.Message}");
                return new List<BluetoothDeviceInfo>();
            }
        }

        public async Task StartServerAsync()
        {
            if (_role != BluetoothRole.Server)
            {
                throw new InvalidOperationException("This instance is not configured as a server");
            }

            try
            {
                _listener = new BluetoothListener(serviceGuid);
                _listener.Start();
                LogInfo("服务器已启动，等待连接...");
                ConnectionStatusChanged?.Invoke(this, "等待连接...");

                while (true)
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested ?? true)
                        break;

                    var client = await Task.Run(() => _listener.AcceptBluetoothClient());
                    connectedClient = client;
                    LogInfo($"客户端已连接: {client.RemoteMachineName}");
                    ConnectionStatusChanged?.Invoke(this, $"已连接到: {client.RemoteMachineName}");

                    _ = HandleClientCommunicationAsync(client);
                }
            }
            catch (Exception ex)
            {
                LogError("启动服务器时出错", ex);
                throw;
            }
        }

        public async Task ConnectToDeviceAsync(BluetoothDeviceInfo device)
        {
            if (_role != BluetoothRole.Client)
            {
                throw new InvalidOperationException("This instance is not configured as a client");
            }

            try
            {
                _client = new BluetoothClient();
                await Task.Run(() => _client.Connect(device.DeviceAddress, serviceGuid));
                _stream = _client.GetStream();
                
                LogInfo($"已连接到: {device.DeviceName}");
                ConnectionStatusChanged?.Invoke(this, $"已连接到: {device.DeviceName}");

                _ = HandleClientCommunicationAsync(_client);
            }
            catch (Exception ex)
            {
                LogError($"连接到 {device.DeviceName} 时出错", ex);
                throw;
            }
        }

        private async Task<bool> EnsureDevicePairedAsync(BluetoothDeviceInfo device)
        {
            try
            {
                LogInfo("Checking device pairing status...");

                using var tempClient = new BluetoothClient();
                var pairedDevice = tempClient.PairedDevices
                    .FirstOrDefault(d => d.DeviceAddress == device.DeviceAddress);

                if (pairedDevice != null && pairedDevice.Authenticated)
                {
                    LogInfo("Device is already paired");
                    return true;
                }

                LogInfo("Device not paired, attempting to pair...");
                ConnectionStatusChanged?.Invoke(this, "正在配对...");

                return await Task.Run(() =>
                {
                    try
                    {
                        return InTheHand.Net.Bluetooth.BluetoothSecurity.PairRequest(device.DeviceAddress, null);
                    }
                    catch
                    {
                        string[] commonPins = { "0000", "1234", "000000" };
                        foreach (var pin in commonPins)
                        {
                            try
                            {
                                if (InTheHand.Net.Bluetooth.BluetoothSecurity.PairRequest(device.DeviceAddress, pin))
                                {
                                    return true;
                                }
                            }
                            catch
                            {
                                // try next PIN
                            }
                        }
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                LogError($"Pairing failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestConnectionAsync()
        {
            if (_stream == null) return false;

            try
            {
                LogInfo("Testing connection...");

                // Send test data.
                var testData = "CONNECT_TEST";
                var testBytes = Encoding.UTF8.GetBytes(testData);
                await _stream.WriteAsync(testBytes, 0, testBytes.Length);
                await _stream.FlushAsync();

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task SendClipboardDataAsync(string data)
        {
            try
            {
                var client = connectedClient ?? this._client;
                if (client?.Connected != true) return;

                var stream = client.GetStream();
                var buffer = System.Text.Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(buffer, 0, buffer.Length);
                LogMessage?.Invoke(this, "剪贴板数据已发送");
            }
            catch (Exception ex)
            {
                LogError($"发送数据时出错: {ex.Message}");
            }
        }

        public async Task SendDataAsync(ClipboardData data)
        {
            try
            {
                if (_stream == null)
                {
                    LogError("无法发送数据：未连接到设备");
                    return;
                }

                // 如果是文件类型，直接返回
                if (data.Type == ClipboardDataType.Files)
                {
                    LogInfo("不支持文件传输");
                    return;
                }

                // 如果是图片，检查哈希值是否相同
                if (data.Type == ClipboardDataType.Image && data.ImageData != null)
                {
                    var hash = ComputeHash(data.ImageData);
                    if (hash == _lastImageHash)
                    {
                        LogInfo("图片数据未变化，跳过传输");
                        return;
                    }
                    _lastImageHash = hash;
                }

                // 序列化数据
                var json = JsonSerializer.Serialize(data);
                var buffer = Encoding.UTF8.GetBytes(json);
                var isImage = data.Type == ClipboardDataType.Image;
                
                if (isImage)
                {
                    LogInfo($"开始传输图片数据 ({buffer.Length / 1024.0:F2}KB)...");
                }

                // 首先发送数据大小
                var sizeBuffer = BitConverter.GetBytes(buffer.Length);
                await _stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length);
                
                // 分块发送数据
                int totalSent = 0;
                for (int i = 0; i < buffer.Length; i += ChunkSize)
                {
                    int size = Math.Min(ChunkSize, buffer.Length - i);
                    await _stream.WriteAsync(buffer, i, size);
                    if (isImage)
                    {
                        totalSent += size;
                        ImageTransferProgress?.Invoke(this, (totalSent, buffer.Length));
                    }
                }
                
                await _stream.FlushAsync();
                LogInfo($"已发送 {data.Type} 类型的数据");
            }
            catch (Exception ex)
            {
                LogError("发送数据时出错", ex);
                throw;
            }
        }

        private void CleanupConnection()
        {
            _stream?.Dispose();
            _client?.Dispose();
            _stream = null;
            _client = null;
        }

        public void Disconnect()
        {
            try
            {
                LogInfo("Disconnecting...");

                _cancellationTokenSource?.Cancel();
                CleanupConnection();
                _listener?.Stop();
                _listener = null;

                _cancellationTokenSource = new CancellationTokenSource();
                ConnectionStatusChanged?.Invoke(this, "已断开连接");

                LogInfo("Disconnected successfully");
            }
            catch (Exception ex)
            {
                LogError($"Disconnect error: {ex.Message}");
                ConnectionStatusChanged?.Invoke(this, "已断开连接");
            }
        }

        ~BluetoothService()
        {
            Disconnect();
        }

        private async Task HandleClientCommunicationAsync(BluetoothClient client)
        {
            try
            {
                _stream = client.GetStream();
                if (_stream == null)
                {
                    throw new InvalidOperationException("Failed to get stream from client");
                }
                
                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }
                
                var buffer = new byte[ChunkSize];
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // 读取数据大小
                        var sizeBuffer = new byte[4];
                        var headerBytesRead = await _stream.ReadAsync(sizeBuffer, 0, 4);
                        if (headerBytesRead == 0)
                        {
                            // 连接已断开
                            throw new IOException("Connection closed by remote host");
                        }
                        
                        int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
                        
                        // 读取完整数据
                        var data = new byte[dataSize];
                        int totalRead = 0;
                        var isImage = false;
                        
                        while (totalRead < dataSize)
                        {
                            int bytesRead = await _stream.ReadAsync(data, totalRead, 
                                Math.Min(ChunkSize, dataSize - totalRead));
                            
                            if (bytesRead == 0) break;
                            totalRead += bytesRead;
                            
                            // 检查是否为图片数据（只需检查一次）
                            if (totalRead > 10 && !isImage)
                            {
                                var partialJson = Encoding.UTF8.GetString(data, 0, Math.Min(totalRead, 100));
                                isImage = partialJson.Contains("\"Type\":1"); // 1 = ClipboardDataType.Image
                            }
                            
                            if (isImage)
                            {
                                ImageTransferProgress?.Invoke(this, (totalRead, dataSize));
                            }
                        }
                        
                        if (totalRead == dataSize)
                        {
                            var json = Encoding.UTF8.GetString(data);
                            var clipboardData = JsonSerializer.Deserialize<ClipboardData>(json);
                            if (clipboardData != null)
                            {
                                DataReceived?.Invoke(this, clipboardData);
                            }
                        }
                    }
                    catch (IOException)
                    {
                        // 连接已断开
                        ConnectionStatusChanged?.Invoke(this, "连接已断开");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("通信错误", ex);
                ConnectionStatusChanged?.Invoke(this, "连接已断开");
            }
            finally
            {
                CleanupConnection();
            }
        }

        private string ComputeHash(byte[] data)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}