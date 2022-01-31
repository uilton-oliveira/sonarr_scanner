using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace sonarr_scanner
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

            var thread = new Thread(
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
                var thread = new Thread(
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
                var thread = new Thread(
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
            
            // timed force import scan
            if (Settings.ForceImport)
            {
                var thread = new Thread(
                    delegate ()
                    {
                        Console.WriteLine("Force Import started.");
                        while (true)
                        {
                            Task.Delay(TimeSpan.FromMinutes(Settings.ForceImportInterval), cancellationToken).Wait();
                            ForceImport();
                        }
                    }
                );
                thread.Start();
            }

            // startup scan
            if (Settings.ScanOnStart)
            {
                var thread = new Thread(
                        delegate ()
                        {
                            Console.WriteLine("Startup Scan started.");
                            Scan();
                            if (Settings.ForceImport)
                                ForceImport();
                        }
                    );
                thread.Start();
            }

            return Settings.ScanOnWake || Settings.ScanOnInterval || Settings.ScanOnStart;
        }

        private void ForceImport()
        {
            if (Settings.Provider() == Settings.NAME_RADAR) { return;} // not implemented yet to radarr
            
            var rawJson = Get($"/api/queue?sort_by=timeleft&order=asc&apikey={Settings.APIKey}");
            dynamic queues = JArray.Parse(rawJson);
            foreach (dynamic queue in queues)
            {
                if (queue.trackedDownloadStatus != "Warning") {continue;}

                
                Debug.WriteLine($"ForceImport title: {queue.series.title} / status: {queue.trackedDownloadStatus}");
                
                var downloadId = queue.downloadId;
                var episodeId = queue.episode.id;
                var serieId = queue.episode.seriesId;

                var manualimportJson = Get($"/api/manualimport?downloadId={downloadId}&sort_by=qualityWeight&order=desc&apikey={Settings.APIKey}");
                dynamic manualimports = JArray.Parse(manualimportJson);
                foreach (dynamic manual in manualimports)
                {
                    var rejectedPermanently = false;
                    foreach (dynamic rejection in manual.rejections)
                    {
                        Debug.WriteLine($"Rejection reason: \"{rejection.reason}\" / type: {rejection.type}");
                        if (rejection.type == "permanent")
                        {
                            rejectedPermanently = true;
                        }
                    }

                    if (!rejectedPermanently) {continue;}
                    var path = manual.path;
                    var files = new List<dynamic>();

                    dynamic file = new ExpandoObject();
                    file.path = path;
                    file.seriesId = serieId;
                    file.episodeIds = new List<dynamic> {episodeId};
                    file.quality = queue.quality;
                    file.downloadId = downloadId;
                    
                    files.Add(file);
                    
                    dynamic dyn = new ExpandoObject();
                    dyn.importMode = Settings.ForceImportMode;
                    dyn.name = "manualImport";
                    dyn.files = files;
                    
                    string postJson = JsonConvert.SerializeObject(dyn);

                    Debug.WriteLine($"Sending {Settings.Provider()} POST: {postJson}");
                    var commandOutput = Post($"/api/command?apikey={Settings.APIKey}", postJson);
                    Console.WriteLine($"{Settings.Provider()} POST Result: {commandOutput}");
                    
                }

            }
        }

        private void Scan()
        {
            var rawJson = Get($"/api/wanted/missing?pageSize=50&apikey={Settings.APIKey}");
            if (String.IsNullOrEmpty(rawJson)) {
                Console.WriteLine($"{Settings.Provider()} GET returned null or empty, skipping...");
                return;
            })
            Console.WriteLine($"{Settings.Provider()} GET sent");
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
