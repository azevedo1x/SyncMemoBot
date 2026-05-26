using FluentAssertions;
using Microsoft.Recognizers.Text;
using SyncMemoBot.Core.Time;
using SyncMemoBot.Infrastructure.Time;
using Xunit;

namespace SyncMemoBot.Tests.Time;

public class MultilingualTimeParserTests
{
    static MultilingualTimeParserTests() => MultilingualTimeParser.WarmUp();

    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private static MultilingualTimeParser Build(DateTimeOffset utcNow)
        => new(new FakeClock(utcNow), new FixedTimeZone(SaoPaulo));

    [Fact]
    public void Parses_relative_english_input()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("in 2 hours", "en-US");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 22, 15, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.English);
    }

    [Fact]
    public void Parses_relative_portuguese_input()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("em 2 horas", "pt-BR");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 22, 15, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.Portuguese);
    }

    [Fact]
    public void Spanish_locale_now_uses_english_as_primary()
    {
        // Spanish support was removed; an es-* locale is treated as English.
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("in 2 hours", "es-ES");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 22, 15, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.English);
    }

    [Fact]
    public void Falls_back_to_portuguese_when_input_doesnt_match_user_locale()
    {
        // now = 2026-05-22 13:00 UTC
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        // "em 2 horas" is Portuguese ("em" preposition); EN parser doesn't recognize it.
        // Falls back to PT → +2 hours → 2026-05-22 15:00 UTC. Same input/output as the
        // pt-BR test above, but with en-US locale to force the fallback chain.
        var result = parser.Parse("em 2 horas", "en-US");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 22, 15, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.Portuguese);
    }

    [Fact]
    public void Parses_compound_datetime_portuguese()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("amanhã às 15:00", "pt-BR");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 23, 18, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.Portuguese);
    }

    [Fact]
    public void Parses_compound_datetime_english()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("tomorrow at 3pm", "en-US");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.ResolvedUtc.Should().Be(new DateTimeOffset(2026, 5, 23, 18, 0, 0, TimeSpan.Zero));
        success.UsedCulture.Should().Be(Culture.English);
    }

    [Fact]
    public void Maps_unsupported_locale_to_english_as_primary()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("in 3 hours", "fr-FR");

        var success = result.Should().BeOfType<TimeParseResult.Success>().Subject;
        success.UsedCulture.Should().Be(Culture.English);
    }

    [Fact]
    public void Returns_NoDateTimeFound_for_garbage_input()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("xyzqwerty random nonsense", "en-US");

        result.Should().BeOfType<TimeParseResult.Failure>()
            .Which.Reason.Should().Be(TimeParseFailureReason.NoDateTimeFound);
    }

    [Fact]
    public void Returns_NoDateTimeFound_for_empty_input()
    {
        var nowUtc = new DateTimeOffset(2026, 5, 22, 13, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("", "en-US");

        result.Should().BeOfType<TimeParseResult.Failure>()
            .Which.Reason.Should().Be(TimeParseFailureReason.NoDateTimeFound);
    }

    [Fact]
    public void Returns_ResolvedInPast_when_time_only_already_passed_today()
    {
        // 2026-05-22 23:00 UTC = 2026-05-22 20:00 São Paulo (UTC-3)
        var nowUtc = new DateTimeOffset(2026, 5, 22, 23, 0, 0, TimeSpan.Zero);
        var parser = Build(nowUtc);

        var result = parser.Parse("5pm", "en-US");

        result.Should().BeOfType<TimeParseResult.Failure>()
            .Which.Reason.Should().Be(TimeParseFailureReason.ResolvedInPast);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class FixedTimeZone(TimeZoneInfo zone) : IReminderTimeZone
    {
        public TimeZoneInfo Zone { get; } = zone;
    }
}
