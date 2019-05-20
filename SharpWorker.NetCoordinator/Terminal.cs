using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleUtilities;

namespace SharpWorker.NetCoordinator
{
    internal class Terminal
    {
        public Terminal(Coordinator coordinator, ConsoleWriter consoleWriter, LocalLogger logger)
        {
            Logger = logger;
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            ConsoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        }

        public ConsoleWriter ConsoleWriter { get; }
        public Coordinator Coordinator { get; }
        public LocalLogger Logger { get; }

        // ReSharper disable once TooManyDeclarations
        // ReSharper disable once MethodNameNotMeaningful
        public void Run()
        {
            Logger.FilterClear();

            new ConsoleTerminal(nameof(SharpWorker), new Dictionary<string, Action<string[]>>
            {
                {
                    "", commandArgs => TerminalHelp()
                },
                {
                    "list", commandArgs => TerminalListWorkers()
                },
                {
                    "types", commandArgs => TerminalListWorkerTypes()
                },
                {
                    "add", commandArgs => TerminalAddWorker(string.Join(" ", commandArgs))
                },
                {
                    "remove", TerminalRemoveWorker
                },
                {
                    "flushconfig", commandArgs => TerminalFlushConfig()
                },
                {
                    "stop", TerminalStopWorkers
                },
                {
                    "start", TerminalStartWorkers
                },
                {
                    "restart", TerminalRestartWorkers
                },
                {
                    "stopall", commandArgs => TerminalStopAllWorkers()
                },
                {
                    "startall", commandArgs => TerminalStartAllWorkers()
                },
                {
                    "logs", commandArgs => TerminalLogs(string.Join(" ", commandArgs))
                },
                {
                    "debug", commandArgs => TerminalToggleDebug()
                },
                {
                    "connect",
                    commandArgs => new WorkerTerminal(Coordinator, commandArgs.FirstOrDefault(), ConsoleWriter, Logger)
                        .Run()
                }
            }).RunTerminal();
        }

        private void TerminalAddWorker(string workerType)
        {
            if (string.IsNullOrWhiteSpace(workerType))
            {
                ConsoleWriter.PrintError($"Bad worker name.");

                return;
            }

            var type = workerType.GetTypeFromSimplifiedName();

            if (type == null || !typeof(IWorker).IsAssignableFrom(type))
            {
                ConsoleWriter.PrintError($"Invalid worker type.");

                return;
            }

            var workerId = Coordinator.AddWorker(WorkerConfiguration.FromWorker(type));
            ConsoleWriter.PrintSuccess($"Worker {workerId} added.");
        }


