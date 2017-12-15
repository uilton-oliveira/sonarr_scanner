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
        public readonly Settings Settings;
        CancellationToken cancellationToken;
        DateTime lastCheck = DateTime.UtcNow;

        public Monitor(Settings settings, CancellationToken cancellationToken)
        {
            this.Settings = settings;
            this.cancellationToken = cancellationToken;
        }

        public void ScanNow()
        {
            if (Settings.APIKey == null || Settings.APIKey.Trim() == "")
            {
                return;
            }

            Thread thread = new Thread(
                        delegate ()
                        {
                            Scan();
                        }
                    );
            thread.Start();
        }
        public bool Init()
        {
            if (Settings.APIKey == null || Settings.APIKey.Trim() == "")
            {
                Console.WriteLine($"APIKey not defined, aborting on {Settings.Provider()}, set it on {Settings.FileName()}");
                return false;
            }

            Console.WriteLine($"Starting Monitor to {Settings.Provider()} on URL: {Settings.URL}");

            // wake up scan
            if (Settings.ScanOnWake)
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
                                Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).Wait();
                            }
                        }
                    );
                thread.Start();
            }

            // timed scan
            if (Settings.ScanOnInterval)
            {
                Thread thread = new Thread(
                        delegate ()
                        {
                            Console.WriteLine("Timed Scan started.");
                            while (true)
                            {
                                Task.Delay(TimeSpan.FromMinutes(Settings.Interval), cancellationToken).Wait();
                                Scan();
                            }
                        }
                    );
                thread.Start();
            }

            // startup scan
            if (Settings.ScanOnStart)
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

            return Settings.ScanOnWake || Settings.ScanOnInterval;
        }

        private void Scan()
        {
            var rawJson = Get($"/api/wanted/missing?pageSize=50&apikey={Settings.APIKey}");
            Console.WriteLine($"{Settings.Provider()} GET Result: {rawJson}");
            dynamic task = JObject.Parse(rawJson);

            List<dynamic> searchIds = new List<dynamic>();
            foreach (dynamic record in task.records)
            {
                if (Settings.Provider() == Settings.NAME_SONARR)
                    Debug.WriteLine($"EP ID: {record.id} / Name: {record.series.title} / Season: {record.seasonNumber} / Episode: {record.episodeNumber}");
                else
                    Debug.WriteLine($"Movie ID: {record.id} / Name: {record.title} / Year: {record.year} / Status: {record.status}");
                searchIds.Add(record.id);
            }

            dynamic dyn = new ExpandoObject();
            if (Settings.Provider() == Settings.NAME_SONARR)
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

            Debug.WriteLine($"Sending {Settings.Provider()} POST: {postJson}");
            string commandOutput = Post($"/api/command?apikey={Settings.APIKey}", postJson);
            Console.WriteLine($"{Settings.Provider()} POST Result: {commandOutput}");
        }

        private string Get(string queryString)
        {

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"{Settings.URL}{queryString}";
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
                        var url = $"{Settings.URL}{queryString}";
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
