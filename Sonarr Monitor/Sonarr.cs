using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sonarr_Monitor
{
    class Sonarr
    {
        public static decimal currentInterval = -1;
        public static string currentApiKey = null;
        public static async Task<string> Get(string queryString)
        {
            using (var httpClient = new HttpClient())
            {
                string url = Properties.Settings.Default.url;

                // The actual Get method
                using (var result = await httpClient.GetAsync($"{url}{queryString}"))
                {
                    return await result.Content.ReadAsStringAsync();
                }
            }
        }

        public static async Task<string> Post(string queryString, string postData)
        {
            using (var httpClient = new HttpClient())
            {
                string url = Properties.Settings.Default.url;

                using (var content = new StringContent(postData))
                {
                    // The actual Get method
                    using (var result = await httpClient.PostAsync($"{url}{queryString}", content))
                    {
                        return await result.Content.ReadAsStringAsync();
                    }

                }
            }
        }

        public static async Task StartMonitor(TimeSpan interval, CancellationToken cancellationToken)
        {
            currentInterval = interval.Minutes;
            while (true)
            {
                await FindMissing();
                await Task.Delay(interval, cancellationToken);
            }
        }

        private static async Task FindMissing()
        {
            var rawJson = await Get($"/api/wanted/missing?apikey={Properties.Settings.Default.apiKey}");
            dynamic task = JObject.Parse(rawJson);
            Debug.WriteLine(rawJson);

            List<dynamic> episodeIds = new List<dynamic>();
            foreach (dynamic record in task.records)
            {
                Debug.WriteLine($"EP ID: {record.id} / Name: {record.series.title} / Season: {record.seasonNumber} / Episode: {record.episodeNumber}");
                episodeIds.Add(record.id);
            }

            dynamic dyn = new ExpandoObject();
            dyn.episodeIds = episodeIds;
            dyn.name = "EpisodeSearch";
            string postJson = JsonConvert.SerializeObject(dyn);

            Debug.WriteLine(postJson);
            string commandOutput = await Post($"/api/command?apikey={Properties.Settings.Default.apiKey}", postJson);
            Debug.WriteLine(commandOutput);
        }
    }
}
