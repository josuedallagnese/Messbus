using System.Text.Json.Serialization;
using Bogus;

namespace MessageBus.PubSub.Tests.Events;

public class FrameworkEvent
{
    public string StringProperty { get; set; }
    public int IntProperty { get; set; }
    public int? NullableIntProperty { get; set; }
    public long LongProperty { get; set; }
    public long? NullableLongProperty { get; set; }
    public float FloatProperty { get; set; }
    public float? NullableFloatProperty { get; set; }
    public double DoubleProperty { get; set; }
    public double? NullableDoubleProperty { get; set; }
    public decimal DecimalProperty { get; set; }
    public decimal? NullableDecimalProperty { get; set; }
    public DateOnly DateOnlyProperty { get; set; }
    public DateOnly? NullableDateOnlyProperty { get; set; }
    public DateTime DateTimeProperty { get; set; }
    public DateTime? NullableDateTimeProperty { get; set; }
    public TimeOnly TimeOnlyProperty { get; set; }
    public TimeOnly? NullableTimeOnlyProperty { get; set; }
    public short ShortProperty { get; set; }
    public short? NullableShortProperty { get; set; }
    public ushort UShortProperty { get; set; }
    public ushort? NullableUShortProperty { get; set; }
    public uint UIntProperty { get; set; }
    public uint? NullableUIntProperty { get; set; }
    public ulong ULongProperty { get; set; }
    public ulong? NullableULongProperty { get; set; }
    public byte ByteProperty { get; set; }
    public byte? NullableByteProperty { get; set; }
    public sbyte SByteProperty { get; set; }
    public sbyte? NullableSByteProperty { get; set; }
    public bool BoolProperty { get; set; }
    public bool? NulalbleBoolProperty { get; set; }
    public char CharProperty { get; set; }
    public char? NullableCharProperty { get; set; }
    public Guid GuidProperty { get; set; }
    public Guid? NullableGuidProperty { get; set; }
    public TimeSpan TimeSpanProperty { get; set; }
    public TimeSpan? NullableTimeSpanProperty { get; set; }
    public Uri UriProperty { get; set; }

    [JsonPropertyName("camelCase")]
    public string camelCase { get; set; }

    [JsonPropertyName("PascalCase")]
    public string PascalCase { get; set; }

    public static FrameworkEvent Create()
    {
        var faker = new Faker<FrameworkEvent>()
            .RuleFor(e => e.StringProperty, f => f.Lorem.Sentence())
            .RuleFor(e => e.IntProperty, f => f.Random.Int(1, 100))
            .RuleFor(e => e.NullableIntProperty, f => f.Random.Bool() ? f.Random.Int(1, 100) : null)
            .RuleFor(e => e.LongProperty, f => f.Random.Long(1000, 9999))
            .RuleFor(e => e.NullableLongProperty, f => f.Random.Bool() ? f.Random.Long(1000, 9999) : null)
            .RuleFor(e => e.FloatProperty, f => f.Random.Float(1.5f, 100.5f))
            .RuleFor(e => e.NullableFloatProperty, f => f.Random.Bool() ? f.Random.Float(1.5f, 100.5f) : null)
            .RuleFor(e => e.DoubleProperty, f => f.Random.Double(1.5, 10000.5))
            .RuleFor(e => e.NullableDoubleProperty, f => f.Random.Bool() ? f.Random.Double(1.5, 10000.5) : null)
            .RuleFor(e => e.DecimalProperty, f => f.Random.Decimal(1.5m, 1000.5m))
            .RuleFor(e => e.NullableDecimalProperty, f => f.Random.Bool() ? f.Random.Decimal(1.5m, 1000.5m) : null)
            .RuleFor(e => e.DateOnlyProperty, f => DateOnly.FromDateTime(f.Date.Past(10)))
            .RuleFor(e => e.NullableDateOnlyProperty, f => f.Random.Bool() ? DateOnly.FromDateTime(f.Date.Past(10)) : null)
            .RuleFor(e => e.DateTimeProperty, f => f.Date.Past(5).ToUniversalTime())
            .RuleFor(e => e.NullableDateTimeProperty, f => f.Random.Bool() ? f.Date.Past(5).ToUniversalTime() : null)
            .RuleFor(e => e.TimeOnlyProperty, f => TimeOnly.FromDateTime(f.Date.Recent()))
            .RuleFor(e => e.NullableTimeOnlyProperty, f => f.Random.Bool() ? TimeOnly.FromDateTime(f.Date.Recent()) : null)
            .RuleFor(e => e.ShortProperty, f => f.Random.Short(1, 30000))
            .RuleFor(e => e.NullableShortProperty, f => f.Random.Bool() ? f.Random.Short(1, 30000) : null)
            .RuleFor(e => e.UShortProperty, f => f.Random.UShort(1, 60000))
            .RuleFor(e => e.NullableUShortProperty, f => f.Random.Bool() ? f.Random.UShort(1, 60000) : null)
            .RuleFor(e => e.UIntProperty, f => f.Random.UInt(1, 4000000000))
            .RuleFor(e => e.NullableUIntProperty, f => f.Random.Bool() ? f.Random.UInt(1, 4000000000) : null)
            .RuleFor(e => e.ULongProperty, f => (ulong)f.Random.Long(1, long.MaxValue))
            .RuleFor(e => e.NullableULongProperty, f => f.Random.Bool() ? (ulong)f.Random.Long(1, long.MaxValue) : null)
            .RuleFor(e => e.ByteProperty, f => f.Random.Byte(1, 255))
            .RuleFor(e => e.NullableByteProperty, f => f.Random.Bool() ? f.Random.Byte(1, 255) : null)
            .RuleFor(e => e.SByteProperty, f => f.Random.SByte(-128, 127))
            .RuleFor(e => e.NullableSByteProperty, f => f.Random.Bool() ? f.Random.SByte(-128, 127) : null)
            .RuleFor(e => e.BoolProperty, f => f.Random.Bool())
            .RuleFor(e => e.NulalbleBoolProperty, f => f.Random.Bool() ? f.Random.Bool() : null)
            .RuleFor(e => e.CharProperty, f => f.Random.Char('A', 'Z'))
            .RuleFor(e => e.NullableCharProperty, f => f.Random.Bool() ? f.Random.Char('A', 'Z') : null)
            .RuleFor(e => e.GuidProperty, f => f.Random.Guid())
            .RuleFor(e => e.NullableGuidProperty, f => f.Random.Bool() ? f.Random.Guid() : null)
            .RuleFor(e => e.TimeSpanProperty, f => f.Date.Timespan(TimeSpan.FromHours(24)))
            .RuleFor(e => e.NullableTimeSpanProperty, f => f.Random.Bool() ? f.Date.Timespan(TimeSpan.FromHours(24)) : null)
            .RuleFor(e => e.camelCase, f => "camelCase")
            .RuleFor(e => e.PascalCase, f => "PascalCase")
            .RuleFor(e => e.UriProperty, f => new Uri("http://dev.org"));

        return faker.Generate();
    }
}
