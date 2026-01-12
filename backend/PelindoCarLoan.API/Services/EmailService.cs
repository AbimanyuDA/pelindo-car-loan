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

        private string GetEmailTemplate(string title, string content, string accentColor = "#0066CC")
        {
            // Use hosted logo URL
            var logoUrl = "http://localhost:5000/images/logo-pelindo.png";
            
            return $@"
<!DOCTYPE html>
<html lang='id'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background-color: #f5f7fa;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f7fa; padding: 20px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                    <!-- Header with Gradient -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #0066CC 0%, #004999 100%); padding: 40px 30px; text-align: center;'>
                            <table width='100%'>
                                <tr>
                                    <td align='center'>
                                        <div style='background-color: white; border-radius: 12px; padding: 15px; display: inline-block; margin-bottom: 20px;'>
                                            <img src='{logoUrl}' alt='PT Pelindo' style='height: 50px; width: auto; display: block;' />
                                        </div>
                                        <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; letter-spacing: -0.5px;'>
                                            {title}
                                        </h1>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            {content}
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                            <p style='margin: 0 0 10px 0; color: #6c757d; font-size: 14px; line-height: 1.6;'>
                                <strong>PT Pelindo - Sistem Peminjaman Kendaraan</strong>
                            </p>
                            <p style='margin: 0 0 15px 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                                Email ini dikirim secara otomatis oleh sistem.<br/>
                                Mohon tidak membalas email ini.
                            </p>
                            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                                <p style='margin: 0; color: #adb5bd; font-size: 11px;'>
                                    © {DateTime.Now.Year} PT Pelindo. All rights reserved.
                                </p>
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
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
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo,
    </p>
    <p style='font-size: 15px; margin: 0 0 25px 0; line-height: 1.6; color: #495057;'>
        Ada permohonan peminjaman kendaraan baru yang memerlukan persetujuan Anda:
    </p>
    
    <!-- Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td style='padding: 8px 0; width: 180px; color: #6c757d; font-size: 14px; font-weight: 600;'>
                            Nomor Permohonan:
                        </td>
                        <td style='padding: 8px 0; color: #212529; font-size: 14px; font-weight: 600;'>
                            {requestNumber}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px;'>
                            Pemohon:
                        </td>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {requesterName}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px;'>
                            Keperluan:
                        </td>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {purpose}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- CTA Button -->
    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
        <tr>
            <td align='center'>
                <a href='http://localhost:3000' style='display: inline-block; background: linear-gradient(135deg, #0066CC 0%, #004999 100%); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(0, 102, 204, 0.2);'>
                    Buka Sistem
                </a>
            </td>
        </tr>
    </table>
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Silakan login ke sistem untuk meninjau dan memproses permohonan ini.
    </p>
</div>";

            var body = GetEmailTemplate("Permohonan Peminjaman Kendaraan Baru", content);
            await SendEmailAsync(approverEmail, subject, body);
        }

        public async Task SendLoanRequestApprovedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Disetujui Level 1";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo <strong>{requesterName}</strong>,
    </p>
    
    <!-- Success Badge -->
    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;'>
        <div style='font-size: 48px; margin-bottom: 10px;'>✓</div>
        <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 20px;'>Disetujui Level 1</h2>
        <p style='color: #ffffff; margin: 0; font-size: 14px; opacity: 0.9;'>Permohonan #{requestNumber}</p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah disetujui oleh Approver Level 1.
    </p>
    
    <!-- Info Alert -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0; color: #856404; font-size: 14px; line-height: 1.6;'>
                    <strong>Status Selanjutnya:</strong><br/>
                    Permohonan Anda saat ini sedang menunggu persetujuan dari Approver Level 2.
                </p>
            </td>
        </tr>
    </table>
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Anda akan menerima pemberitahuan lebih lanjut setelah Approver Level 2 memproses permohonan Anda.
    </p>
