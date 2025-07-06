using GMAP_wpf_project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GMAP_wpf_project.Service
{

    public class UdpCommunicationService : IcommunicationService
    {
        private UdpClient _udpClient;
        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isListening;

        public event EventHandler<GpsData> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => _isListening;
        public string ConnectionInfo => $"UDP: {_localEndPoint?.Port}";

        public bool Connect(string connectionString)
        {
            try
            {
                // connectionString 형식: "port" 또는 "localPort:remoteIP:remotePort"
                var parts = connectionString.Split(':');
                int localPort = int.Parse(parts[0]);

                _localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                _udpClient = new UdpClient(_localEndPoint);

                if (parts.Length == 3)
                {
                    // 송신용 원격 엔드포인트 설정
                    _remoteEndPoint = new IPEndPoint(IPAddress.Parse(parts[1]), int.Parse(parts[2]));
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _isListening = true;

                Task.Run(() => ListenForDataAsync(_cancellationTokenSource.Token));

                StatusChanged?.Invoke(this, $"UDP listening on port {localPort}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"UDP connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                _isListening = false;
                _cancellationTokenSource?.Cancel();
                _udpClient?.Close();
                _udpClient?.Dispose();
                _udpClient = null;

                StatusChanged?.Invoke(this, "UDP disconnected");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"UDP disconnect error: {ex.Message}");
            }
        }

        public void SendData(string data)
        {
            try
            {
                if (_udpClient != null && _remoteEndPoint != null)
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    _udpClient.Send(dataBytes, dataBytes.Length, _remoteEndPoint);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"UDP send error: {ex.Message}");
            }
        }

        private async Task ListenForDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isListening)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    string data = Encoding.UTF8.GetString(result.Buffer).Trim();

                    if (TryParseGpsData(data, out double latitude, out double longitude))
                    {
                        var gpsData = new GpsData(latitude, longitude, "UDP");
                        DataReceived?.Invoke(this, gpsData);
                    }
                    else
                    {
                        ErrorOccurred?.Invoke(this, $"Invalid GPS data format: {data}");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // UDP 클라이언트가 이미 해제됨
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke(this, $"UDP receive error: {ex.Message}");
                    }
                }
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
    }
}
