using CarvedRock.Data.Entities;

namespace CarvedRock.Domain;

/// <summary>
/// Abstraction for sending the order-confirmation email. The concrete
/// implementation (MailKit/MailPit) lives in the API so the Domain layer
/// stays free of email infrastructure.
/// </summary>
public interface IOrderEmailSender
{
    Task SendOrderConfirmationAsync(Order order);
}
