using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace BluetoothClipboard.Models
{
    public class BluetoothDeviceWrapper
    {
        public BluetoothDeviceInfo Device { get; }
        public string Name => Device.DeviceName;
        public string Address => Device.DeviceAddress.ToString();

        public BluetoothDeviceWrapper(BluetoothDeviceInfo device)
        {
            Device = device;
        }

        public override string ToString()
        {
            return Name;
        }
    }
} 