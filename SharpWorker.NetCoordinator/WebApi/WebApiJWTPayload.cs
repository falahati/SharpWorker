using System;
using System.Text;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Newtonsoft.Json;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal class WebApiJWTPayload
    {
        private static JwtDecoder _jwtDecoder;
        private static readonly object LockObject = new object();

        [JsonProperty("scopes", Required = Required.Always)]
        public string[] ClaimedScopes { get; }

        [JsonConstructor]
        public WebApiJWTPayload(string[] claimedScopes)
        {
            ClaimedScopes = claimedScopes;
        }

        private static JwtDecoder JWTDecoder
        {
            get
            {
                lock (LockObject)
                {
                    if (_jwtDecoder == null)
                    {
                        var serializer = new JsonNetSerializer();
                        _jwtDecoder = new JwtDecoder(
                            serializer,
                            new JwtValidator(serializer, new UtcDateTimeProvider()),
                            new JwtBase64UrlEncoder(),
                            new HMACSHAAlgorithmFactory()
                        );
                    }
                }

                return _jwtDecoder;
            }
        }

        public static string JWTSecret { get; set; }

        public static WebApiJWTPayload GetPayload(string token)
        {
            if (string.IsNullOrWhiteSpace(JWTSecret) || string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var json = JWTDecoder.Decode(token, JWTSecret, true);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<WebApiJWTPayload>(json);
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return null;
        }

        public static bool Validate(string token)
        {
            if (string.IsNullOrWhiteSpace(JWTSecret) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                JWTDecoder.Validate(new JwtParts(token), Encoding.UTF8.GetBytes(JWTSecret));

                return true;
            }
            catch (Exception)
            {
                // ignore
            }

            return false;
        }
    }
}