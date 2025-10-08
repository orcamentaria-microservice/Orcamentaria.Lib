using MongoDB.Bson.Serialization.Attributes;
using Orcamentaria.Lib.Domain.Enums;
using System.Text.Json.Serialization;

namespace Orcamentaria.Lib.Domain.Models.Logs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$originType")]
    [JsonDerivedType(typeof(RequestExceptionOrigin), "request")]
    [JsonDerivedType(typeof(ServiceExceptionOrigin), "service")]

    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(RequestExceptionOrigin), typeof(ServiceExceptionOrigin))]
    public abstract class ExceptionOrigin 
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OriginEnum Type { get; set; }
    }
}
