using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpWorker.DataStore;
using SharpWorker.Log;

namespace SharpWorker
{
    public class DefaultWorkerResolver : IWorkerResolver
    {
        /// <inheritdoc />
        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once TooManyArguments
        // ReSharper disable once MethodTooLong
        public virtual IWorker ActivateWorker(
            Coordinator coordinator,
            DataStoreBase dataStore,
            Logger logger,
            WorkerConfiguration configuration)
        {
            var workerTypes = GetWorkerTypes().ToDictionary(TypeExtension.GetSimplifiedName, type => type);

            foreach (var pair in workerTypes)
            {
                if (configuration.WorkerType != null && configuration.WorkerType != pair.Key)
                {
                    continue;
                }

                var availableParameters = new List<Tuple<Type, Type, object>>
                {
                    new Tuple<Type, Type, object>(coordinator?.GetType() ?? typeof(Coordinator),
                        typeof(Coordinator), coordinator),
                    new Tuple<Type, Type, object>(dataStore?.GetType() ?? typeof(DataStoreBase), typeof(DataStoreBase),
                        dataStore),
                    new Tuple<Type, Type, object>(logger?.GetType() ?? typeof(Logger), typeof(Logger), logger),
                    new Tuple<Type, Type, object>(configuration.GetType(), typeof(WorkerConfiguration), configuration)
                };

                if (typeof(ICustomizableWorker).IsAssignableFrom(pair.Value))
                {
                    availableParameters.Add(
                        new Tuple<Type, Type, object>(
                            configuration.Options?.GetType() ?? typeof(WorkerOptions),
                            typeof(WorkerOptions),
                            configuration.Options
                        )
                    );
                }
                else if (configuration.WorkerType == null)
                {
                    continue;
                }

                var constructorParameters = pair.Value.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Select(
                        c => c.GetParameters().Select(p =>
                            availableParameters.FirstOrDefault(availableParameter =>
                                availableParameter.Item3 != null &&
                                p.ParameterType.IsAssignableFrom(availableParameter.Item1) ||
                                availableParameter.Item3 == null &&
                                availableParameter.Item2.IsAssignableFrom(p.ParameterType)
                            )
                        ).ToArray()
                    )
                    .Where(parameters => parameters.All(t => t != null))
                    .OrderByDescending(parameters => parameters.Length)
                    .Select(parameters => parameters.Select(t => t.Item3).ToArray())
                    .FirstOrDefault();

                if (constructorParameters == null)
                {
                    continue;
                }

                return Activator.CreateInstance(pair.Value, constructorParameters) as IWorker;
            }


            return null;
        }

        /// <inheritdoc />
        public virtual Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        /// <inheritdoc />
        public virtual Type[] GetWorkerTypes()
        {
            return GetAssemblies().SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes().Where(type =>
                        type.IsClass && !type.IsAbstract && typeof(IWorker).IsAssignableFrom(type));
                }
                catch
                {
                    return new Type[0];
                }
            }).ToArray();
        }
    }
}