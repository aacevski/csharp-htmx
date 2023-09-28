namespace chat.models;

using System.Text;
using Newtonsoft.Json;

public class HtmxWsRequest
{
    public string Message { get; set; }

    // Constructor
    public HtmxWsRequest(string message)
    {
        Message = message;
    }

    // Convert the current instance to a JSON string
    public string? ToJson() => JsonConvert.SerializeObject(this);

    // Convert a JSON string to a HtmxWsRequest instance
    public static HtmxWsRequest? FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        return JsonConvert.DeserializeObject<HtmxWsRequest>(json);
    }

    // Convert a buffer to a HtmxWsRequest instance
    public static HtmxWsRequest? FromBuffer(byte[] buffer, int count)
    {
        var json = Encoding.UTF8.GetString(buffer, 0, count);
        return FromJson(json);
    }

    // Convert the current instance to a buffer
    public byte[] ToBuffer()
    {
        var json = ToJson();
        return Encoding.UTF8.GetBytes(json ?? string.Empty);
    }

    // Overridden ToString method
    public override string ToString() => ToJson() ?? string.Empty;

    // Implicit conversion from HtmxWsRequest to string
    public static implicit operator string(HtmxWsRequest? message) => message?.ToJson() ?? string.Empty;

    // Implicit conversion from string to HtmxWsRequest
    public static implicit operator HtmxWsRequest?(string? json) => FromJson(json);
}
