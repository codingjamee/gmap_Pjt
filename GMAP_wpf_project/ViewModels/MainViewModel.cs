using GMap.NET;
using GMAP_wpf_project.Commands;
using GMAP_wpf_project.Models;
using GMAP_wpf_project.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GMAP_wpf_project.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private IcommunicationService _currentService;
        private string _selectedConnectionType = "Serial";
        private string _connectionParameter = "";
        private bool _isConnected;
        private string _connectionStatus = "연결안됨";
        private Color _statusColor = Colors.Red;
        private string _logText = "";
        private double _currentLatitude;
        private double _currentLongitude;
        private int _dataCount;
        private string _lastUpdate = "-";
        private PointLatLng _mapCenter = new PointLatLng(37.5665, 126.9780);

        public ObservableCollection<string> ConnectionTypes { get; } = new ObservableCollection<string>
        {
            "Serial", "UDP", "TCP", "Simulation"
        };

        public ObservableCollection<string> SerialPorts { get; } = new ObservableCollection<string>();
        public ObservableCollection<PointLatLng> GpsPoints { get; } = new ObservableCollection<PointLatLng>();

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ClearMapCommand { get; }
        public ICommand RefreshPortsCommand { get; }

        public string SelectedConnectionType
        {
            get => _selectedConnectionType;
            set
            {
                _selectedConnectionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSerialSelected));
                OnPropertyChanged(nameof(IsUdpSelected));
                OnPropertyChanged(nameof(IsTcpSelected));
                OnPropertyChanged(nameof(IsSimulationSelected));
                OnPropertyChanged(nameof(ConnectionParameterLabel));
            }
        }

        public string ConnectionParameter
        {
            get => _connectionParameter;
            set
            {
                _connectionParameter = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public Color StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public double CurrentLatitude
        {
            get => _currentLatitude;
            set
            {
                _currentLatitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LatitudeDisplay));
            }
        }

        public double CurrentLongitude
        {
            get => _currentLongitude;
            set
            {
                _currentLongitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LongitudeDisplay));
            }
        }

        public int DataCount
        {
            get => _dataCount;
            set
            {
                _dataCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataCountDisplay));
            }
        }

        public string LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUpdateDisplay));
            }
        }

        public PointLatLng MapCenter
        {
            get => _mapCenter;
            set
            {
                _mapCenter = value;
                OnPropertyChanged();
            }
        }

        // UI 표시용 속성들
        public bool IsSerialSelected => SelectedConnectionType == "Serial";
        public bool IsUdpSelected => SelectedConnectionType == "UDP";
        public bool IsTcpSelected => SelectedConnectionType == "TCP";
        public bool IsSimulationSelected => SelectedConnectionType == "Simulation";

        public string ConnectionParameterLabel => SelectedConnectionType switch
        {
            "Serial" => "포트:",
            "UDP" => "포트 (예: 1234):",
            "TCP" => "연결 (예: listen:1234 또는 192.168.1.100:1234):",
            "Simulation" => "간격(ms):",
            _ => "매개변수:"
        };

        public string LatitudeDisplay => $"위도: {CurrentLatitude:F6}";
        public string LongitudeDisplay => $"경도: {CurrentLongitude:F6}";
        public string DataCountDisplay => $"데이터 개수: {DataCount}";
        public string LastUpdateDisplay => $"마지막 업데이트: {LastUpdate}";

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);
            ClearMapCommand = new RelayCommand(ClearMap);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);

            RefreshSerialPorts();
            AddLogMessage("애플리케이션 시작");
        }

        private bool CanConnect(object parameter)
        {
            return !IsConnected && !string.IsNullOrEmpty(ConnectionParameter);
        }

        private bool CanDisconnect(object parameter)
        {
            return IsConnected;
        }

        private void Connect(object parameter)
        {
            try
            {
                _currentService = CreateCommunicationService(SelectedConnectionType);
                if (_currentService == null)
                {
                    AddLogMessage("지원하지 않는 통신 방식입니다.");
                    return;
                }

                _currentService.DataReceived += OnDataReceived;
                _currentService.ErrorOccurred += OnErrorOccurred;
                _currentService.StatusChanged += OnStatusChanged;

                if (_currentService.Connect(ConnectionParameter))
                {
                    IsConnected = true;
                    ConnectionStatus = "연결됨";
                    StatusColor = Colors.Green;
                    AddLogMessage($"{SelectedConnectionType} 연결 성공: {_currentService.ConnectionInfo}");
                }
                else
                {
                    AddLogMessage($"{SelectedConnectionType} 연결 실패");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"연결 오류: {ex.Message}");
            }
        }

        private void Disconnect(object parameter)
        {
            try
            {
                if (_currentService != null)
                {
                    _currentService.DataReceived -= OnDataReceived;
                    _currentService.ErrorOccurred -= OnErrorOccurred;
                    _currentService.StatusChanged -= OnStatusChanged;
                    _currentService.Disconnect();
                    _currentService = null;
                }

                IsConnected = false;
                ConnectionStatus = "연결안됨";
                StatusColor = Colors.Red;
                AddLogMessage("연결 해제됨");
            }
            catch (Exception ex)
            {
                AddLogMessage($"연결 해제 오류: {ex.Message}");
            }
        }

        private void ClearMap(object parameter)
        {
            GpsPoints.Clear();
            DataCount = 0;
            CurrentLatitude = 0;
            CurrentLongitude = 0;
            LastUpdate = "-";
            AddLogMessage("지도 및 데이터 초기화 완료");
        }

        private void RefreshPorts(object parameter)
        {
            RefreshSerialPorts();
        }

        private IcommunicationService CreateCommunicationService(string type)
        {
            return type switch
            {
                "Serial" => new SerialCommunicationService(),
                "UDP" => new UdpCommunicationService(),
                "TCP" => new TcpCommunicationService(),
                "Simulation" => new SimulationService(),
                _ => null
            };
        }

        private void OnDataReceived(object sender, GpsData gpsData)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (gpsData.IsValid())
                {
                    CurrentLatitude = gpsData.Latitude;
                    CurrentLongitude = gpsData.Longitude;
                    LastUpdate = gpsData.Timestamp.ToString("HH:mm:ss");
                    DataCount++;

                    var point = new PointLatLng(gpsData.Latitude, gpsData.Longitude);
                    GpsPoints.Add(point);
                    MapCenter = point;

                    AddLogMessage($"GPS 데이터 수신 [{gpsData.Source}]: {gpsData.Latitude:F6}, {gpsData.Longitude:F6}");
                }
            });
        }

        private void OnErrorOccurred(object sender, string error)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AddLogMessage($"오류: {error}");
            });
        }

        private void OnStatusChanged(object sender, string status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AddLogMessage($"상태: {status}");
            });
        }

        private void RefreshSerialPorts()
        {
            SerialPorts.Clear();
            var ports = SerialCommunicationService.GetAvailablePorts();
            foreach (var port in ports)
            {
                SerialPorts.Add(port);
            }

            if (SerialPorts.Count > 0 && IsSerialSelected)
            {
                ConnectionParameter = SerialPorts[0];
            }

            AddLogMessage($"시리얼 포트 검색 완료: {ports.Length}개 발견");
        }

        private void AddLogMessage(string message)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            LogText += logEntry;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
