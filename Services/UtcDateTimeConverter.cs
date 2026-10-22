using System.Text.Json;
using System.Text.Json.Serialization;

namespace AttendanceApi.Services;

// BR-08: الأوقات بتتخزن UTC. SQL Server بيرجّع DateTime من غير Kind فالـ JSON كان هيطلع من غير Z
// والواجهة ممكن تفهمه محلي غلط. الـ converter ده بيضمن إن كل وقت يطلع بصيغة UTC (فيها Z)
// والواجهة (متصفح/موبايل) هي اللي تعرضه بالتوقيت المحلي للمستخدم
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset().UtcDateTime;

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc);
    }
}
