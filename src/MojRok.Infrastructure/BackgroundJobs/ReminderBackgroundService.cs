using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MojRok.Application.Reminders;

namespace MojRok.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls once per minute and processes due InApp reminders.
/// Email reminders are intentionally left unsent until an email provider is added.
/// </summary>
public sealed class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MojRok ReminderBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<ReminderService>();
                var processed = await reminderService.ProcessDueInAppRemindersAsync(stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation(
                        "ReminderBackgroundService processed {Count} due InApp reminder(s).",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A transient failure must not terminate the host.
                _logger.LogError(ex, "Error while processing reminders.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("MojRok ReminderBackgroundService stopped.");
    }
}