        private void TerminalRemoveWorker(string[] workerIds)
        {
            var workers = Coordinator.Workers.Where(worker =>
                    workerIds.Any(s => s.Equals(worker.Id, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            if (workers.Length == 0)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            foreach (var worker in workers)
            {
                if (Coordinator.DeleteWorker(worker.Id))
                {
                    ConsoleWriter.PrintSuccess($"Worker {worker.Id} deleted.");
                }
                else
                {
                    ConsoleWriter.PrintWarning($"Failed to delete worker {worker.Id}.");
                }
            }
        }

        private void TerminalFlushConfig()
        {
            Coordinator.FlushSettings();
        }

        private void TerminalHelp()
        {
            var help = new[]
            {
                "list - Lists all active workers",
                "types - Lists all possible worker types",
                "logs - Resets log filter and returns a list of valid subjects",
                "logs {subject} - Filters logs with a subject",
                "flushconfig - Saves the configuration file to disk",
                "add {workerType} - Adds a new worker",
                "remove {workerId} [workerId2] ... - Removes one or more workers",
                "stop {workerId} [workerId2] ... - Stops one or more workers",
                "start {workerId} [workerId2] ... - Starts one or more workers",
                "restart {workerId} [workerId2] ... - Restarts one or more workers",
                "stopall - Stops all workers",
                "startall - Starts all workers",
                "connect {workerId} - Connects to the worker terminal",
                "exit|quit - Exits from terminal and terminates the program"
            };

            ConsoleWriter.WritePaddedText(string.Join(Environment.NewLine, help), 5, ConsoleWriter.Theme.SuccessColor);
        }

        private void TerminalListWorkers()
        {
            foreach (var worker in Coordinator.Workers)
            {
                ConsoleWriter.WriteColoredText($"{worker.Id}", ConsoleWriter.Theme.SuccessColor);
                ConsoleWriter.WriteColoredText($" [{worker.Configuration.Alias}] ", ConsoleWriter.Theme.MessageColor);
                ConsoleWriter.WriteColoredTextLine($"{worker.Status}", ConsoleWriter.Theme.WarningColor);
            }
        }

        private void TerminalListWorkerTypes()
        {
            foreach (var worker in Coordinator.WorkerResolver.GetWorkerTypes())
            {
                ConsoleWriter.WriteColoredTextLine($"{WorkerConfiguration.GetWorkerName(worker)}",
                    ConsoleWriter.Theme.SuccessColor);
            }
        }

        private void TerminalLogs(string filterSubject)
        {
            var allSubjects = Logger.GetHistoryWorkers().Select(s => s ?? "~").ToArray();

            if (string.IsNullOrWhiteSpace(filterSubject))
            {
                if (!string.IsNullOrWhiteSpace(Logger.Filtered))
                {
                    Logger.FilterClear();
                }

                foreach (var subject in allSubjects)
                {
                    ConsoleWriter.WriteColoredTextLine(subject, ConsoleWriter.Theme.SuccessColor);
                }
            }
            else if (filterSubject == "~")
            {
                Logger.FilterBySystem();
            }
            else if (allSubjects.Any(s => s.Equals(filterSubject, StringComparison.InvariantCultureIgnoreCase)))
            {
                Logger.FilterByWorkerName(filterSubject);
            }
            else
            {
                ConsoleWriter.WriteColoredTextLine("Invalid logger subject provided.",
                    ConsoleWriter.Theme.WarningColor);
            }
        }

        private void TerminalRestartWorkers(string[] workerIds)
        {
            var workers = Coordinator.Workers.Where(worker =>
                    workerIds.Any(s => s.Equals(worker.Id, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            if (workers.Length == 0)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            foreach (var worker in workers)
            {
                ConsoleWriter.PrintWarning($"Restarting {worker.Id} ...");
                Coordinator.RestartWorker(worker.Id).Wait();
            }
        }

        private void TerminalStartAllWorkers()
        {
            ConsoleWriter.PrintWarning("Starting all workers ...");
            Coordinator.StartAll(false).Wait();
        }

        private void TerminalStartWorkers(string[] workerIds)
        {
            var workers = Coordinator.Workers.Where(worker =>
                    workerIds.Any(s => s.Equals(worker.Id, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            if (workers.Length == 0)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            foreach (var worker in workers)
            {
                ConsoleWriter.PrintWarning($"Starting {worker.Id} ...");
                Coordinator.StartWorker(worker.Id, false).Wait();
            }
        }

        private void TerminalStopAllWorkers()
        {
            ConsoleWriter.PrintWarning("Stopping all workers ...");
            Coordinator.StopAll().Wait();
        }

        private void TerminalStopWorkers(string[] workerIds)
        {
            var workers = Coordinator.Workers.Where(worker =>
                    workerIds.Any(s => s.Equals(worker.Id, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();

            if (workers.Length == 0)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            foreach (var worker in workers)
            {
                ConsoleWriter.PrintWarning($"Stopping {worker.Id} ...");
                Coordinator.StopWorker(worker.Id).Wait();
            }
        }

        private void TerminalToggleDebug()
        {
            Logger.Debug = !Logger.Debug;

            ConsoleWriter.PrintSuccess(Logger.Debug ? "Debug messages enabled." : "Debug messages disabled.");
        }
    }
}