</div>";

            var body = GetEmailTemplate("Permohonan Disetujui Level 1", content, "#28a745");
            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendLoanRequestRejectedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Ditolak Level 1";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo <strong>{requesterName}</strong>,
    </p>
    
    <!-- Rejected Badge -->
    <div style='background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;'>
        <div style='font-size: 48px; margin-bottom: 10px;'>✕</div>
        <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 20px;'>Permohonan Ditolak</h2>
        <p style='color: #ffffff; margin: 0; font-size: 14px; opacity: 0.9;'>Level 1 - #{requestNumber}</p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah ditolak oleh Approver Level 1.
    </p>
    
    {(string.IsNullOrEmpty(notes) ? "" : $@"
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8d7da; border-left: 4px solid #dc3545; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0 0 8px 0; color: #721c24; font-size: 14px; font-weight: 600;'>Catatan dari Approver:</p>
                <p style='margin: 0; color: #721c24; font-size: 14px; line-height: 1.6;'>{notes}</p>
            </td>
        </tr>
    </table>
    ")}
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Silakan hubungi Approver Level 1 untuk informasi lebih lanjut.
    </p>
</div>";

            var body = GetEmailTemplate("Permohonan Ditolak", content, "#dc3545");
            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendApprovalL1NotificationToL2Async(string approverL2Email, string requesterName, string requestNumber, string purpose)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Menunggu Persetujuan Level 2";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo,
    </p>
    <p style='font-size: 15px; margin: 0 0 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan berikut telah <strong style='color: #28a745;'>disetujui Level 1</strong> dan memerlukan persetujuan Anda:
    </p>
    
    <!-- Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td style='padding: 8px 0; width: 180px; color: #6c757d; font-size: 14px; font-weight: 600;'>
                            Nomor Permohonan:
                        </td>
                        <td style='padding: 8px 0; color: #212529; font-size: 14px; font-weight: 600;'>
                            {requestNumber}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px;'>
                            Pemohon:
                        </td>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {requesterName}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px;'>
                            Keperluan:
                        </td>
                        <td style='padding: 8px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {purpose}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Status Badge -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 12px 20px; text-align: center;'>
                <p style='margin: 0; color: #ffffff; font-size: 14px; font-weight: 600;'>
                    ✓ Disetujui Level 1 - Menunggu Persetujuan Level 2
                </p>
            </td>
        </tr>
    </table>
    
    <!-- CTA Button -->
    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
        <tr>
            <td align='center'>
                <a href='http://localhost:3000' style='display: inline-block; background: linear-gradient(135deg, #0066CC 0%, #004999 100%); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(0, 102, 204, 0.2);'>
                    Buka Sistem
                </a>
            </td>
        </tr>
    </table>
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Silakan login ke sistem untuk meninjau dan memproses permohonan ini.
    </p>
</div>";

            var body = GetEmailTemplate("Permohonan Level 2", content);
            await SendEmailAsync(approverL2Email, subject, body);
        }

        public async Task SendLoanRequestApprovedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Disetujui";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo <strong>{requesterName}</strong>,
    </p>
    
    <!-- Success Badge with Icon -->
    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); border-radius: 12px; padding: 35px 20px; margin: 25px 0; text-align: center; box-shadow: 0 4px 12px rgba(40, 167, 69, 0.2);'>
        <div style='font-size: 64px; margin-bottom: 15px;'>🎉</div>
        <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 24px; font-weight: 700;'>Selamat!</h2>
        <h3 style='color: #ffffff; margin: 0 0 12px 0; font-size: 18px; font-weight: 600;'>Permohonan Disetujui</h3>
        <p style='color: #ffffff; margin: 0; font-size: 15px; opacity: 0.95;'>Nomor Permohonan: <strong>{requestNumber}</strong></p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan Anda telah <strong style='color: #28a745;'>disetujui sepenuhnya</strong>!
    </p>
    
    <!-- Next Steps -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #d1ecf1; border-left: 4px solid #17a2b8; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0 0 8px 0; color: #0c5460; font-size: 14px; font-weight: 600;'>Langkah Selanjutnya:</p>
                <p style='margin: 0; color: #0c5460; font-size: 14px; line-height: 1.6;'>
                    Sistem akan secara otomatis menjadwalkan kendaraan dan driver untuk Anda.<br/>
                    Silakan cek detail jadwal di sistem.
                </p>
            </td>
        </tr>
    </table>
    
    <!-- CTA Button -->
    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
        <tr>
            <td align='center'>
                <a href='http://localhost:3000' style='display: inline-block; background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(40, 167, 69, 0.3);'>
                    Lihat Detail Jadwal
                </a>
            </td>
        </tr>
    </table>
