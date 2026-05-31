using System.Net.Mail;
using System.Text;
using CarvedRock.Data.Entities;
using CarvedRock.Domain;
using MailKit.Client;
using MimeKit;

namespace CarvedRock.Api;

public class EmailService(MailKitClientFactory factory, ILogger<EmailService> logger)
    : IOrderEmailSender
{
    public async Task SendOrderConfirmationAsync(Order order)
    {
        string basePath = AppContext.BaseDirectory;
        string templatePath = Path.Combine(basePath, "emailTemplate.html");
        string template = await File.ReadAllTextAsync(templatePath);

        template = template.Replace("{{NarrativeContent}}", "<h1>Thank you for your order!</h1>");

        var productRows = new StringBuilder();
        foreach (var detail in order.Details)
        {
            productRows.AppendLine(
                $"<tr><td>{detail.ProductName}</td><td>{detail.Quantity}</td><td>{detail.LineTotal}</td></tr>");
        }
        template = template.Replace("{{ProductRows}}", productRows.ToString());
        template = template.Replace("{{AdditionalNotes}}", "Enjoy your new gear!");

        var client = await factory.GetSmtpClientAsync();

        using var message = new MailMessage
        {
            Body = template,
            Subject = "Your CarvedRock Order",
            IsBodyHtml = true,
            From = new MailAddress("e-commerce@carvedrock.com", "Carved Rock Shop"),
            To = { order.Email }
        };

        await client.SendAsync(MimeMessage.CreateFromMailMessage(message));
        logger.LogInformation("Order confirmation email sent to {Email} for order {OrderId}.",
            order.Email, order.Id);
    }
}
