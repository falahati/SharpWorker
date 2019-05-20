using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using SharpWorker.DataBackup;
using SharpWorker.Health;

namespace SharpWorker
{
    public class CoordinatorSettings
    {
        private string _fileName = "";
        public string DataDirectory { get; set; } = "Data";
        public bool IsCoordinatorWebApiEnable { get; set; } = false;
        public bool IsDataStoreWebApiEnable { get; set; } = false;
        public bool IsLoggerWebApiEnable { get; set; } = false;

        public WorkerConfiguration[] Workers { get; set; } =
            {WorkerConfiguration.FromWorker<HealthWorker>(), WorkerConfiguration.FromWorker<DataBackupWorker>()};

        public static T Open<T>(string address = null) where T : CoordinatorSettings
        {
            address = Path.GetFullPath(address ?? Assembly.GetEntryAssembly().GetName().Name + ".json");

            if (File.Exists(address))
            {
                try
                {
                    using (var file = File.Open(address, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new StreamReader(file))
                        {
                            var result = JsonConvert.DeserializeObject<T>(reader.ReadToEnd(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Include,
                                TypeNameHandling = TypeNameHandling.All
                            });
                            result._fileName = address;

                            return result;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore?
                }
            }

            var fallbackResult = JsonConvert.DeserializeObject<T>("{}", new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                TypeNameHandling = TypeNameHandling.All
            });
            fallbackResult._fileName = address;

            return fallbackResult;
        }

        public bool Save(string address = null)
        {
            address = Path.GetFullPath(address ?? _fileName);

            try
            {
                using (var file = File.Open(address, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(file))
                    {
                        lock (this)
                        {
                            writer.Write(
                                JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Include,
                                    DefaultValueHandling = DefaultValueHandling.Populate,
                                    TypeNameHandling = TypeNameHandling.Auto
                                })
                            );
                        }

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}