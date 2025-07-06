using GMAP_wpf_project.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GMAP_wpf_project.Service
{
    public class TcpCommunicationService : IcommunicationService
    {
        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isServer;
        private string _connectionInfo;

        public event EventHandler<GpsData> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => _tcpClient?.Connected ?? false;
        public string ConnectionInfo => $"TCP: {_connectionInfo}";

        public bool Connect(string connectionString)
        {
            try
            {
                // connectionString 형식: "server:port" (클라이언트) 또는 "listen:port" (서버)
                var parts = connectionString.Split(':');
                if (parts.Length != 2) return false;

                _cancellationTokenSource = new CancellationTokenSource();

                if (parts[0].ToLower() == "listen")
                {
                    // 서버 모드
                    return StartServer(int.Parse(parts[1]));
                }
                else
                {
                    // 클라이언트 모드
                    return ConnectToServer(parts[0], int.Parse(parts[1]));
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"TCP connection failed: {ex.Message}");
                return false;
            }
        }

        private bool StartServer(int port)
        {
            try
            {
                _isServer = true;
                _connectionInfo = $"Server:{port}";
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();

                Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));

                StatusChanged?.Invoke(this, $"TCP server listening on port {port}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"TCP server start failed: {ex.Message}");
                return false;
            }
        }

        private bool ConnectToServer(string host, int port)
        {
            try
            {
                _isServer = false;
                _connectionInfo = $"Client:{host}:{port}";
                _tcpClient = new TcpClient();
                _tcpClient.Connect(host, port);

                SetupClientStreams(_tcpClient);
                Task.Run(() => ReceiveDataAsync(_cancellationTokenSource.Token));

                StatusChanged?.Invoke(this, $"TCP connected to {host}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"TCP client connection failed: {ex.Message}");
                return false;
            }
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    _tcpClient = client;

                    SetupClientStreams(client);
                    StatusChanged?.Invoke(this, $"TCP client connected: {client.Client.RemoteEndPoint}");

                    Task.Run(() => ReceiveDataAsync(cancellationToken));
                }
                catch (ObjectDisposedException)
                {
                    // 리스너가 이미 해제됨
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke(this, $"TCP accept error: {ex.Message}");
                    }
                }
            }
        }

        private void SetupClientStreams(TcpClient client)
        {
            _networkStream = client.GetStream();
            _streamReader = new StreamReader(_networkStream, Encoding.UTF8);
            _streamWriter = new StreamWriter(_networkStream, Encoding.UTF8) { AutoFlush = true };
        }

        private async Task ReceiveDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                try
                {
                    string data = await _streamReader.ReadLineAsync();
                    if (data == null) break; // 연결 종료

                    data = data.Trim();
                    if (TryParseGpsData(data, out double latitude, out double longitude))
                    {
                        var gpsData = new GpsData(latitude, longitude, "TCP");
                        DataReceived?.Invoke(this, gpsData);
                    }
                    else
                    {
                        ErrorOccurred?.Invoke(this, $"Invalid GPS data format: {data}");
                    }
                }
                catch (IOException)
                {
                    // 연결 종료
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke(this, $"TCP receive error: {ex.Message}");
                    }
                }
            }

            StatusChanged?.Invoke(this, "TCP client disconnected");
        }

        public void Disconnect()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                _streamReader?.Close();
                _streamWriter?.Close();
                _networkStream?.Close();
                _tcpClient?.Close();
                _tcpListener?.Stop();

                _streamReader = null;
                _streamWriter = null;
                _networkStream = null;
                _tcpClient = null;
                _tcpListener = null;

                StatusChanged?.Invoke(this, "TCP disconnected");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"TCP disconnect error: {ex.Message}");
            }
        }

        public void SendData(string data)
        {
            try
            {
                if (_streamWriter != null && _tcpClient?.Connected == true)
                {
                    _streamWriter.WriteLine(data);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"TCP send error: {ex.Message}");
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
