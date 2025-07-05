using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace GMAP_wpf_project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort serialPort;
        private bool isConnected = false;
        private int dataCount = 0;
        private Random random = new Random();
        private Timer simulationTimer;
        private List<PointLatLng> receivedPoints = new List<PointLatLng>();

        public MainWindow()
        {
            InitializeComponent();
            //XAML 파일 로드: MainWindow.xaml의 UI 요소들을 메모리에 로드
            // UI 요소 연결: x: Name으로 지정한 컨트롤들을 C# 변수와 연결
            // 이벤트 연결: XAML에서 정의한 이벤트 핸들러들을 연결
            InitializeMap();
            LoadSerialPorts();
        }

        private void InitializeMap()
        {
            try
            {
                // 구글맵 설정
                MainMap.MapProvider = GoogleMapProvider.Instance;
                GMaps.Instance.Mode = AccessMode.ServerOnly;

                // 서울 중심으로 설정
                MainMap.Position = new PointLatLng(37.5665, 126.9780);
                MainMap.Zoom = 12;

                // 마커 레이어 초기화
                MainMap.Markers.Clear();

                AddLogMessage("지도 초기화 완료 - 서울 중심");
            }
            catch (Exception ex)
            {
                AddLogMessage($"지도 초기화 실패: {ex.Message}");
            }
        }

        private void LoadSerialPorts()
        {
            ComboBoxPorts.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                ComboBoxPorts.Items.Add(port);
            }

            if (ComboBoxPorts.Items.Count > 0)
            {
                ComboBoxPorts.SelectedIndex = 0;
            }

            AddLogMessage($"시리얼 포트 검색 완료: {ports.Length}개 발견");
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxPorts.SelectedItem == null)
            {
                MessageBox.Show("시리얼 포트를 선택해주세요.");
                return;
            }

            try
            {
                string selectedPort = ComboBoxPorts.SelectedItem.ToString();

                serialPort = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                isConnected = true;
                UpdateConnectionStatus("연결됨", Colors.Green);

                ButtonConnect.IsEnabled = false;
                ButtonDisconnect.IsEnabled = true;
                ComboBoxPorts.IsEnabled = false;

                AddLogMessage($"시리얼 포트 {selectedPort} 연결 성공");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시리얼 포트 연결 실패: {ex.Message}");
                AddLogMessage($"연결 실패: {ex.Message}");
            }
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort.Dispose();
                }

                isConnected = false;
                UpdateConnectionStatus("연결안됨", Colors.Red);

                ButtonConnect.IsEnabled = true;
                ButtonDisconnect.IsEnabled = false;
                ComboBoxPorts.IsEnabled = true;

                AddLogMessage("시리얼 포트 연결 해제");
            }
            catch (Exception ex)
            {
                AddLogMessage($"연결 해제 실패: {ex.Message}");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadLine().Trim();

                // GPS 데이터 파싱 (예: "37.5665,126.9780" 형태)
                if (TryParseGpsData(data, out double latitude, out double longitude))
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddPointToMap(latitude, longitude);
                        UpdateDataLabels(latitude, longitude);
                        AddLogMessage($"GPS 데이터 수신: {latitude:F6}, {longitude:F6}");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLogMessage($"잘못된 데이터 형식: {data}");
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    AddLogMessage($"데이터 처리 오류: {ex.Message}");
                });
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

                    // 유효한 GPS 좌표 범위 확인
                    if (latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // 파싱 실패
            }

            return false;
        }

        private void AddPointToMap(double latitude, double longitude)
        {
            var point = new PointLatLng(latitude, longitude);
            receivedPoints.Add(point);

            // 마커 생성
            var marker = new GMapMarker(point)
            {
                Shape = new System.Windows.Shapes.Ellipse()
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                }
            };

            MainMap.Markers.Add(marker);

            // 지도 중심을 최신 포인트로 이동
            MainMap.Position = point;

            dataCount++;
        }

        private void UpdateDataLabels(double latitude, double longitude)
        {
            LabelLatitude.Content = $"위도: {latitude:F6}";
            LabelLongitude.Content = $"경도: {longitude:F6}";
            LabelDataCount.Content = $"데이터 개수: {dataCount}";
            LabelLastUpdate.Content = $"마지막 업데이트: {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateConnectionStatus(string status, Color color)
        {
            LabelStatus.Content = status;
            LabelStatus.Foreground = new SolidColorBrush(color);
        }

        private void AddLogMessage(string message)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            TextBlockLog.Text += logEntry;

            // 로그 스크롤을 맨 아래로
            var scrollViewer = (ScrollViewer)TextBlockLog.Parent;
            scrollViewer.ScrollToEnd();
        }

        private void ButtonClearMap_Click(object sender, RoutedEventArgs e)
        {
            MainMap.Markers.Clear();
            receivedPoints.Clear();
            dataCount = 0;

            LabelLatitude.Content = "위도: -";
            LabelLongitude.Content = "경도: -";
            LabelDataCount.Content = "데이터 개수: 0";
            LabelLastUpdate.Content = "마지막 업데이트: -";

            AddLogMessage("지도 및 데이터 초기화 완료");
        }

        private void ButtonSimulate_Click(object sender, RoutedEventArgs e)
        {
            if (simulationTimer != null)
            {
                simulationTimer.Dispose();
                simulationTimer = null;
                ButtonSimulate.Content = "시뮬레이션";
                AddLogMessage("시뮬레이션 중지");
                return;
            }

            // 서울 근처 랜덤 좌표 생성 시뮬레이션
            simulationTimer = new Timer(SimulateGpsData, null, 0, 2000); // 2초마다
            ButtonSimulate.Content = "시뮬레이션 중지";
            AddLogMessage("GPS 데이터 시뮬레이션 시작");
        }

        private void SimulateGpsData(object state)
        {
            // 서울 근처 랜덤 좌표 생성
            double baseLat = 37.5665;
            double baseLng = 126.9780;

            double latitude = baseLat + (random.NextDouble() - 0.5) * 0.1; // ±0.05도 범위
            double longitude = baseLng + (random.NextDouble() - 0.5) * 0.1;

            Dispatcher.Invoke(() =>
            {
                AddPointToMap(latitude, longitude);
                UpdateDataLabels(latitude, longitude);
                AddLogMessage($"시뮬레이션 데이터: {latitude:F6}, {longitude:F6}");
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 리소스 정리
            simulationTimer?.Dispose();

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort.Dispose();
            }
        }
    }
}