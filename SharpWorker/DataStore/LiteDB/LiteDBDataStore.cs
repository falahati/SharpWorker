using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using LiteDB;
using SharpWorker.DataStore.Attributes;
using SharpWorker.DataStore.Query;

namespace SharpWorker.DataStore.LiteDB
{
    public class LiteDBDataStore : DataStoreBase
    {
        protected const int CollectionPerDatabase = 100;

        protected readonly Dictionary<string, Dictionary<int, LiteDatabase>> Databases =
            new Dictionary<string, Dictionary<int, LiteDatabase>>();

        protected readonly LiteCollection<LiteDBDataTopic> TopicCollection;
        protected readonly Dictionary<string, List<LiteDBDataTopic>> TopicsByProvider;

        public LiteDBDataStore(string dataDirectory) : base(dataDirectory)
        {
            RootDatabase = new LiteDatabase(new ConnectionString
            {
                UtcDate = true,
                Filename = Path.Combine(DataDirectory.FullName, RootDatabaseName + DatabaseExtension),
                CacheSize = 1000
            });

            RootDatabase.Mapper.ResolveMember += ResolveMember;

            TopicCollection = RootDatabase.GetCollection<LiteDBDataTopic>();
            TopicCollection.EnsureIndex(nameof(LiteDBDataTopic.Provider));
            TopicCollection.EnsureIndex(nameof(LiteDBDataTopic.Subject));
            TopicCollection.EnsureIndex(nameof(LiteDBDataTopic.CollectionIndex));
            TopicCollection.EnsureIndex(nameof(LiteDBDataTopic.Parameters));
            TopicsByProvider = TopicCollection.FindAll().GroupBy(topic => topic.Provider)
                .ToDictionary(topics => topics.Key, topics => topics.ToList());
        }


