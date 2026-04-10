using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Notifications;

/// <summary>
/// Background service that runs hourly and sends appointment reminders
/// 24 hours and 1 hour before each scheduled appointment.
/// </summary>
public sealed class AppointmentReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppointmentReminderBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public AppointmentReminderBackgroundService(
        IServiceProvider services,
        ILogger<AppointmentReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing appointment reminders.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dispatch = scope.ServiceProvider.GetRequiredService<INotificationDispatchService>();

        var now = DateTime.UtcNow;

        // Windows for reminders: 24h and 1h before appointment
        var windows = new[]
        {
            (Label: "24h", From: now.AddHours(23), To: now.AddHours(25)),
            (Label: "1h",  From: now.AddMinutes(55), To: now.AddMinutes(65))
        };

        foreach (var (label, from, to) in windows)
        {
            var appointments = await db.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a =>
                    a.Status == AppointmentStatus.Scheduled &&
                    a.ScheduledDate >= from &&
                    a.ScheduledDate < to &&
                    !a.IsDeleted)
                .ToListAsync(ct);

            foreach (var apt in appointments)
            {
                try
                {
                    var payload = new Dictionary<string, object>
                    {
                        ["patient_name"]   = apt.Patient?.NombreCompleto ?? "Paciente",
                        ["doctor_name"]    = apt.Doctor?.FullName ?? "Médico",
                        ["appointment_date"] = apt.ScheduledDate.ToString("dd/MM/yyyy"),
                        ["appointment_time"] = apt.ScheduledDate.ToString("HH:mm"),
                        ["reminder_window"] = label
                    };

                    var request = new DispatchRequest(
                        TenantId: apt.TenantId,
                        EventType: NotificationEventType.AppointmentReminder,
                        Payload: payload,
                        RecipientEmail: apt.Patient?.Correo,
                        RecipientPhone: apt.Patient?.Telefono,
                        RelatedEntityType: "Appointment",
                        RelatedEntityId: apt.Id.ToString());

                    var result = await dispatch.DispatchAsync(request, ct);

                    if (result.Success)
                        _logger.LogInformation(
                            "Reminder ({Label}) sent for appointment {AptId} — patient {Patient}",
                            label, apt.Id, apt.Patient?.NombreCompleto);
                    else
                        _logger.LogWarning(
                            "Reminder ({Label}) for appointment {AptId} failed: {Errors}",
                            label, apt.Id, string.Join(", ", result.Errors));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unhandled error sending {Label} reminder for appointment {AptId}", label, apt.Id);
                }
            }
        }
    }
}
