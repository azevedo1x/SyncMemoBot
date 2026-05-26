using System.Globalization;
using Microsoft.Recognizers.Text;
using SyncMemoBot.Core.Localization;
using SyncMemoBot.Core.Time;
using MsRecognizers = Microsoft.Recognizers.Text.DateTime;

namespace SyncMemoBot.Infrastructure.Time;

public sealed class MultilingualTimeParser(IClock clock, IReminderTimeZone timeZone) : ITimeParsingService
{
    /// <summary>
    /// Fallback chains preallocated to avoid an allocation per Parse call. The user's
    /// primary language is tried first; the other covers cross-locale input (e.g. a PT
    /// phrase typed by a user whose Discord locale is en-US).
    /// </summary>
    private static readonly SupportedLanguage[] EnglishFirst = [SupportedLanguage.English, SupportedLanguage.Portuguese];
    private static readonly SupportedLanguage[] PortugueseFirst = [SupportedLanguage.Portuguese, SupportedLanguage.English];

    private static readonly TimeSpan DefaultTimeForDateOnly = new(9, 0, 0);
    private static readonly TimeSpan PastTolerance = TimeSpan.FromSeconds(30);

    private readonly IClock _clock = clock;
    private readonly IReminderTimeZone _timeZone = timeZone;

    public TimeParseResult Parse(string input, string? userLocale)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new TimeParseResult.Failure(TimeParseFailureReason.NoDateTimeFound);

        var nowInZone = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow.UtcDateTime, _timeZone.Zone);
        var anyParsedButPast = false;

        foreach (var language in OrderedChain(LanguageDetection.FromLocale(userLocale)))
        {
            var culture = ToRecognizersCulture(language);
            var candidates = ExtractCandidates(input, culture, nowInZone);
            if (candidates.Count == 0) continue;

            var futures = candidates.Where(c => c >= nowInZone - PastTolerance).ToList();
            if (futures.Count > 0)
            {
                var earliest = futures.Min();
                var utcInstant = TimeZoneInfo.ConvertTimeToUtc(earliest, _timeZone.Zone);
                return new TimeParseResult.Success(
                    new DateTimeOffset(utcInstant, TimeSpan.Zero),
                    culture);
            }

            anyParsedButPast = true;
        }

        return anyParsedButPast
            ? new TimeParseResult.Failure(TimeParseFailureReason.ResolvedInPast)
            : new TimeParseResult.Failure(TimeParseFailureReason.NoDateTimeFound);
    }

    public static void WarmUp()
    {
        var refTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        _ = MsRecognizers.DateTimeRecognizer.RecognizeDateTime("tomorrow 8pm", Culture.English, refTime: refTime);
        _ = MsRecognizers.DateTimeRecognizer.RecognizeDateTime("amanhã às 15:00", Culture.Portuguese, refTime: refTime);
    }

    private static SupportedLanguage[] OrderedChain(SupportedLanguage primary) =>
        primary == SupportedLanguage.Portuguese ? PortugueseFirst : EnglishFirst;

    private static string ToRecognizersCulture(SupportedLanguage language) =>
        language == SupportedLanguage.Portuguese ? Culture.Portuguese : Culture.English;

    private static List<DateTime> ExtractCandidates(string input, string culture, DateTime referenceInZone)
    {
        var results = MsRecognizers.DateTimeRecognizer.RecognizeDateTime(input, culture, refTime: referenceInZone);
        var candidates = new List<DateTime>(capacity: results.Count);

        foreach (var r in results)
        {
            if (r.Resolution is null) continue;
            if (!r.Resolution.TryGetValue("values", out var valuesObj)) continue;
            if (valuesObj is not List<Dictionary<string, string>> values) continue;

            foreach (var v in values)
            {
                var dt = TryExtractCandidate(v, referenceInZone);
                if (dt is not null) candidates.Add(dt.Value);
            }
        }

        return candidates;
    }

    private static DateTime? TryExtractCandidate(IReadOnlyDictionary<string, string> value, DateTime referenceInZone)
    {
        if (!value.TryGetValue("type", out var type)) return null;

        switch (type)
        {
            case "datetime":
                if (value.TryGetValue("value", out var dtStr)
                    && DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
                break;

            case "date":
                if (value.TryGetValue("value", out var dateStr)
                    && DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return DateTime.SpecifyKind(d.Date + DefaultTimeForDateOnly, DateTimeKind.Unspecified);
                break;

            case "time":
                if (value.TryGetValue("value", out var timeStr)
                    && TimeSpan.TryParse(timeStr, CultureInfo.InvariantCulture, out var t))
                    return DateTime.SpecifyKind(referenceInZone.Date + t, DateTimeKind.Unspecified);
                break;

            case "datetimerange":
            case "daterange":
                if (value.TryGetValue("start", out var startStr)
                    && DateTime.TryParse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                    return DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
                break;
        }

        return null;
    }
}
