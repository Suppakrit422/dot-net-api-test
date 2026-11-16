using System.Text.Json.Serialization;

namespace WebAPITemplate.Application.Common
{
    // Kind of failure a service returns in SaveResult; the controller maps it to an HTTP status.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ErrorType
    {
        None,        // success
        Validation,  // 400
        NotFound,    // 404
        Forbidden,   // 403
        Conflict     // 409
    }
}