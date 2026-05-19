namespace OpenBusinessPlatform.Api.Application.Common;

public sealed record PagedRequestDto(
    int SkipCount = 0,
    int MaxResultCount = 20,
    string? Sorting = null);

public sealed record PagedResultDto<T>(long TotalCount, IReadOnlyList<T> Items)
{
    public PagedResultDto(long totalCount, IEnumerable<T> items)
        : this(totalCount, items.ToArray())
    {
    }
}
