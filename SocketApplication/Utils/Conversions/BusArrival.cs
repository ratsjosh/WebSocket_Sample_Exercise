using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocketApplication.Utils.Conversions
{
    public class BusArrival
    {
        public class ServiceInformation
        {
            [JsonProperty(PropertyName = "odata.metadata")]
            public string Metadata { get; set; }
            public string BusStopID { get; set; }
            public List<Service> Services { get; set; }
        }

        public enum Operation { InOperation, NotInOperation };

        public class Service
        {
            public string ServiceNo { get; set; }
            public string Status { get; set; }
            public string Operator { get; set; }
            public string OriginatingID { get; set; }
            public string TerminatingID { get; set; }
            public Bus NextBus { get; set; }
            public Bus SubsequentBus { get; set; }
            public Bus SubsequentBus3 { get; set; }
        }

        public class Bus
        {
            public string EstimatedArrival { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string VisitNumber { get; set; }
            public string Load { get; set; }
            public string Feature { get; set; }
        }
    }
}
