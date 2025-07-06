using GMAP_wpf_project.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMAP_wpf_project.Models;

namespace GMAP_wpf_project.Service
{
    public class SerialCommunicationService : IcommunicationService
    {
        private SerialPort _serialPort;
        private string _portName;

        public event EventHandler<GpsData> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public string ConnectionInfo => $"Serial: {_portName}";

        public bool Connect(string portName)
        {
            try
            {
                _portName = portName;
                _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();

                StatusChanged?.Invoke(this, $"Serial port {portName} connected");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Serial connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                    StatusChanged?.Invoke(this, "Serial port disconnected");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Serial disconnect error: {ex.Message}");
            }
        }

        public void SendData(string data)
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.WriteLine(data);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Serial send error: {ex.Message}");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadLine().Trim();
                if (TryParseGpsData(data, out double latitude, out double longitude))
                {
                    var gpsData = new GpsData(latitude, longitude, "Serial");
                    DataReceived?.Invoke(this, gpsData);
                }
                else
                {
                    ErrorOccurred?.Invoke(this, $"Invalid GPS data format: {data}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Serial data processing error: {ex.Message}");
            }
        }

        private bool TryParseGpsData(string data, out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;

            try
            {
                string[] parts = data.Split(',');
                if (parts.Length == 2)
                {
                    latitude = double.Parse(parts[0]);
                    longitude = double.Parse(parts[1]);
                    return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
                }
            }
            catch
            {
                // 파싱 실패
            }

            return false;
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
