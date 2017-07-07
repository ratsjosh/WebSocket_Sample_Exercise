using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SocketApplication.Utils.Conversions.BusArrival;

namespace SocketApplication.Data
{
    // API Source: https://www.mytransport.sg/content/mytransport/home/dataMall.html
    // API Documentation: https://www.mytransport.sg/content/dam/mytransport/DataMall_StaticData/LTA_DataMall_API_User_Guide.pdf
    public class BusInfo
    {

        public Information GetBusRequest(string[] extracts)
        {
            string stop = "";
            string bus = "";

            // Regex patterns to filter bus information
            string _stopPattern = @"\d{5}";
            string _busPattern = @"[A-Z]{1,2}\d{1,3}|(\d{1,3}([a-zA-Z]{1})?)(?!\d|\W)";

            // Instantiate the regular expression object.
            Regex r1 = new Regex(_stopPattern, RegexOptions.IgnoreCase);
            // Instantiate the regular expression object.
            Regex r2 = new Regex(_busPattern, RegexOptions.IgnoreCase);


            foreach (string extract in extracts)
            {
                if (r1.Match(extract).Success)
                    stop = extract;
                else if (r2.Match(extract).Success)
                    bus = extract;
            }
            if (!String.IsNullOrEmpty(stop) && !String.IsNullOrEmpty(bus))
                return new Information(stop, bus);
            else
                return null;
        }

        public async Task<ServiceInformation> GetBusInformationAsync(string stop, string bus)
        {
            string stopParam = "BusStopID=" + stop;
            string busParam = "ServiceNo=" + bus;
            using (var client = new HttpClient())
            {
                Uri uri = new Uri("http://datamall2.mytransport.sg/ltaodataservice/BusArrival?" + $"{stopParam}&{ busParam}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("AccountKey", "VfAAnVl4S4q+i1l4KzlLQg==");

                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<ServiceInformation>(await response.Content.ReadAsStringAsync());
            }
        }
    }
    

    public class Information
    {
        public Information(string s, string b)
        {
            StopNumber = s;
            BusNumber = b;
        }
        public string StopNumber = "";
        public string BusNumber = "";
    }
}
