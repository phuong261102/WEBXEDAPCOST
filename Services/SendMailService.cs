using System;
using System.Threading.Tasks;
using App.Models;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

public class MailSettings
{
    public string Mail { get; set; }
    public string DisplayName { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }

}

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendSmsAsync(string number, string message);
    Task SendOrderConfirmationEmailAsync(Order order);
}

public class SendMailService : IEmailSender
{


    private readonly MailSettings mailSettings;

    private readonly ILogger<SendMailService> logger;


    // mailSetting được Inject qua dịch vụ hệ thống
    // Có inject Logger để xuất log
    public SendMailService(IOptions<MailSettings> _mailSettings, ILogger<SendMailService> _logger)
    {
        mailSettings = _mailSettings.Value;
        logger = _logger;
        logger.LogInformation("Create SendMailService");
    }


    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.Sender = new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail);
        message.From.Add(new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;


        var builder = new BodyBuilder();
        builder.HtmlBody = htmlMessage;
        message.Body = builder.ToMessageBody();

        // dùng SmtpClient của MailKit
        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        try
        {
            smtp.Connect(mailSettings.Host, mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(mailSettings.Mail, mailSettings.Password);
            await smtp.SendAsync(message);
        }

        catch (Exception ex)
        {
            // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
            System.IO.Directory.CreateDirectory("mailssave");
            var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
            await message.WriteToAsync(emailsavefile);

            logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
            logger.LogError(ex.Message);
        }

        smtp.Disconnect(true);

        logger.LogInformation("send mail to " + email);


    }

    public Task SendSmsAsync(string number, string message)
    {
        // Cài đặt dịch vụ gửi SMS tại đây
        System.IO.Directory.CreateDirectory("smssave");
        var emailsavefile = string.Format(@"smssave/{0}-{1}.txt", number, Guid.NewGuid());
        System.IO.File.WriteAllTextAsync(emailsavefile, message);
        return Task.FromResult(0);
    }

    public async Task SendOrderConfirmationEmailAsync(Order order)
    {
        var subject = "Order Confirmation";
        var message = BuildOrderConfirmationMessage(order);
        await SendEmailAsync(order.UserEmail, subject, message);
    }

    private string BuildOrderConfirmationMessage(Order order)
    {
        // Convert TotalAmount to decimal if it's not already a numeric type
        if (!decimal.TryParse(order.TotalAmount, out var totalAmount))
        {
            totalAmount = 0; // Fallback to 0 if conversion fails
        }

        var message = $@"
        <div style='font-family: Arial, sans-serif; color: #333;'>
            <h1 style='color: #4CAF50;'>Order Confirmation</h1>
            <p>Dear {order.UserName},</p>
            <p>Thank you for your order. Here are the details:</p>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Order ID:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{order.Id}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Order Date:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{order.OrderDate}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Shipping Address:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{order.ShippingAddress}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Shipping Method:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{order.ShippingMethod}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Payment Method:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{order.PaymentMethod}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>Total Amount:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{totalAmount.ToString("N0")}VND</td>
                </tr>
            </table>
            <h2 style='color: #4CAF50;'>Order Details</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <thead>
                    <tr>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Product</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Description</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Quantity</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Unit Price</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Total Price</th>
                    </tr>
                </thead>
                <tbody>";

        foreach (var detail in order.OrderDetails)
        {
            message += $@"
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{detail.ProductName}</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{detail.ProductDescription}</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{detail.Quantity}</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{detail.UnitPrice.ToString("N0")}VND</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{(detail.UnitPrice * detail.Quantity).ToString("N0")}VND</td>
                    </tr>";
        }

        message += @"
                </tbody>
            </table>
            <p>We hope you enjoy your purchase!</p>
            <p>Sincerely, <br> <strong>KingCycle</strong></p>
        </div>";

        return message;
    }



}