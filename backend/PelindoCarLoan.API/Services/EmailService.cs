using System.Net;
using System.Net.Mail;

namespace PelindoCarLoan.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendLoanRequestSubmittedEmailAsync(string approverEmail, string requesterName, string requestNumber, string purpose);
        Task SendLoanRequestApprovedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber);
        Task SendLoanRequestRejectedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes);
        Task SendApprovalL1NotificationToL2Async(string approverL2Email, string requesterName, string requestNumber, string purpose);
        Task SendLoanRequestApprovedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber);
        Task SendLoanRequestRejectedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes);
        Task SendDriverAssignmentEmailAsync(string driverEmail, string driverName, string requestNumber, string startDatetime, string endDatetime, string destination);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"] ?? "Pelindo Car Loan System";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("Email configuration not set. Email not sent to {To}", to);
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? smtpUsername, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                // Don't throw - email failure shouldn't break the main flow
            }
        }

        public async Task SendLoanRequestSubmittedEmailAsync(string approverEmail, string requesterName, string requestNumber, string purpose)
        {
            var subject = $"[Pelindo Car Loan] Permohonan Baru #{requestNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Permohonan Peminjaman Kendaraan Baru</h2>
                    <p>Halo,</p>
                    <p>Ada permohonan peminjaman kendaraan baru yang memerlukan persetujuan Anda:</p>
                    <table style='border-collapse: collapse; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Nomor Permohonan:</td>
                            <td style='padding: 5px;'>{requestNumber}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Pemohon:</td>
                            <td style='padding: 5px;'>{requesterName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Keperluan:</td>
                            <td style='padding: 5px;'>{purpose}</td>
                        </tr>
                    </table>
                    <p>Silakan login ke sistem untuk meninjau dan memproses permohonan ini.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(approverEmail, subject, body);
        }

        public async Task SendLoanRequestApprovedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Disetujui Level 1";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Permohonan Disetujui Level 1</h2>
                    <p>Halo {requesterName},</p>
                    <p>Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah disetujui oleh Approver Level 1.</p>
                    <p>Permohonan Anda saat ini sedang menunggu persetujuan dari Approver Level 2.</p>
                    <p>Anda akan menerima pemberitahuan lebih lanjut setelah Approver Level 2 memproses permohonan Anda.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendLoanRequestRejectedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Ditolak Level 1";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Permohonan Ditolak Level 1</h2>
                    <p>Halo {requesterName},</p>
                    <p>Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah ditolak oleh Approver Level 1.</p>
                    {(string.IsNullOrEmpty(notes) ? "" : $"<p><strong>Catatan:</strong> {notes}</p>")}
                    <p>Silakan hubungi Approver Level 1 untuk informasi lebih lanjut.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendApprovalL1NotificationToL2Async(string approverL2Email, string requesterName, string requestNumber, string purpose)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Menunggu Persetujuan Level 2";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Permohonan Peminjaman Kendaraan - Level 2</h2>
                    <p>Halo,</p>
                    <p>Permohonan peminjaman kendaraan berikut telah disetujui Level 1 dan memerlukan persetujuan Anda:</p>
                    <table style='border-collapse: collapse; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Nomor Permohonan:</td>
                            <td style='padding: 5px;'>{requestNumber}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Pemohon:</td>
                            <td style='padding: 5px;'>{requesterName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Keperluan:</td>
                            <td style='padding: 5px;'>{purpose}</td>
                        </tr>
                    </table>
                    <p>Silakan login ke sistem untuk meninjau dan memproses permohonan ini.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(approverL2Email, subject, body);
        }

        public async Task SendLoanRequestApprovedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Disetujui";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #28a745;'>Permohonan Disetujui</h2>
                    <p>Halo {requesterName},</p>
                    <p>Selamat! Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah disetujui.</p>
                    <p>Sistem akan secara otomatis menjadwalkan kendaraan dan driver untuk Anda.</p>
                    <p>Silakan cek detail jadwal di sistem.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendLoanRequestRejectedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Ditolak Level 2";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Permohonan Ditolak Level 2</h2>
                    <p>Halo {requesterName},</p>
                    <p>Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah ditolak oleh Approver Level 2.</p>
                    {(string.IsNullOrEmpty(notes) ? "" : $"<p><strong>Catatan:</strong> {notes}</p>")}
                    <p>Silakan hubungi Approver Level 2 untuk informasi lebih lanjut.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendDriverAssignmentEmailAsync(string driverEmail, string driverName, string requestNumber, string startDatetime, string endDatetime, string destination)
        {
            var subject = $"[Pelindo Car Loan] Penugasan Driver #{requestNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Penugasan Driver Baru</h2>
                    <p>Halo {driverName},</p>
                    <p>Anda telah ditugaskan untuk permohonan peminjaman kendaraan dengan nomor <strong>{requestNumber}</strong>.</p>
                    <table style='border-collapse: collapse; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Waktu Mulai:</td>
                            <td style='padding: 5px;'>{startDatetime}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Waktu Selesai:</td>
                            <td style='padding: 5px;'>{endDatetime}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px; font-weight: bold;'>Tujuan:</td>
                            <td style='padding: 5px;'>{destination}</td>
                        </tr>
                    </table>
                    <p>Silakan login ke sistem untuk melihat detail lengkap penugasan Anda.</p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                    </p>
                </body>
                </html>
            ";

            await SendEmailAsync(driverEmail, subject, body);
        }
    }
}
