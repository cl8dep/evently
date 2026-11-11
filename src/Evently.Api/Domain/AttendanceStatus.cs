using System.Text.Json.Serialization;

namespace Evently.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceStatus { Going, Maybe, Declined }
