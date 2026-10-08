using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace eBayHero.Infrastructure.Data;

/// <summary>
/// Persists <see cref="DateTimeOffset"/> as UTC ticks (a 64-bit integer).
///
/// Why: the SQLite provider stores DateTimeOffset as TEXT by default, and it cannot
/// translate an <c>ORDER BY</c> over that representation to SQL, which breaks paged and
/// sorted inventory queries. Storing normalized UTC ticks gives a compact, index-friendly
/// column that sorts chronologically and keeps NULL ordering (undated rows sort last in a
/// descending sort) intact.
/// </summary>
public sealed class UtcTicksConverter : ValueConverter<DateTimeOffset, long>
{
    public UtcTicksConverter()
        : base(
            value => value.UtcDateTime.Ticks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
    {
    }
}
