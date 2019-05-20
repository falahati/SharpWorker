using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWorker.DataStore.Query;
using SharpWorker.WebApi;

namespace SharpWorker.DataStore
{
    public abstract class DataStoreBase
    {
        protected const string DatabaseExtension = ".db";
        protected const string RootDatabaseName = "Root";
        protected readonly SemaphoreSlim ArchiveLock = new SemaphoreSlim(1, 1);

        protected DataStoreBase(string dataDirectory)
        {
            DataDirectory = new DirectoryInfo(Path.GetFullPath(dataDirectory));

            if (!DataDirectory.Exists)
            {
                DataDirectory.Create();
            }
        }

        protected DirectoryInfo DataDirectory { get; }

        public bool IsBackingUp
        {
            get => ArchiveLock.CurrentCount == 0;
        }

        public virtual async Task Archive(CancellationToken cancellationToken, IDataArchive archive)
        {
            await ArchiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await Task.Factory.StartNew(() =>
                {
                    var exceptions = new List<Exception>();

                    var providers = new[] {RootDatabaseName}
                        .Concat(GetTopics().Select(topic => topic.Provider).Distinct())
                        .ToArray();

                    foreach (var provider in providers)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            ArchiveProvider(provider, archive, cancellationToken);
                        }
                        catch (AggregateException e)
                        {
                            exceptions.AddRange(e.InnerExceptions);
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                    }

                    if (exceptions.Any())
                    {
                        throw new AggregateException(exceptions);
                    }
                }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
            }
            finally
            {
                ArchiveLock.Release();
            }
        }

        public abstract T GetById<T>(DataTopic topic, string recordId) where T : DataRecord;

        // ReSharper disable once TooManyArguments
        public virtual IEnumerable<T> GetCreated<T>(
            DataTopic topic,
            DateTime? start,
            DateTime? end,
            DataStoreQueryCalculatedValue condition = null,
            int skip = 0,
            int limit = int.MaxValue) where T : DataRecord
        {
            DataStoreQueryCalculatedValue finalCondition = null;

            var startConditions = start != null
                ? new DataStoreCompareQuery(
                    new DataStoreQueryExpressionIndex(nameof(DataRecord.CreatedTimestamp)),
                    new DataStoreQueryConstant(start.Value.ToUniversalTime().Ticks),
                    DataStoreCompareQueryType.EqualOrGreater
                )
                : null;

            var endCondition = end != null
                ? new DataStoreCompareQuery(
                    new DataStoreQueryExpressionIndex(nameof(DataRecord.CreatedTimestamp)),
                    new DataStoreQueryConstant(end.Value.ToUniversalTime().Ticks),
                    DataStoreCompareQueryType.Less
                )
                : null;

            if (startConditions != null && endCondition != null)
            {
                finalCondition = new DataStoreOperatorQuery(
                    startConditions,
                    endCondition,
                    DataStoreOperatorQueryType.And
                );
            }
            else if (startConditions != null)
            {
                finalCondition = startConditions;
            }
            else if (endCondition != null)
            {
                finalCondition = endCondition;
            }

            if (condition != null && finalCondition != null)
            {
                finalCondition = new DataStoreOperatorQuery(
                    finalCondition,
                    condition,
                    DataStoreOperatorQueryType.And
                );
            }
            else if (condition != null)
            {
                finalCondition = condition;
            }

            return GetRecords<T>(topic, finalCondition, null, skip, limit);
        }


        public virtual long GetDatabaseSize()
        {
            try
            {
                return DataDirectory.GetFiles("*" + DatabaseExtension).DefaultIfEmpty().Sum(info => info.Length);
            }
            catch
            {
                // ignored
            }

            return 0;
        }

        public virtual DataTopic GetOrCreateTopic(
            Type provider,
            string subject)
        {
            return GetOrCreateTopic(provider, subject, new string[0]);
        }

        public virtual DataTopic GetOrCreateTopic(
            string provider,
            string subject)
        {
            return GetOrCreateTopic(provider, subject, new string[0]);
        }

        public abstract DataTopic GetOrCreateTopic(string provider, string subject, string[] parameters);

