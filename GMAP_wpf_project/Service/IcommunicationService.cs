using GMAP_wpf_project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMAP_wpf_project.Models;

namespace GMAP_wpf_project.Service
{
    public interface IcommunicationService
    {
        event EventHandler<GpsData> DataReceived;
        event EventHandler<string> ErrorOccurred;
        event EventHandler<string> StatusChanged;

        bool IsConnected { get; }
        string ConnectionInfo { get; }

        bool Connect(string connectionString);
        void Disconnect();
        void SendData(string data);
    }
}