</div>";

            var body = GetEmailTemplate("Permohonan Disetujui", content, "#28a745");
            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendLoanRequestRejectedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{requestNumber} Ditolak Level 2";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo <strong>{requesterName}</strong>,
    </p>
    
    <!-- Rejected Badge -->
    <div style='background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;'>
        <div style='font-size: 48px; margin-bottom: 10px;'>✕</div>
        <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 20px;'>Permohonan Ditolak</h2>
        <p style='color: #ffffff; margin: 0; font-size: 14px; opacity: 0.9;'>Level 2 - #{requestNumber}</p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan Anda dengan nomor <strong>{requestNumber}</strong> telah ditolak oleh Approver Level 2.
    </p>
    
    {(string.IsNullOrEmpty(notes) ? "" : $@"
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8d7da; border-left: 4px solid #dc3545; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0 0 8px 0; color: #721c24; font-size: 14px; font-weight: 600;'>Catatan dari Approver:</p>
                <p style='margin: 0; color: #721c24; font-size: 14px; line-height: 1.6;'>{notes}</p>
            </td>
        </tr>
    </table>
    ")}
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Silakan hubungi Approver Level 2 untuk informasi lebih lanjut.
    </p>
</div>";

            var body = GetEmailTemplate("Permohonan Ditolak", content, "#dc3545");
            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendDriverAssignmentEmailAsync(string driverEmail, string driverName, string requestNumber, string startDatetime, string endDatetime, string destination)
        {
            var subject = $"[Pelindo Car Loan] Penugasan Driver #{requestNumber}";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo <strong>{driverName}</strong>,
    </p>
    
    <!-- Assignment Badge -->
    <div style='background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); border-radius: 12px; padding: 30px 20px; margin: 25px 0; text-align: center; box-shadow: 0 4px 12px rgba(23, 162, 184, 0.2);'>
        <div style='font-size: 56px; margin-bottom: 12px;'>🚗</div>
        <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 22px; font-weight: 700;'>Penugasan Driver Baru</h2>
        <p style='color: #ffffff; margin: 0; font-size: 15px; opacity: 0.95;'>Permohonan #{requestNumber}</p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Anda telah ditugaskan untuk permohonan peminjaman kendaraan dengan nomor <strong>{requestNumber}</strong>.
    </p>
    
    <!-- Assignment Details Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <p style='margin: 0 0 15px 0; color: #0066CC; font-size: 15px; font-weight: 700;'>Detail Penugasan:</p>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td style='padding: 10px 0; width: 140px; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            🕐 Waktu Mulai:
                        </td>
                        <td style='padding: 10px 0; color: #212529; font-size: 14px; font-weight: 600;'>
                            {startDatetime}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            🕐 Waktu Selesai:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px; font-weight: 600;'>
                            {endDatetime}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            📍 Tujuan:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {destination}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Important Notice -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    ⚠️ <strong>Penting:</strong> Harap datang tepat waktu dan periksa kondisi kendaraan sebelum keberangkatan.
                </p>
            </td>
        </tr>
    </table>
    
    <!-- CTA Button -->
    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
        <tr>
            <td align='center'>
                <a href='http://localhost:3000' style='display: inline-block; background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(23, 162, 184, 0.3);'>
                    Lihat Detail Lengkap
                </a>
            </td>
        </tr>
    </table>
    
    <p style='font-size: 14px; margin: 25px 0 0 0; line-height: 1.6; color: #6c757d;'>
        Silakan login ke sistem untuk melihat detail lengkap penugasan dan informasi kendaraan Anda.
    </p>
</div>";

            var body = GetEmailTemplate("Penugasan Driver", content, "#17a2b8");
            await SendEmailAsync(driverEmail, subject, body);
        }
    }
}