        public virtual DataTopic GetOrCreateTopic(Type provider, string subject, string[] parameters)
        {
            return GetOrCreateTopic(DataTopic.ProviderTypeToString(provider), subject, parameters);
        }

        // ReSharper disable once TooManyArguments
        public abstract IEnumerable<T> GetRecords<T>(
            DataTopic topic,
            DataStoreQueryCalculatedValue condition = null,
            DataStoreSortQuery sort = null,
            int skip = 0,
            int limit = int.MaxValue)
            where T : DataRecord;

        public abstract int DeleteRecords(
            DataTopic topic,
            DataStoreQueryCalculatedValue condition);


        public abstract bool DeleteRecord(
            DataTopic topic,
            string recordId);

        public abstract DataTopic GetTopic(string topicId);

        public abstract DataTopic GetTopic(string provider, string subject, string[] parameters);
        public abstract int ClearTopic(string provider, string subject, string[] parameters);
        public abstract int ClearTopic(string topicId);

        public virtual DataTopic GetTopic(Type provider, string subject, string[] parameters)
        {
            return GetTopic(DataTopic.ProviderTypeToString(provider), subject, parameters);
        }

        public abstract DataTopic[] GetTopics();

        public abstract DataTopic[] GetTopics(string provider);

        public virtual DataTopic[] GetTopics(Type provider)
        {
            return GetTopics(DataTopic.ProviderTypeToString(provider));
        }

        public virtual DataTopic[] GetTopics(Type provider, string subject)
        {
            return GetTopics(DataTopic.ProviderTypeToString(provider), subject);
        }

        public abstract DataTopic[] GetTopics(string provider, string subject);

        // ReSharper disable once TooManyArguments
        public virtual IEnumerable<T> GetUpdated<T>(
            DataTopic topic,
            DateTime? start,
            DateTime? end,
            DataStoreQueryCalculatedValue condition = null,
            int skip = 0,
            int limit = int.MaxValue)
            where T : DataRecord
        {
            DataStoreQueryCalculatedValue finalCondition = null;

            var startConditions = start != null
                ? new DataStoreCompareQuery(
                    new DataStoreQueryExpressionIndex(nameof(DataRecord.LastUpdateTimestamp)),
                    new DataStoreQueryConstant(start.Value.ToUniversalTime().Ticks),
                    DataStoreCompareQueryType.EqualOrGreater
                )
                : null;

            var endCondition = end != null
                ? new DataStoreCompareQuery(
                    new DataStoreQueryExpressionIndex(nameof(DataRecord.LastUpdateTimestamp)),
                    new DataStoreQueryConstant(end.Value.ToUniversalTime().Ticks),
                    DataStoreCompareQueryType.Less
                )
                : null;

            if (startConditions != null && endCondition != null)
            {
                finalCondition = new DataStoreOperatorQuery(
                    startConditions,
                    endCondition,
                    DataStoreOperatorQueryType.And
                );
            }
            else if (startConditions != null)
            {
                finalCondition = startConditions;
            }
            else if (endCondition != null)
            {
                finalCondition = endCondition;
            }

            if (condition != null && finalCondition != null)
            {
                finalCondition = new DataStoreOperatorQuery(
                    finalCondition,
                    condition,
                    DataStoreOperatorQueryType.And
                );
            }
            else if (condition != null)
            {
                finalCondition = condition;
            }

            return GetRecords<T>(topic, finalCondition, null, skip, limit);
        }

        public abstract void Upsert<T>(DataTopic topic, IEnumerable<T> records) where T : DataRecord;

        protected virtual void ArchiveProvider(
            string providerName,
            IDataArchive archive,
            CancellationToken cancellationToken)
        {
            var tempFilename = Path.GetTempFileName();

            try
            {
                var databaseFile = new FileInfo(Path.Combine(DataDirectory.FullName, providerName + DatabaseExtension));

                if (databaseFile.Exists && !archive.HasFile(databaseFile.Name))
                {
                    lock (this)
                    {
                        databaseFile.CopyTo(tempFilename, true);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    archive.AddFile(tempFilename, databaseFile.Name);
                }
            }
            finally
            {
                if (File.Exists(tempFilename))
                {
                    File.Delete(tempFilename);
                }
            }
        }
    }
}