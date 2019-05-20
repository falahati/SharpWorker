using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ConsoleUtilities;
using Newtonsoft.Json;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Description;

namespace SharpWorker.NetCoordinator
{
    internal class WorkerTerminal
    {
        public WorkerTerminal(Coordinator coordinator, string workerId, ConsoleWriter consoleWriter, LocalLogger logger)
        {
            if (string.IsNullOrWhiteSpace(workerId))
            {
                throw new ArgumentException("Bad command arguments.", nameof(workerId));
            }

            WorkerId = workerId;
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            ConsoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            Logger = logger;
        }

        public ConsoleWriter ConsoleWriter { get; }
        public LocalLogger Logger { get; }
        public Coordinator Coordinator { get; }
        public string WorkerId { get; }

        // ReSharper disable once TooManyDeclarations
        // ReSharper disable once MethodNameNotMeaningful
        public void Run()
        {
            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            if (!string.IsNullOrWhiteSpace(worker.Worker.Name))
            {
                Logger.FilterByWorkerName(worker.Worker.Name);
            }

            new ConsoleTerminal(worker.GetType().Name, new Dictionary<string, Action<string[]>>
            {
                {
                    "", commandArgs => TerminalWorkerHelp()
                },
                {
                    "stop", commandArgs => TerminalStopWorker()
                },
                {
                    "start", commandArgs => TerminalStartWorker()
                },
                {
                    "restart", commandArgs => TerminalRestartWorker()
                },
                {
                    "stat", TerminalWorkerStat
                },
                {
                    "webapi", TerminalWorkerWebApiCommand
                }
            }).RunTerminal();

            Logger.FilterClear();
        }

