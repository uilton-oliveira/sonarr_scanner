using Newtonsoft.Json;
using System;
using System.IO;

namespace Sonarr_Scanner
{
    public class Settings
    {
        public static Settings Sonarr;
        public static Settings Radarr;
        public string URL;
        public int Interval = 30;
        public bool ScanOnWake = true;
        public bool ScanOnInterval = false;
        public bool ScanOnStart = true;
        public string APIKey = "";
        private string filePath;
        private string fileName;
        private string name;

        public static readonly string NAME_RADAR = "Radarr";
        public static readonly string NAME_SONARR = "Sonarr";

        [JsonConstructor]
        private Settings()
        {
        }


        public static void Init()
        {
            Sonarr = new Settings("settings_sonarr.json", NAME_SONARR);
            Radarr = new Settings("settings_radarr.json", NAME_RADAR);
        }

        public string Provider()
        {
            return name;
        }

        public string FileName()
        {
            return fileName;
        }

        private Settings(string fileName, string name)
        {
            this.fileName = fileName;
            this.filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            this.name = name;
            Load();
            
            URL = URL ?? (name == NAME_SONARR ? "http://localhost:8989" : "http://localhost:7878");

            Save();
        }


        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public void Load()
        {
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText(filePath), this);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Config file not found to {name}");
            }
        }
    }
}
