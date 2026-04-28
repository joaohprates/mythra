using Mythra.Domain.Common;

namespace Mythra.Domain.Progress;

public sealed class ReadingProgress : Entity
{
    public Guid ProfileId { get; set; }
    public Guid MediaItemId { get; set; }
    public Guid? CurrentChapterId { get; set; }
    public int? CurrentPage { get; set; }
    public int? TotalPages { get; set; }
    public string? CfiLocator { get; set; }
    public double PercentComplete { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset LastReadAt { get; set; } = DateTimeOffset.UtcNow;

    public void UpdateProgress(double percent, int? currentPage = null, string? cfi = null)
    {
        PercentComplete = Math.Clamp(percent, 0, 100);
        if (currentPage.HasValue) CurrentPage = currentPage;
        if (cfi is not null) CfiLocator = cfi;
        LastReadAt = DateTimeOffset.UtcNow;
        if (PercentComplete >= 99.0) IsCompleted = true;
        Touch();
    }
}