        // ReSharper disable once ExcessiveIndentation
        private dynamic ParseInput(string input, Type type)
        {
            try
            {
                if (type.IsGenericType && Nullable.GetUnderlyingType(type) != null)
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                if (type == typeof(bool))
                {
                    if (input.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                    {
                        input = "true";
                    }
                    else if (input.Equals("no", StringComparison.CurrentCultureIgnoreCase))
                    {
                        input = "false";
                    }

                    if (bool.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(uint))
                {
                    if (uint.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(int))
                {
                    if (int.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(short))
                {
                    if (short.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(ushort))
                {
                    if (ushort.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(long))
                {
                    if (long.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(ulong))
                {
                    if (ulong.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(byte))
                {
                    if (byte.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(decimal))
                {
                    if (decimal.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(double))
                {
                    if (double.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(char))
                {
                    if (char.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(float))
                {
                    if (float.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(sbyte))
                {
                    if (sbyte.TryParse(input, out var result))
                    {
                        return result;
                    }
                }
                else if (type == typeof(string))
                {
                    return input;
                }
                else if (type?.IsClass == true)
                {
                    return JsonConvert.DeserializeObject(input.Trim().TrimEnd(';'), type);
                }
                else
                {
                    return input;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private void TerminalRestartWorker()
        {
            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            ConsoleWriter.PrintWarning($"Restarting {worker.Id} ...");
            Coordinator.RestartWorker(worker.Id).Wait();
        }

        private void TerminalStartWorker()
        {
            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            ConsoleWriter.PrintWarning($"Starting {worker.Id} ...");
            Coordinator.StartWorker(worker.Id, false).Wait();
        }

        private void TerminalStopWorker()
        {
            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            ConsoleWriter.PrintWarning($"Stopping {worker.Id} ...");
            Coordinator.StopWorker(worker.Id).Wait();
        }

        private void TerminalWorkerHelp()
        {
            var help = new[]
            {
                "stop - Stops the worker",
                "start - Starts the worker",
                "restart - Restarts the worker",
                "stat [Worker's Property] [Property's Property] ... - Allows browsing of worker's settings and properties",
                "stat {Worker's Property} [Property's Property] ... = {PropertyValue} - Allows Changing worker's properties",
                "webapi [ControllerIndex] [ControllerAction] ... - Allows browsing of worker's web api controllers",
                "webapi {ControllerIndex} {ControllerAction}() [ActionArgument1] [ActionArgument2] ... - Allows execution of web api actions",
                "exit|quit - Exits from worker terminal"
            };

            ConsoleWriter.WritePaddedText(string.Join(Environment.NewLine, help), 5, ConsoleWriter.Theme.SuccessColor);
        }


        private void TerminalWorkerStat(string[] path)
        {
            if (string.IsNullOrWhiteSpace(WorkerId))
            {
                ConsoleWriter.PrintWarning("Bad command arguments.");

                return;
            }

            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            path = path ?? new string[0];

            var obj = (object) worker;
            string setValue = null;

            if (Array.IndexOf(path, "=") >= 0)
            {
                setValue = string.Join(" ", path.Skip(Array.IndexOf(path, "=") + 1).ToArray());
                path = path.Take(Array.IndexOf(path, "=")).ToArray();
            }

            if (setValue != null && worker.Status != CoordinatedWorkerStatus.Stopped)
            {
                ConsoleWriter.PrintWarning("Can not set property value when worker is running.");

                return;
            }

            for (var i = 0; i < path.Length; i++)
            {
                // Set if this is the last item in path and there is a value to be set
                if (setValue != null && i == path.Length - 1)
                {
                    var value = ParseInput(setValue, obj?.GetType().GetProperty(path[i])?.PropertyType);
                    obj?.GetType().GetProperty(path[i])?.GetSetMethod(false).Invoke(obj, new object[] {value});
                }

                if (obj?.GetType().IsArray == true && int.TryParse(path[i], out var index))
                {
                    obj = (obj as IEnumerable)?.Cast<object>().ToArray()[index];
                }
                else
                {
                    obj = obj?.GetType().GetProperty(path[i])?.GetGetMethod().Invoke(obj, new object[0]);
                }
            }

            ConsoleWriter.WriteObject(obj, 0);
        }

        private void TerminalWorkerWebApiActionDetails(
            WebApiControllerDescription controllerDescription,
            WebApiControllerActionDescription action)
        {
            ConsoleWriter.PrintCaption(controllerDescription.Name);


            foreach (var parameter in action.Parameters)
            {
                ConsoleWriter.WriteColoredText($"{parameter.Name} ", ConsoleWriter.Theme.SuccessColor);
                ConsoleWriter.WriteColoredText("(", ConsoleWriter.Theme.MessageColor);
                ConsoleWriter.WriteColoredText($"{parameter.Type.Name}", ConsoleWriter.Theme.TypeColor);
                ConsoleWriter.WriteColoredText(")", ConsoleWriter.Theme.MessageColor);

                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    ConsoleWriter.WriteColoredText(": ", ConsoleWriter.Theme.MessageColor);
                    ConsoleWriter.WriteColoredText(parameter.Description, ConsoleWriter.Theme.MessageColor);
                }

                ConsoleWriter.WriteColoredTextLine("", ConsoleWriter.Theme.MessageColor);
            }

            var returnType =
                action.ResponseAttributes.FirstOrDefault(attribute => attribute.ResponseCode == HttpStatusCode.OK)
                    ?.ResponseType ??
                typeof(object);

            ConsoleWriter.WriteColoredText("(", ConsoleWriter.Theme.MessageColor);
            ConsoleWriter.WriteColoredText(returnType.Name, ConsoleWriter.Theme.TypeColor);
            ConsoleWriter.WriteColoredText(") ", ConsoleWriter.Theme.MessageColor);

            ConsoleWriter.WriteColoredTextLine(
                !string.IsNullOrWhiteSpace(controllerDescription.Description) ? controllerDescription.Description : "",
                ConsoleWriter.Theme.MessageColor
            );
        }

        // ReSharper disable once TooManyArguments
        private void TerminalWorkerWebApiActionInvoke(
            string[] commandArgs,
            WebApiController controller,
            WebApiControllerActionDescription action)
        {
            var parameterValues = new List<object>();

            for (var i = 0; i < action.Parameters.Length; i++)
            {
                var parameter = action.Parameters[i];
                var value = commandArgs.Skip(2).ToArray()[i];
                parameterValues.Add(ParseInput(value, parameter.Type));
            }

            var result = action.MethodInfo.Invoke(controller, parameterValues.ToArray());

            if (result is WebApiResponse response)
            {
                result = response.Content;
            }

            ConsoleWriter.WriteObject(result);
        }

        // ReSharper disable once ExcessiveIndentation
        private void TerminalWorkerWebApiCommand(string[] commandArgs)
        {
            if (string.IsNullOrWhiteSpace(WorkerId))
            {
                ConsoleWriter.PrintWarning("Bad command arguments.");

                return;
            }

            var worker = Coordinator.Workers.FirstOrDefault(w =>
                w.Id.Equals(WorkerId, StringComparison.InvariantCultureIgnoreCase));

            if (worker == null)
            {
                ConsoleWriter.PrintWarning("Bad worker id provided.");

                return;
            }

            var webApiControllers = (worker.Worker as IWebApiWorker)?.GetWebApiControllers() ?? new WebApiController[0];

            if (worker.DoesProvideWebApi == false || webApiControllers.Length == 0)
            {
                ConsoleWriter.PrintWarning("Worker does not supports web api calls.");

                return;
            }

            if (commandArgs.Length == 0)
            {
                TerminalWorkerWebApiControllers(webApiControllers);
            }
            else if (commandArgs.Length >= 1 &&
                     int.TryParse(commandArgs.First(), out var index) &&
                     index >= 0 &&
                     index < webApiControllers.Length &&
                     webApiControllers[index] != null)
            {
                var controller = webApiControllers[index];
                var controllerDescription = new WebApiControllerDescription(controller.GetType(), "");

                // Controller Action
                if (commandArgs.Length > 1)
                {
                    var action =
                        controllerDescription.Actions.FirstOrDefault(a => a.Name == commandArgs[1].TrimEnd('(', ')'));

                    if (action != null)
                    {
                        // Controller Action Invoke
                        if (commandArgs.Length == action.Parameters.Length + 2 && commandArgs[1].EndsWith("()"))
                        {
                            TerminalWorkerWebApiActionInvoke(commandArgs, controller, action);
                        }
                        else if (commandArgs.Length == 2) // Controller Action Details
                        {
                            TerminalWorkerWebApiActionDetails(controllerDescription, action);
                        }
                    }
                }

                // Controller Details
                if (commandArgs.Length == 1)
                {
                    TerminalWorkerWebApiControllerDetails(controllerDescription);
                }
            }
        }

        private void TerminalWorkerWebApiControllerDetails(WebApiControllerDescription controllerDescription)
        {
            ConsoleWriter.PrintCaption(controllerDescription.Name);

            if (!string.IsNullOrWhiteSpace(controllerDescription.Description))
            {
                ConsoleWriter.PrintMessage(controllerDescription.Description);
                ConsoleWriter.PrintSeparator();
            }

            foreach (var action in controllerDescription.Actions)
            {
                var returnType =
                    action.ResponseAttributes.FirstOrDefault(attribute => attribute.ResponseCode == HttpStatusCode.OK)
                        ?.ResponseType ??
                    typeof(object);

                ConsoleWriter.WriteColoredText("(", ConsoleWriter.Theme.MessageColor);
                ConsoleWriter.WriteColoredText(returnType.Name, ConsoleWriter.Theme.TypeColor);
                ConsoleWriter.WriteColoredText(") ", ConsoleWriter.Theme.MessageColor);

                ConsoleWriter.WriteColoredText($"{action.Name}", ConsoleWriter.Theme.SuccessColor);
                ConsoleWriter.WriteColoredText("(", ConsoleWriter.Theme.MessageColor);

                for (var i = 0; i < action.Parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        ConsoleWriter.WriteColoredText(", ", ConsoleWriter.Theme.MessageColor);
                    }

                    ConsoleWriter.WriteColoredText(action.Parameters[i].Type.Name, ConsoleWriter.Theme.TypeColor);
                }

                ConsoleWriter.WriteColoredTextLine(")", ConsoleWriter.Theme.MessageColor);
            }
        }

        private void TerminalWorkerWebApiControllers(WebApiController[] webApiControllers)
        {
            for (var i = 0; i < webApiControllers.Length; i++)
            {
                if (webApiControllers[0] == null)
                {
                    continue;
                }

                ConsoleWriter.WriteColoredText($"[{i}] ", ConsoleWriter.Theme.SuccessColor);
                ConsoleWriter.WriteColoredTextLine($"{webApiControllers[0].GetType().Name}",
                    ConsoleWriter.Theme.MessageColor);
            }
        }
    }
}