        protected LiteDatabase RootDatabase
        {
            get
            {
                lock (Databases)
                {
                    return Databases[RootDatabaseName][0];
                }
            }
            private set
            {
                lock (Databases)
                {
                    if (!Databases.ContainsKey(RootDatabaseName))
                    {
                        Databases.Add(RootDatabaseName, new Dictionary<int, LiteDatabase> {{0, value}});
                    }
                    else
                    {
                        Databases[RootDatabaseName][0] = value;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override int ClearTopic(string provider, string subject, string[] parameters)
        {
            var topic = GetTopic(provider, subject, parameters);

            return ClearTopic(topic as LiteDBDataTopic);
        }

        /// <inheritdoc />
        public override int ClearTopic(string topicId)
        {
            var topic = GetTopic(topicId);

            return ClearTopic(topic as LiteDBDataTopic);
        }

        /// <inheritdoc />
        public override bool DeleteRecord(DataTopic topic, string recordId)
        {
            return DeleteRecords(topic,
                       (DataStoreQueryField) nameof(DataRecord.Id) == (DataStoreQueryConstant) recordId) >
                   0;
        }

        /// <inheritdoc />
        public override int DeleteRecords(DataTopic topic, DataStoreQueryCalculatedValue condition)
        {
            var collection = GetCollection(topic);

            var conditionQuery = condition?.ToQuery();

            if (conditionQuery == null)
            {
                return 0;
            }

            lock (collection)
            {
                return collection.Delete(conditionQuery);
            }
        }

        public override T GetById<T>(DataTopic topic, string recordId)
        {
            var collection = GetCollection<T>(topic);

            lock (collection)
            {
                return collection.FindById(recordId);
            }
        }

        public override DataTopic GetOrCreateTopic(
            string provider,
            string subject,
            string[] parameters)
        {
            lock (TopicsByProvider)
            {
                LiteDBDataTopic topic = null;
                parameters = parameters ?? new string[0];

                if (TopicsByProvider.ContainsKey(provider))
                {
                    topic = TopicsByProvider[provider]
                        .FirstOrDefault(t =>
                            t.Provider == provider &&
                            t.Subject == subject &&
                            t.Parameters.SequenceEqual(parameters));
                }

                if (topic == null)
                {
                    topic = new LiteDBDataTopic(provider, subject, parameters);
                    var sameProviderCollections = TopicsByProvider.ContainsKey(provider)
                        ? TopicsByProvider[provider].Count(t => t.Provider == topic.Provider)
                        : 0;

                    topic.CollectionIndex = (int) Math.Floor(sameProviderCollections / (double) CollectionPerDatabase);

                    lock (RootDatabase)
                    {
                        lock (TopicCollection)
                        {
                            TopicCollection.Upsert(topic);
                        }
                    }

                    if (!TopicsByProvider.ContainsKey(topic.Provider))
                    {
                        TopicsByProvider.Add(topic.Provider, new List<LiteDBDataTopic>());
                    }

                    TopicsByProvider[topic.Provider].Add(topic);
                }

                return topic;
            }
        }

        // ReSharper disable once TooManyArguments
        public override IEnumerable<T> GetRecords<T>(
            DataTopic topic,
            DataStoreQueryCalculatedValue condition = null,
            DataStoreSortQuery sort = null,
            int skip = 0,
            int limit = int.MaxValue)
        {
            var collection = GetCollection<T>(topic);

            var conditionQuery = condition?.ToQuery();

            lock (collection)
            {
                if (conditionQuery != null)
                {
                    if (sort != null)
                    {
                        return collection.Find(conditionQuery)?.SortBy(sort)?.Skip(skip).Take(limit) ?? new T[0];
                    }

                    return collection.Find(conditionQuery, skip, limit);
                }

                if (sort != null)
                {
                    return collection.Find(sort.ToQuery(), skip, limit);
                }

                return collection.Find(global::LiteDB.Query.All(), skip, limit);
            }
        }

        public override DataTopic GetTopic(string provider, string subject, string[] parameters)
        {
            lock (TopicsByProvider)
            {
                parameters = parameters ?? new string[0];

                return TopicsByProvider.ContainsKey(provider)
                    ? TopicsByProvider[provider].FirstOrDefault(topic =>
                        topic.IsEqual(provider, subject, parameters))
                    : null;
            }
        }

        public override DataTopic GetTopic(string topicId)
        {
            lock (TopicsByProvider)
            {
                return TopicsByProvider.SelectMany(pair => pair.Value).FirstOrDefault(topic => topic.Id == topicId);
            }
        }

        public override DataTopic[] GetTopics()
        {
            lock (TopicsByProvider)
            {
                return TopicsByProvider.SelectMany(pair => pair.Value).Cast<DataTopic>().ToArray();
            }
        }

        public override DataTopic[] GetTopics(string provider, string subject)
        {
            return GetTopics(provider).Where(topic => topic.Subject == subject).ToArray();
        }

        public override DataTopic[] GetTopics(string provider)
        {
            lock (TopicsByProvider)
            {
                return TopicsByProvider.ContainsKey(provider)
                    ? TopicsByProvider[provider].Where(topic => topic.Provider == provider)
                        .Cast<DataTopic>()
                        .ToArray()
                    : new DataTopic[0];
            }
        }

        public override void Upsert<T>(DataTopic topic, IEnumerable<T> records)
        {
            var collection = GetCollection<T>(topic as LiteDBDataTopic);

            lock (collection)
            {
                collection.UpsertBulk(records.Select(record =>
                {
                    record.OnSave(this, topic);

                    return record;
                }));
            }

            UpdateTopic(topic as LiteDBDataTopic, collection);
        }

        // ReSharper disable once ExcessiveIndentation
        protected override void ArchiveProvider(
            string providerName,
            IDataArchive archive,
            CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();

            var maxProviderCollectionIndex = GetTopics().Where(topic => topic.Provider == providerName)
                .DefaultIfEmpty().Max(topic => (topic as LiteDBDataTopic)?.CollectionIndex ?? 0);

            for (var index = 0; index <= maxProviderCollectionIndex; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var tempFilename = Path.GetTempFileName();

                try
                {
                    var databaseFile = new FileInfo(Path.Combine(DataDirectory.FullName,
                        providerName + (index > 0 ? "." + index : "") + DatabaseExtension));

                    if (databaseFile.Exists && !archive.HasFile(databaseFile.Name))
                    {
                        lock (Databases)
                        {
                            if (Databases.ContainsKey(providerName))
                            {
                                lock (Databases[providerName])
                                {
                                    if (Databases[providerName].ContainsKey(index))
                                    {
                                        lock (Databases[providerName][index])
                                        {
                                            databaseFile.CopyTo(tempFilename, true);
                                        }
                                    }
                                    else
                                    {
                                        databaseFile.CopyTo(tempFilename, true);
                                    }
                                }
                            }
                            else
                            {
                                databaseFile.CopyTo(tempFilename, true);
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        archive.AddFile(tempFilename, databaseFile.Name);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    if (File.Exists(tempFilename))
                    {
                        File.Delete(tempFilename);
                    }
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions.ToArray());
            }
        }

        protected virtual int ClearTopic(LiteDBDataTopic topic)
        {
            var collection = GetCollection(topic);

            int deleted;

            lock (collection)
            {
                deleted = collection.Delete(global::LiteDB.Query.All());
            }

            UpdateTopic(topic, collection);

            return deleted;
        }

        protected LiteCollection<BsonDocument> GetCollection(DataTopic topic)
        {
            return GetCollection<BsonDocument>(topic);
        }

        protected virtual void ResolveMember(Type type, MemberInfo memberInfo, MemberMapper member)
        {
            var fieldAttribute = DataFieldAttribute.GetAttribute(memberInfo);

            if (fieldAttribute == null)
            {
                return;
            }

            if (fieldAttribute.Ignored)
            {
                member.FieldName = null;

                return;
            }

            if (!string.IsNullOrWhiteSpace(fieldAttribute.FieldName))
            {
                member.FieldName = fieldAttribute.FieldName;
            }
        }

        protected virtual void UpdateTopic<T>(LiteDBDataTopic topic, LiteCollection<T> collection)
        {
            if (topic == null || collection == null)
            {
                return;
            }

            lock (collection)
            {
                topic.Count = collection.Count();

                if (topic.Count > 0)
                {
                    topic.LastRecordTimestamp = collection.Max(nameof(DataRecord.CreatedTimestamp));
                    topic.FirstRecordTimestamp = collection.Min(nameof(DataRecord.CreatedTimestamp));
                }
                else
                {
                    topic.LastRecordTimestamp = null;
                    topic.FirstRecordTimestamp = null;
                }

                lock (RootDatabase)
                {
                    lock (TopicCollection)
                    {
                        TopicCollection.Upsert(topic);
                    }
                }
            }
        }

        private LiteCollection<T> GetCollection<T>(DataTopic topic)
        {
            var dataTopic = topic as LiteDBDataTopic;

            if (dataTopic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            LiteDatabase db;

            lock (Databases)
            {
                if (!Databases.ContainsKey(dataTopic.Provider))
                {
                    Databases.Add(dataTopic.Provider, new Dictionary<int, LiteDatabase>());
                }

                lock (Databases[dataTopic.Provider])
                {
                    if (!Databases[dataTopic.Provider].ContainsKey(dataTopic.CollectionIndex))
                    {
                        var databaseFile = new FileInfo(Path.Combine(DataDirectory.FullName,
                            dataTopic.Provider +
                            (dataTopic.CollectionIndex > 0 ? "." + dataTopic.CollectionIndex : "") +
                            DatabaseExtension));

                        db = new LiteDatabase(new ConnectionString
                        {
                            UtcDate = true,
                            Filename = databaseFile.FullName,
                            CacheSize = 1000
                        });

                        Databases[dataTopic.Provider].Add(dataTopic.CollectionIndex, db);

                        lock (TopicsByProvider)
                        {
                            if (!TopicsByProvider.ContainsKey(dataTopic.Provider))
                            {
                                TopicsByProvider.Add(dataTopic.Provider, new List<LiteDBDataTopic>());
                            }

                            TopicsByProvider[dataTopic.Provider].Add(dataTopic);
                        }
                    }
                    else
                    {
                        db = Databases[dataTopic.Provider][dataTopic.CollectionIndex];
                    }
                }
            }

            lock (db)
            {
                if (!db.CollectionExists(dataTopic.Id))
                {
                    var collection = db.GetCollection<T>(dataTopic.Id);

                    foreach (var index in DataRecordFieldIndexAttribute.GetAttributes(typeof(T)))
                    {
                        collection.EnsureIndex(index.FieldName, index.IsUnique);
                    }

                    foreach (var index in DataRecordExpressionIndexAttribute.GetAttributes(typeof(T)))
                    {
                        collection.EnsureIndex(LiteDBQueryExtensions.GetIndexName(index.IndexName), index.Expression,
                            index.IsUnique);
                    }

                    return collection;
                }

                return db.GetCollection<T>(dataTopic.Id);
            }
        }
    }
}