using System;

namespace SharpWorker.DataStore.LiteDB
{
    public class LiteDBDataTopic : DataTopic, IEquatable<LiteDBDataTopic>
    {
        public LiteDBDataTopic()
        {
        }

        public LiteDBDataTopic(Type provider) :
            base(provider)
        {
        }

        public LiteDBDataTopic(Type provider, string subject) :
            base(provider, subject)
        {
        }

        public LiteDBDataTopic(Type provider, string subject, string[] parameters) :
            base(provider, subject, parameters)
        {
        }

        public LiteDBDataTopic(string provider) :
            base(provider)
        {
        }

        public LiteDBDataTopic(string provider, string subject) :
            base(provider, subject)
        {
        }

        public LiteDBDataTopic(string provider, string subject, string[] parameters) :
            base(provider, subject, parameters)
        {
        }

        public int CollectionIndex { get; set; } = 0;

        /// <inheritdoc />
        public bool Equals(LiteDBDataTopic other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && CollectionIndex == other.CollectionIndex;
        }

        public static bool operator ==(LiteDBDataTopic left, LiteDBDataTopic right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(LiteDBDataTopic left, LiteDBDataTopic right)
        {
            return !(left == right);
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

            return Equals(obj as LiteDBDataTopic);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return (base.GetHashCode() * 397) ^ CollectionIndex;
            }
        }
    }
}