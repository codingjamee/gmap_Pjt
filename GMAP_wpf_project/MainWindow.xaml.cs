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
using GMAP_wpf_project.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Shapes;

namespace GMAP_wpf_project
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();

            // ViewModel의 GPS 포인트 컬렉션 변경 이벤트 구독
            if (ViewModel != null)
            {
                ViewModel.GpsPoints.CollectionChanged += OnGpsPointsChanged;
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 로그 스크롤을 자동으로 맨 아래로 이동
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.LogText))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LogScrollViewer.ScrollToEnd();
                        }));
                    }
                };
            }
        }

        private void InitializeMap()
        {
            try
            {
                // GMap 전역 설정
                GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;

                // 지도 설정
                MainMap.MapProvider = GoogleMapProvider.Instance;
                MainMap.Position = new PointLatLng(37.5665, 126.9780); // 서울 중심
                MainMap.Zoom = 12;
                MainMap.Markers.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"지도 초기화 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnGpsPointsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (PointLatLng point in e.NewItems)
                {
                    AddMarkerToMap(point);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                MainMap.Markers.Clear();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.MapCenter))
            {
                // 지도 중심을 최신 위치로 이동
                // MainMap.Position = ViewModel.MapCenter;
                var newCenter = ViewModel.MapCenter;
                if (Math.Abs(MainMap.Position.Lat - newCenter.Lat) > 0.000001 ||
                    Math.Abs(MainMap.Position.Lng - newCenter.Lng) > 0.000001)
                {
                    MainMap.Position = newCenter;
                }
            }
        }

        private void AddMarkerToMap(PointLatLng point)
        {
            // 마커 생성
            var marker = new GMapMarker(point)
            {
                Shape = new Ellipse()
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                }
            };

            MainMap.Markers.Add(marker);

            // 마커가 너무 많아지면 오래된 것부터 제거 (성능 최적화)
            if (MainMap.Markers.Count > 1000)
            {
                MainMap.Markers.RemoveAt(0);
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // 리소스 정리
            ViewModel?.DisconnectCommand?.Execute(null);
            base.OnClosing(e);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            OnClosing(e);
        }
    }
}