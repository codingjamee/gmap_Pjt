using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMAP_wpf_project.Models
{
    public class GpsData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } // "Serial", "UDP", "TCP", "Simulation"

        public GpsData(double latitude, double longitude, string source = "Unknown")
        {
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = DateTime.Now;
            Source = source;
        }

        public bool IsValid()
        {
            return Latitude >= -90 && Latitude <= 90 &&
                   Longitude >= -180 && Longitude <= 180;
        }

        public override string ToString()
        {
            return $"[{Source}] {Latitude:F6}, {Longitude:F6} at {Timestamp:HH:mm:ss}";
        }
    }
}
