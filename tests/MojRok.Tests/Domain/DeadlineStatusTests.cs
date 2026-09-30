using Xunit;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;

namespace MojRok.Tests.Domain;

public class DeadlineStatusTests
{
    private static Deadline MakeDeadline(DateTime dueDate, bool isCompleted = false) => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Test Deadline",
        DueDate     = dueDate,
        IsCompleted = isCompleted,
        UserId      = Guid.NewGuid(),
        CategoryId  = Guid.NewGuid()
    };

    [Fact]
    public void Status_IsCompleted_WhenIsCompletedTrue()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(-1), isCompleted: true);
        Assert.Equal(DeadlineStatus.Completed, d.Status);
    }

    [Fact]
    public void Status_IsExpired_WhenDueDateYesterday()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(-1));
        Assert.Equal(DeadlineStatus.Expired, d.Status);
    }

    [Fact]
    public void Status_IsUrgent_WhenDueDateToday()
    {
        var d = MakeDeadline(DateTime.UtcNow);
        Assert.Equal(DeadlineStatus.Urgent, d.Status);
    }

    [Fact]
    public void Status_IsUrgent_WhenDueDateIn3Days()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(3));
        Assert.Equal(DeadlineStatus.Urgent, d.Status);
    }

    [Fact]
    public void Status_IsUpcoming_WhenDueDateIn4Days()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(4));
        Assert.Equal(DeadlineStatus.Upcoming, d.Status);
    }

    [Fact]
    public void Status_IsUpcoming_WhenDueDateIn14Days()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(14));
        Assert.Equal(DeadlineStatus.Upcoming, d.Status);
    }

    [Fact]
    public void Status_IsActive_WhenDueDateIn15Days()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(15));
        Assert.Equal(DeadlineStatus.Active, d.Status);
    }

    [Fact]
    public void Status_IsActive_WhenDueDateFarFuture()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(90));
        Assert.Equal(DeadlineStatus.Active, d.Status);
    }

    [Fact]
    public void CompletedDeadline_IsNotExpired_EvenIfPastDue()
    {
        var d = MakeDeadline(DateTime.UtcNow.AddDays(-30), isCompleted: true);
        Assert.NotEqual(DeadlineStatus.Expired, d.Status);
    }
}
