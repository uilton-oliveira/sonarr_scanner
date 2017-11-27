using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sonarr_Scanner
{
    class Monitor
    {
        Settings settings;
        CancellationToken cancellationToken;
        DateTime lastCheck = DateTime.UtcNow;

        public Monitor(Settings settings, CancellationToken cancellationToken)
        {
            this.settings = settings;
            this.cancellationToken = cancellationToken;
        }

        public bool Init()
        {
            if (settings.APIKey == null || settings.APIKey.Trim() == "")
            {
                Console.WriteLine($"APIKey not defined, aborting on {settings.Provider()}, set it on {settings.FileName()}");
                return false;
            }

            Console.WriteLine($"Starting Monitor to {settings.Provider()} on URL: {settings.URL}");

            // wake up scan
            if (settings.ScanOnWake)
            {
                Thread thread = new Thread(
                        delegate ()
                        {
                            Console.WriteLine("Wake UP Scan started.");
                            while (true)
                            {
                                if (DateTime.UtcNow > lastCheck.AddMinutes(5))
                                {
                                    Console.WriteLine("Wake from sleep!");
                                    Scan();
                                }
                                lastCheck = DateTime.UtcNow;
                                Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                            }
                        }
                    );
                thread.Start();
            }

            // timed scan
            if (settings.ScanOnInterval)
            {
                Thread thread = new Thread(
                        delegate ()
                        {
                            Console.WriteLine("Timed Scan started.");
                            while (true)
                            {
                                Task.Delay(TimeSpan.FromMinutes(settings.Interval), cancellationToken);
                                Scan();
                            }
                        }
                    );
                thread.Start();
            }

            // startup scan
            if (settings.ScanOnStart)
            {
                Thread thread = new Thread(
                        delegate ()
                        {
                            Console.WriteLine("Startup Scan started.");
                            Scan();
                        }
                    );
                thread.Start();
            }

            return settings.ScanOnWake || settings.ScanOnInterval;
        }

        private void Scan()
        {
            var rawJson = Get($"/api/wanted/missing?pageSize=50&apikey={settings.APIKey}");
            Console.WriteLine($"{settings.Provider()} GET Result: {rawJson}");
            dynamic task = JObject.Parse(rawJson);

            List<dynamic> searchIds = new List<dynamic>();
            foreach (dynamic record in task.records)
            {
                if (settings.Provider() == Settings.NAME_SONARR)
                    Debug.WriteLine($"EP ID: {record.id} / Name: {record.series.title} / Season: {record.seasonNumber} / Episode: {record.episodeNumber}");
                else
                    Debug.WriteLine($"Movie ID: {record.id} / Name: {record.title} / Year: {record.year} / Status: {record.status}");
                searchIds.Add(record.id);
            }

            dynamic dyn = new ExpandoObject();
            if (settings.Provider() == Settings.NAME_SONARR)
            {
                dyn.episodeIds = searchIds;
                dyn.name = "EpisodeSearch";
            }
            else
            {
                dyn.movieIds = searchIds;
                dyn.name = "MoviesSearch";
            }
            string postJson = JsonConvert.SerializeObject(dyn);

            Debug.WriteLine($"Sending {settings.Provider()} POST: {postJson}");
            string commandOutput = Post($"/api/command?apikey={settings.APIKey}", postJson);
            Console.WriteLine($"{settings.Provider()} POST Result: {commandOutput}");
        }

        private string Get(string queryString)
        {

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"{settings.URL}{queryString}";
                    Console.WriteLine($"Running GET: {url}");
                    // The actual Get method
                    return httpClient.GetAsync($"{url}").Result.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"GET Exception: {e.Message}");
                return "";
            }
        }

        private string Post(string queryString, string postData)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {

                    using (var content = new StringContent(postData))
                    {
                        // The actual Post method
                        var url = $"{settings.URL}{queryString}";
                        Console.WriteLine($"Running POST: {url}");
                        return httpClient.PostAsync($"{url}", content).Result.Content.ReadAsStringAsync().Result;

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"POST Exception: {e.Message}");
                return "";
            }
        }
    }
}
