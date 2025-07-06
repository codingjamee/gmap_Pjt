using GMAP_wpf_project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMAP_wpf_project.Service
{
    public class SimulationService : IcommunicationService
    {
        private Timer _simulationTimer;
        private Random _random = new Random();
        private bool _isRunning;
        private int _intervalMs;

        public event EventHandler<GpsData> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => _isRunning;
        public string ConnectionInfo => $"Simulation: {_intervalMs}ms interval";

        public bool Connect(string intervalMs)
        {
            try
            {
                _intervalMs = int.Parse(intervalMs);
                if (_intervalMs < 100) _intervalMs = 2000; // 최소 100ms

                _simulationTimer = new Timer(GenerateSimulationData, null, 0, _intervalMs);
                _isRunning = true;

                StatusChanged?.Invoke(this, $"GPS simulation started with {_intervalMs}ms interval");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Simulation start failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                _isRunning = false;
                _simulationTimer?.Dispose();
                _simulationTimer = null;

                StatusChanged?.Invoke(this, "GPS simulation stopped");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Simulation stop error: {ex.Message}");
            }
        }

        public void SendData(string data)
        {
            // 시뮬레이션에서는 송신 기능 없음
        }

        private void GenerateSimulationData(object state)
        {
            try
            {
                // 서울 근처 랜덤 좌표 생성
                double baseLat = 37.5665;
                double baseLng = 126.9780;

                double latitude = baseLat + (_random.NextDouble() - 0.5) * 0.1; // ±0.05도 범위
                double longitude = baseLng + (_random.NextDouble() - 0.5) * 0.1;

                var gpsData = new GpsData(latitude, longitude, "Simulation");
                DataReceived?.Invoke(this, gpsData);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Simulation data generation error: {ex.Message}");
            }
        }
    }
}
