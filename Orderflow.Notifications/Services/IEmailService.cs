namespace Orderflow.Notifications.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string? firstName, CancellationToken cancellationToken = default);
}
