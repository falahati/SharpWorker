using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Force.Crc32;
using SharpWorker.DataStore.Attributes;

namespace SharpWorker.DataStore
{
    public class DataTopic : IEquatable<DataTopic>
    {
        public static HashAlgorithm IdHashAlgorithm = new Crc32Algorithm();
        private string _id;

        public DataTopic()
        {
        }

        public DataTopic(Type provider) : this(provider, null, new string[0])
        {
        }

        public DataTopic(Type provider, string subject) : this(provider, subject, new string[0])
        {
        }

        public DataTopic(Type provider, string subject, string[] parameters) : this(ProviderTypeToString(provider),
            subject, parameters)
        {
        }

        public DataTopic(string provider) : this(provider, null, new string[0])
        {
        }

        public DataTopic(string provider, string subject) : this(provider, subject, new string[0])
        {
        }

        public DataTopic(string provider, string subject, string[] parameters)
        {
            Provider = provider;
            Subject = subject;
            Parameters = parameters ?? new string[0];
        }

        public int Count { get; protected internal set; }


        [DataField(true)]
        public virtual DateTime? FirstRecord
        {
            get => FirstRecordTimestamp == null || FirstRecordTimestamp <= 0
                ? (DateTime?) null
                : new DateTime(FirstRecordTimestamp.Value, DateTimeKind.Utc).ToUniversalTime();
            set => FirstRecordTimestamp = value?.ToUniversalTime().Ticks;
        }

        public long? FirstRecordTimestamp { get; protected internal set; }

        public string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    RecalculateId();
                }

                return _id;
            }
            protected set => _id = value;
        }

        [DataField(true)]
        public virtual DateTime? LastRecord
        {
            get => LastRecordTimestamp == null || LastRecordTimestamp <= 0
                ? (DateTime?) null
                : new DateTime(LastRecordTimestamp.Value, DateTimeKind.Utc).ToUniversalTime();
            set => LastRecordTimestamp = value?.ToUniversalTime().Ticks;
        }

        public long? LastRecordTimestamp { get; protected internal set; }
        public string[] Parameters { get; protected set; } = new string[0];
        public string Provider { get; protected set; }
        public string Subject { get; protected set; }

        /// <inheritdoc />
        public bool Equals(DataTopic other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Parameters.SequenceEqual(other.Parameters) &&
                   string.Equals(Provider, other.Provider) &&
                   string.Equals(Subject, other.Subject);
        }

        public static bool operator ==(DataTopic left, DataTopic right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(DataTopic left, DataTopic right)
        {
            return !(left == right);
        }

        public static string ProviderTypeToString(Type type)
        {
            if (type.AssemblyQualifiedName != null)
            {
                return string.Join(",", type.AssemblyQualifiedName.Split(',').Take(2));
            }

            return type.FullName;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DataTopic);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = Provider != null ? Provider.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Subject != null ? Subject.GetHashCode() : 0);

                foreach (var parameter in Parameters)
                {
                    hashCode = (hashCode * 397) ^ (parameter != null ? parameter.GetHashCode() : 0);
                }
                // ReSharper restore NonReadonlyMemberInGetHashCode

                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0} [{1}]", Subject, string.Join("|", Parameters));
        }

        public bool DoesFit(Type provider, string subject)
        {
            return DoesFit(ProviderTypeToString(provider), subject);
        }

        public bool DoesFit(Type provider)
        {
            return DoesFit(ProviderTypeToString(provider));
        }

        public bool DoesFit(string providerName, string subject)
        {
            return Provider == providerName &&
                   Subject == subject;
        }

        public bool DoesFit(string providerName)
        {
            return Provider == providerName;
        }

        public Type GetProviderType()
        {
            return Type.GetType(Provider);
        }

        public bool IsEqual(Type provider, string subject)
        {
            return IsEqual(ProviderTypeToString(provider), subject);
        }

        public bool IsEqual(Type provider, string subject, string[] parameters)
        {
            return IsEqual(ProviderTypeToString(provider), subject, parameters);
        }

        public bool IsEqual(string providerName, string subject)
        {
            return IsEqual(providerName, subject, new string[0]);
        }

        public bool IsEqual(string providerName, string subject, string[] parameters)
        {
            return Provider == providerName &&
                   Subject == subject &&
                   parameters?.SequenceEqual(Parameters) == true;
        }

        public virtual void RecalculateId()
        {
            Id = BitConverter
                .ToString(IdHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(string.Join("_",
                    new[] {Provider ?? "", Subject ?? ""}.Concat(Parameters).ToArray())))).Replace("-", "")
                .ToLowerInvariant();
        }
    }
}