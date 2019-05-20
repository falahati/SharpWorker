using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading.Tasks;
using ConsoleUtilities;
using Ionic.Zlib;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using SharpWorker.DataBackup;
using SharpWorker.DataStore.LiteDB;
using SharpWorker.Log;
using SharpWorker.NetCoordinator.DotNetZip;
using SharpWorker.NetCoordinator.WebApi;

namespace SharpWorker.NetCoordinator
{
    internal class Program
    {
        public static ConsoleWriter ConsoleWriter { get; private set; }
        public static Coordinator Coordinator { get; private set; }
        public static LiteDBDataStore DataStore { get; private set; }
        public static LocalLogger Logger { get; private set; }
        public static Settings Settings { get; private set; }

        public static WebApiProxyControllerService WebAPIService { get; set; }

        private static void CurrentDomainOnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            try
            {
                if (unhandledExceptionEventArgs.ExceptionObject is Exception exception)
                {
                    Logger.Log(nameof(CurrentDomainOnUnhandledException), LogType.Fatal, exception);
                }
                else
                {
                    Logger.Log(nameof(CurrentDomainOnUnhandledException), LogType.Fatal,
                        unhandledExceptionEventArgs.ExceptionObject?.ToString() ??
                        "Fatal unhandled exception occurred.");
                }
            }
            catch (Exception e)
            {
                Logger.Log(nameof(CurrentDomainOnUnhandledException), LogType.Error, e);
            }

            Environment.Exit(-1);
        }

        private static void Initialize(string settingsFile)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            ServicePointManager.DefaultConnectionLimit = 1000;

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new JsonInheritanceConverter());

                return settings;
            };

            Coordinator.WorkerResolver = new WorkerResolver();
            Coordinator.WorkerResolver.GetWorkerTypes();

            ConsoleWriter = new ConsoleWriter();
            Settings = CoordinatorSettings.Open<Settings>(settingsFile);
            DataStore = new LiteDBDataStore(Settings.DataDirectory);
            Logger = new LocalLogger(
                DataStore,
                ConsoleWriter,
                string.Format("{0}.log", DateTime.UtcNow.ToString("s").Replace(":", "-")),
                Settings.FileLogLevel
            );

            WebAPIService = new WebApiProxyControllerService("api");

            Coordinator = new Coordinator(Settings, Logger, DataStore, WebAPIService);

            if (Coordinator.Workers.FirstOrDefault(worker => worker.Worker.GetType() == typeof(DataBackupWorker))
                ?.Worker is DataBackupWorker dataBackup)
            {
                dataBackup.BackupArchiver = new DotNetZipBackupArchiver
                {
                    Zip64 = true,
                    CompressionLevel = CompressionLevel.BestSpeed
                };
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private static void Main(string[] args)
        {
            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception e)
            {
                ConsoleWriter.Default.WriteException(e);
            }

            new Terminal(Coordinator, ConsoleWriter, Logger).Run();
        }

        private static async Task MainAsync(string[] args)
        {
            try
            {
                Initialize(args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : null);
                StartAPI();
                await StartWorkers().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger?.Log(nameof(MainAsync), LogType.Fatal, e);
            }
            finally
            {
                Coordinator.FlushSettings();
            }
        }

        private static void StartAPI()
        {
            try
            {
                if (Settings.IsAPIEnable && !string.IsNullOrWhiteSpace(Settings.JWTSecret))
                {
                    WebApiJWTPayload.JWTSecret = Settings.JWTSecret;
                    WebApp.Start(string.Format("http://{0}:{1}", Settings.ApiInterface, Settings.ApiPort),
                        appBuilder =>
                        {
                            new WebApiStartup(WebAPIService, Logger, Settings.IsSwaggerEnable,
                                Settings.IsSwaggerEnable).Configuration(appBuilder);
                        });
                }
                else
                {
                    Logger.Log((string) null, nameof(StartAPI), LogType.Warning, "API endpoint is disabled.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(StartAPI), LogType.Error, ex);
            }

            Coordinator.FlushSettings();
        }

        private static async Task StartWorkers()
        {
            if (Settings.StartWorkers)
            {
                Logger.Log((string) null, nameof(StartWorkers), LogType.Info,
                    "{0} workers loaded. Starting in 2 seconds ...", Coordinator.Workers.Length);

                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                Logger.Log((string) null, nameof(StartWorkers), LogType.Info, "Starting workers ...");

                var _ = Coordinator.StartAll(true).ContinueWith(async task =>
                {
                    await task.ConfigureAwait(false);
                    Logger.Log((string) null, nameof(StartWorkers), LogType.Info, "{0} workers started.",
                        Coordinator.Workers.Length);
                    Coordinator.FlushSettings();
                });

                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            else
            {
                Logger.Log((string) null, nameof(StartWorkers), LogType.Info, "{0} workers loaded.",
                    Coordinator.Workers.Length);
            }
        }

        private static void TaskSchedulerOnUnobservedTaskException(
            object sender,
            UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            try
            {
                Logger.Log(nameof(TaskSchedulerOnUnobservedTaskException), LogType.Warning,
                    unobservedTaskExceptionEventArgs.Exception);

                if (!unobservedTaskExceptionEventArgs.Observed)
                {
                    unobservedTaskExceptionEventArgs.SetObserved();
                }
            }
            catch (Exception e)
            {
                Logger?.Log(nameof(TaskSchedulerOnUnobservedTaskException), LogType.Warning, e);
            }
        }
    }
}