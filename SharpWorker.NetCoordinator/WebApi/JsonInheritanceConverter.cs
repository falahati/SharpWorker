using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.NetCoordinator.WebApi
{
    public class JsonInheritanceConverter : JsonConverter
    {
        internal const string DiscriminatorName = "discriminator";
        [ThreadStatic] private static bool _isReading;

        [ThreadStatic] private static bool _isWriting;

        /// <inheritdoc />
        public override bool CanRead
        {
            get
            {
                if (_isReading)
                {
                    _isReading = false;

                    return false;
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override bool CanWrite
        {
            get
            {
                if (_isWriting)
                {
                    _isWriting = false;

                    return false;
                }

                return true;
            }
        }


        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return WebApiTypeDiscriminatorAttribute.HasAttribute(objectType, true);
        }

        /// <inheritdoc />
        // ReSharper disable once TooManyArguments
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JObject>(reader);

            if (jObject == null)
            {
                return null;
            }

            var discriminator = jObject.GetValue(DiscriminatorName).Value<string>();
            var subtype = GetDiscriminatorType(jObject, objectType, discriminator);

            try
            {
                _isReading = true;

                return serializer.Deserialize(jObject.CreateReader(), subtype);
            }
            finally
            {
                _isReading = false;
            }
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                _isWriting = true;

                var jObject = JObject.FromObject(value, serializer);
                jObject[DiscriminatorName] = JToken.FromObject(GetDiscriminatorValue(value.GetType()));
                writer.WriteToken(jObject.CreateReader());
            }
            finally
            {
                _isWriting = false;
            }
        }

        /// <summary>Gets the discriminator value for the given type.</summary>
        /// <param name="type">The object type.</param>
        /// <returns>The discriminator value.</returns>
        public virtual string GetDiscriminatorValue(Type type)
        {
            return type.Name;
        }

        /// <summary>Gets the type for the given discriminator value.</summary>
        /// <param name="jObject">The JSON object.</param>
        /// <param name="objectType">The object (base) type.</param>
        /// <param name="discriminatorValue">The discriminator value.</param>
        /// <returns></returns>
        protected virtual Type GetDiscriminatorType(JObject jObject, Type objectType, string discriminatorValue)
        {
            if (objectType.Name == discriminatorValue)
            {
                return objectType;
            }

            var subtype = WebApiTypeDiscriminatorAttribute.GetSubtypes(objectType)
                ?.FirstOrDefault(type => GetDiscriminatorValue(type) == discriminatorValue);

            if (subtype != null)
            {
                return subtype;
            }

            var typeInfo = jObject.GetValue("$type");

            if (typeInfo != null)
            {
                return Type.GetType(typeInfo.Value<string>());
            }

            throw new InvalidOperationException("Could not find subtype of '" +
                                                objectType.Name +
                                                "' with discriminator '" +
                                                discriminatorValue +
                                                "'.");
        }
    }
}