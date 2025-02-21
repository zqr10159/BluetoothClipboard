using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BluetoothClipboard.Models;
using BluetoothClipboard.Services;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace BluetoothClipboard
{
    public partial class MainWindow : Window
    {
        private Services.BluetoothService? _bluetoothService;
        // ClipboardService is assumed to handle clipboard events; ensure you have implemented it appropriately
        private readonly ClipboardService _clipboardService;
        private readonly AppSettings _settings;
        private bool _isAutoConnecting;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _clipboardService = new ClipboardService();
            _clipboardService.ClipboardUpdated += ClipboardService_ClipboardUpdated;
            
            // 启动剪贴板监控
            _clipboardService.StartMonitoring();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            // 服务器模式下的按钮状态
            ServerButton.IsEnabled = !_isConnected;
            
            // 客户端模式下的按钮状态
            ConnectButton.IsEnabled = !_isConnected && DevicesComboBox.SelectedItem != null;
            DisconnectButton.IsEnabled = _isConnected;
            
            // 扫描设备按钮和设备列表只在未连接时可用
            RefreshButton.IsEnabled = !_isConnected;
            DevicesComboBox.IsEnabled = !_isConnected;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesList();
            
            // 如果有上次连接的设备，尝试自动连接
            if (!string.IsNullOrEmpty(_settings.LastConnectedDeviceAddress))
            {
                _isAutoConnecting = true;
                var lastDevice = DevicesComboBox.Items.Cast<BluetoothDeviceWrapper>()
                    .FirstOrDefault(d => d.Address == _settings.LastConnectedDeviceAddress);
                
                if (lastDevice != null)
                {
                    DevicesComboBox.SelectedItem = lastDevice;
                    await ConnectToSelectedDevice();
                }
                _isAutoConnecting = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_bluetoothService != null)
            {
                // 保存当前连接的设备信息
                if (DevicesComboBox.SelectedItem is BluetoothDeviceWrapper device)
                {
                    _settings.LastConnectedDeviceAddress = device.Address;
                    _settings.LastConnectedDeviceName = device.Name;
                    _settings.Save();
                }
                
                _bluetoothService.Disconnect();
            }
        }

        private async Task ConnectToSelectedDevice()
        {
            if (DevicesComboBox.SelectedItem is not BluetoothDeviceWrapper selectedDevice)
                return;

            try
            {
                _isConnected = true;
                UpdateButtonStates();

                _bluetoothService = new Services.BluetoothService(BluetoothRole.Client);
                _bluetoothService.ConnectionStatusChanged += BluetoothService_ConnectionStatusChanged;
                _bluetoothService.DataReceived += BluetoothService_DataReceived;
                _bluetoothService.ImageTransferProgress += BluetoothService_ImageTransferProgress;

                await _bluetoothService.ConnectToDeviceAsync(selectedDevice.Device);
                
                if (!_isAutoConnecting)
                {
                    _settings.LastConnectedDeviceAddress = selectedDevice.Address;
                    _settings.Save();
                }
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"[Error] 连接失败: {ex.Message}\n");
                _isConnected = false;
                UpdateButtonStates();
            }
        }

        private async Task RefreshDevicesList()
        {
            try
            {
                var client = new BluetoothClient();
                var devices = await Task.Run(() => client.DiscoverDevices(10)
                    .Select(d => new BluetoothDeviceWrapper(d))
                    .ToList());

                DevicesComboBox.ItemsSource = devices;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新设备列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesList();
        }

        private async void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isConnected = true;
                UpdateButtonStates();
                
                _bluetoothService = new Services.BluetoothService(BluetoothRole.Server);
                _bluetoothService.ConnectionStatusChanged += BluetoothService_ConnectionStatusChanged;
                _bluetoothService.DataReceived += BluetoothService_DataReceived;

                await _bluetoothService.StartServerAsync();
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"[Error] 启动服务器失败: {ex.Message}\n");
                _isConnected = false;
                UpdateButtonStates();
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bluetoothService?.Disconnect();
                _bluetoothService = null;
                _isConnected = false;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"[Error] 断开连接失败: {ex.Message}\n");
            }
        }

        private void BluetoothService_ConnectionStatusChanged(object? sender, string e)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[Status] {e}\n");
                // 根据状态消息更新连接状态
                if (e.Contains("断开"))
                {
                    _isConnected = false;
                    _bluetoothService = null;  // 清除断开的服务实例
                }
                else if (e.Contains("等待"))
                {
                    _isConnected = false;
                }
                else if (e.Contains("已连接"))
                {
                    _isConnected = true;
                }
                UpdateButtonStates();
            });
        }

        private void BluetoothService_DataReceived(object? sender, ClipboardData data)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[Received] {GetDataDescription(data)}\n");
                // 更新本地剪贴板，但不触发 ClipboardChanged 事件
                _clipboardService.UpdateClipboardWithoutNotification(data);
            });
        }

        private void ClipboardService_ClipboardUpdated(object? sender, ClipboardData data)
        {
            Dispatcher.Invoke(async () =>
            {
                LogTextBox.AppendText($"[Clipboard] {GetDataDescription(data)}\n");

                // Optionally send clipboard data through Bluetooth
                if (_bluetoothService != null)
                {
                    try
                    {
                        await _bluetoothService.SendDataAsync(data);
                    }
                    catch (Exception ex)
                    {
                        LogTextBox.AppendText($"[Error] 发送剪贴板数据失败: {ex.Message}\n");
                    }
                }
            });
        }

        private string GetDataDescription(ClipboardData data)
        {
            return data.Type switch
            {
                ClipboardDataType.Text => $"文本: {data.Text?.Substring(0, Math.Min(50, data.Text.Length))}...",
                ClipboardDataType.Image => "图片数据",
                ClipboardDataType.Files => $"文件: {string.Join(", ", data.FilePaths ?? Array.Empty<string>())}",
                _ => "未知数据类型"
            };
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToSelectedDevice();
        }

        private void BluetoothService_ImageTransferProgress(object? sender, (int current, int total) e)
        {
            Dispatcher.Invoke(() =>
            {
                var percent = (int)((double)e.current / e.total * 100);
                LogTextBox.AppendText($"\r图片传输进度: {percent}%");
                LogTextBox.ScrollToEnd();
            });
        }
    }
}