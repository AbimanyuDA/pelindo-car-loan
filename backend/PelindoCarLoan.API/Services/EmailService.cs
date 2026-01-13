using System.Net;
using System.Net.Mail;
using PelindoCarLoan.API.DTOs;

namespace PelindoCarLoan.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendLoanRequestSubmittedEmailAsync(string approverEmail, string approverName, LoanRequestDto loanRequest);
        Task SendLoanRequestApprovedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber);
        Task SendLoanRequestRejectedL1EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes);
        Task SendApprovalL1NotificationToL2Async(string approverL2Email, string approverL2Name, LoanRequestDto loanRequest, string vehiclePlateNumber, string vehicleType, string driverName, string driverPhone);
        Task SendLoanRequestApprovedL2EmailAsync(string requesterEmail, string requesterName, LoanRequestDto loanRequest, string vehiclePlateNumber, string vehicleType, string driverName, string driverPhone);
        Task SendLoanRequestRejectedL2EmailAsync(string requesterEmail, string requesterName, string requestNumber, string notes);
        Task SendDriverAssignmentEmailAsync(string driverEmail, string driverName, string requestNumber, string requesterName, string requesterPhone, LoanRequestDto loanRequest);
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
            // Use direct Imgur link for logo (hosted externally for email compatibility)
            var logoUrl = "https://i.imgur.com/yCQzPge.png";
            
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
                    
                    <!-- Call to Action Section -->
                    <tr>
                        <td style='padding: 30px; text-align: center; background-color: #f8f9fa;'>
                            <p style='margin: 0 0 20px 0; color: #495057; font-size: 15px; font-weight: 600;'>
                                Silakan Cek Sistem untuk Meninjau dan Informasi Lebih Lanjut
                            </p>
                            <a href='http://localhost:3000' style='display: inline-block; background: linear-gradient(135deg, #0066CC 0%, #004999 100%); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(0, 102, 204, 0.2);'>
                                Buka Sistem
                            </a>
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

        public async Task SendLoanRequestSubmittedEmailAsync(string approverEmail, string approverName, LoanRequestDto loanRequest)
        {
            var subject = $"[Pelindo Car Loan] Permohonan Baru #{loanRequest.RequestNumber} - Memerlukan Persetujuan";
            
            // Format tanggal
            var startDate = loanRequest.StartDatetime.ToString("dd MMMM yyyy HH:mm");
            var endDate = loanRequest.EndDatetime.ToString("dd MMMM yyyy HH:mm");
            
            // Link download surat pelayanan
            var downloadLink = !string.IsNullOrEmpty(loanRequest.ServiceLetterFilePath) 
                ? $"<a href='http://localhost:5000/{loanRequest.ServiceLetterFilePath.Replace("\\", "/")}' style='color: #0066CC; text-decoration: none; font-weight: 600;'>Download Surat</a>"
                : "<span style='color: #6c757d;'>-</span>";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo, <strong>{approverName}</strong>
    </p>
    
    <p style='font-size: 15px; margin: 20px 0; line-height: 1.6; color: #495057;'>
        Ada permohonan peminjaman kendaraan baru yang memerlukan persetujuan Anda:
    </p>
    
    <!-- Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #212529; font-size: 15px; font-weight: 700; border-bottom: 2px solid #0066CC;'>
                            Nomor Permohonan: {loanRequest.RequestNumber}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Pemohon:
                        </td>
                        <td style='padding: 10px 0; color: #212529; font-size: 14px;'>
                            {loanRequest.RequesterName}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Email & WhatsApp:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Email: {loanRequest.RequesterEmail ?? "-"}<br/>
                            WhatsApp: {loanRequest.RequesterPhone ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Divisi & Unit Kerja:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Divisi: {loanRequest.RequesterDivision ?? "-"}<br/>
                            Unit Kerja: {loanRequest.RequesterUnitKerja ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Dasar Surat Pelayanan:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.ServiceLetterBasis}<br/>
                            File: {downloadLink}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Keperluan (Tujuan):
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Purpose}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Destinasi:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Destination}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Daftar Tamu:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.GuestList}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Hotel:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.HotelAccommodation ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Waktu Peminjaman:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Mulai: {startDate}<br/>
                            Selesai: {endDate}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
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
        Permohonan peminjaman kendaraan Anda telah disetujui oleh Approver Level 1.
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

        public async Task SendApprovalL1NotificationToL2Async(string approverL2Email, string approverL2Name, LoanRequestDto loanRequest, string vehiclePlateNumber, string vehicleType, string driverName, string driverPhone)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{loanRequest.RequestNumber} Menunggu Persetujuan Level 2";
            
            // Format tanggal
            var startDate = loanRequest.StartDatetime.ToString("dd MMMM yyyy HH:mm");
            var endDate = loanRequest.EndDatetime.ToString("dd MMMM yyyy HH:mm");
            
            // Link download surat pelayanan
            var downloadLink = !string.IsNullOrEmpty(loanRequest.ServiceLetterFilePath) 
                ? $"<a href='http://localhost:5000/{loanRequest.ServiceLetterFilePath.Replace("\\", "/")}' style='color: #0066CC; text-decoration: none; font-weight: 600;'>Download Surat</a>"
                : "<span style='color: #6c757d;'>-</span>";
            
            // WhatsApp link for driver
            var whatsappLink = !string.IsNullOrEmpty(driverPhone) 
                ? $"https://wa.me/{driverPhone.Replace("+", "").Replace(" ", "").Replace("-", "")}"
                : "#";
            
            var content = $@"
<div style='color: #212529;'>
    <p style='font-size: 16px; margin: 0 0 20px 0; line-height: 1.6;'>
        Halo, <strong>{approverL2Name}</strong>
    </p>
    <p style='font-size: 15px; margin: 0 0 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan berikut telah <strong style='color: #28a745;'>disetujui Level 1</strong> dan memerlukan persetujuan Anda:
    </p>
    
    <!-- Vehicle & Driver Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #e8f5e9; border-radius: 8px; margin: 25px 0; border: 2px solid #28a745;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #155724; font-size: 15px; font-weight: 700; border-bottom: 2px solid #28a745;'>
                            ✓ Disetujui Level 1 - Menunggu Persetujuan Level 2
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Kendaraan:
                        </td>
                        <td style='padding: 10px 0; color: #155724; font-size: 14px;'>
                            <strong>{vehiclePlateNumber}</strong> - {vehicleType}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Driver:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px;'>
                            <strong>{driverName}</strong>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Kontak Driver:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px;'>
                            {driverPhone}<br/>
                            <a href='{whatsappLink}' style='display: inline-block; margin-top: 8px; background-color: #25D366; color: white; text-decoration: none; padding: 8px 16px; border-radius: 5px; font-weight: 600; font-size: 13px;'>
                                💬 Hubungi via WhatsApp
                            </a>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Loan Request Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #212529; font-size: 15px; font-weight: 700; border-bottom: 2px solid #0066CC;'>
                            Nomor Permohonan: {loanRequest.RequestNumber}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Pemohon:
                        </td>
                        <td style='padding: 10px 0; color: #212529; font-size: 14px;'>
                            {loanRequest.RequesterName}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Email & WhatsApp:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Email: {loanRequest.RequesterEmail ?? "-"}<br/>
                            WhatsApp: {loanRequest.RequesterPhone ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Divisi & Unit Kerja:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Divisi: {loanRequest.RequesterDivision ?? "-"}<br/>
                            Unit Kerja: {loanRequest.RequesterUnitKerja ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Dasar Surat Pelayanan:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.ServiceLetterBasis}<br/>
                            File: {downloadLink}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Keperluan (Tujuan):
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Purpose}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Destinasi:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Destination}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Daftar Tamu:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.GuestList}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Hotel:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.HotelAccommodation ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Waktu Peminjaman:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Mulai: {startDate}<br/>
                            Selesai: {endDate}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</div>";

            var body = GetEmailTemplate("Permohonan Level 2", content);
            await SendEmailAsync(approverL2Email, subject, body);
        }

        public async Task SendLoanRequestApprovedL2EmailAsync(string requesterEmail, string requesterName, LoanRequestDto loanRequest, string vehiclePlateNumber, string vehicleType, string driverName, string driverPhone)
        {
            var subject = $"[Pelindo Car Loan] Permohonan #{loanRequest.RequestNumber} Disetujui";
            
            // Format tanggal
            var startDate = loanRequest.StartDatetime.ToString("dd MMMM yyyy HH:mm");
            var endDate = loanRequest.EndDatetime.ToString("dd MMMM yyyy HH:mm");
            
            // Link download surat pelayanan
            var downloadLink = !string.IsNullOrEmpty(loanRequest.ServiceLetterFilePath) 
                ? $"<a href='http://localhost:5000/{loanRequest.ServiceLetterFilePath.Replace("\\", "/")}' style='color: #0066CC; text-decoration: none; font-weight: 600;'>Download Surat</a>"
                : "<span style='color: #6c757d;'>-</span>";
            
            // WhatsApp link for driver
            var whatsappLink = !string.IsNullOrEmpty(driverPhone) 
                ? $"https://wa.me/{driverPhone.Replace("+", "").Replace(" ", "").Replace("-", "")}"
                : "#";
            
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
        <p style='color: #ffffff; margin: 0; font-size: 15px; opacity: 0.95;'>Nomor Permohonan: <strong>{loanRequest.RequestNumber}</strong></p>
    </div>
    
    <p style='font-size: 15px; margin: 25px 0; line-height: 1.6; color: #495057;'>
        Permohonan peminjaman kendaraan Anda telah <strong style='color: #28a745;'>disetujui sepenuhnya</strong>!
    </p>
    
    <!-- Vehicle & Driver Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #e8f5e9; border-radius: 8px; margin: 25px 0; border: 2px solid #28a745;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #155724; font-size: 15px; font-weight: 700; border-bottom: 2px solid #28a745;'>
                            Kendaraan & Driver yang Ditugaskan
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Kendaraan:
                        </td>
                        <td style='padding: 10px 0; color: #155724; font-size: 14px;'>
                            <strong>{vehiclePlateNumber}</strong> - {vehicleType}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Driver:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px;'>
                            <strong>{driverName}</strong>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Kontak Driver:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #c3e6cb; color: #155724; font-size: 14px;'>
                            {driverPhone}<br/>
                            <a href='{whatsappLink}' style='display: inline-block; margin-top: 8px; background-color: #25D366; color: white; text-decoration: none; padding: 8px 16px; border-radius: 5px; font-weight: 600; font-size: 13px;'>
                                💬 Hubungi via WhatsApp
                            </a>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Loan Request Details -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #212529; font-size: 15px; font-weight: 700; border-bottom: 2px solid #0066CC;'>
                            Detail Permohonan
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Nomor Permohonan:
                        </td>
                        <td style='padding: 10px 0; color: #212529; font-size: 14px;'>
                            {loanRequest.RequestNumber}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Pemohon:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.RequesterName}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Email & WhatsApp:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Email: {loanRequest.RequesterEmail ?? "-"}<br/>
                            WhatsApp: {loanRequest.RequesterPhone ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Divisi & Unit Kerja:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Divisi: {loanRequest.RequesterDivision ?? "-"}<br/>
                            Unit Kerja: {loanRequest.RequesterUnitKerja ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Dasar Surat Pelayanan:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.ServiceLetterBasis}<br/>
                            File: {downloadLink}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Keperluan (Tujuan):
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Purpose}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Destinasi:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Destination}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Daftar Tamu:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.GuestList}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Hotel:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.HotelAccommodation ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Waktu Peminjaman:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Mulai: {startDate}<br/>
                            Selesai: {endDate}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Important Note -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0; color: #856404; font-size: 14px; line-height: 1.6;'>
                    <strong>⚠️ Catatan Penting:</strong><br/>
                    Harap berkoordinasi dengan driver terkait jadwal keberangkatan.
                </p>
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
    
</div>";

            var body = GetEmailTemplate("Permohonan Ditolak", content, "#dc3545");
            await SendEmailAsync(requesterEmail, subject, body);
        }

        public async Task SendDriverAssignmentEmailAsync(string driverEmail, string driverName, string requestNumber, string requesterName, string requesterPhone, LoanRequestDto loanRequest)
        {
            var subject = $"[Pelindo Car Loan] Penugasan Driver #{requestNumber}";
            
            // Format tanggal
            var startDate = loanRequest.StartDatetime.ToString("dd MMMM yyyy HH:mm");
            var endDate = loanRequest.EndDatetime.ToString("dd MMMM yyyy HH:mm");
            
            // WhatsApp link for requester
            var whatsappLink = !string.IsNullOrEmpty(requesterPhone) 
                ? $"https://wa.me/{requesterPhone.Replace("+", "").Replace(" ", "").Replace("-", "")}"
                : "#";
            
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
    
    <!-- Requester Info Card -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #d1ecf1; border-radius: 8px; margin: 25px 0; border: 2px solid #17a2b8;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #0c5460; font-size: 15px; font-weight: 700; border-bottom: 2px solid #17a2b8;'>
                            Informasi Pemohon
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #0c5460; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Nama Pemohon:
                        </td>
                        <td style='padding: 10px 0; color: #0c5460; font-size: 14px;'>
                            <strong>{requesterName}</strong>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #bee5eb; color: #0c5460; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Kontak Pemohon:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #bee5eb; color: #0c5460; font-size: 14px;'>
                            {requesterPhone}<br/>
                            <a href='{whatsappLink}' style='display: inline-block; margin-top: 8px; background-color: #25D366; color: white; text-decoration: none; padding: 8px 16px; border-radius: 5px; font-weight: 600; font-size: 13px;'>
                                💬 Hubungi via WhatsApp
                            </a>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Travel Details -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <tr>
            <td style='padding: 20px;'>
                <table width='100%' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td colspan='2' style='padding: 12px 0; color: #212529; font-size: 15px; font-weight: 700; border-bottom: 2px solid #0066CC;'>
                            Detail Perjalanan
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; width: 200px; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Divisi & Unit Kerja:
                        </td>
                        <td style='padding: 10px 0; color: #212529; font-size: 14px;'>
                            Divisi: {loanRequest.RequesterDivision ?? "-"}<br/>
                            Unit Kerja: {loanRequest.RequesterUnitKerja ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Keperluan (Tujuan):
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Purpose}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Destinasi:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.Destination}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Daftar Tamu:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.GuestList}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Hotel:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            {loanRequest.HotelAccommodation ?? "-"}
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #6c757d; font-size: 14px; font-weight: 600; vertical-align: top;'>
                            Waktu Peminjaman:
                        </td>
                        <td style='padding: 10px 0; border-top: 1px solid #e9ecef; color: #212529; font-size: 14px;'>
                            Mulai: {startDate}<br/>
                            Selesai: {endDate}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    
    <!-- Important Note -->
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 6px; margin: 25px 0;'>
        <tr>
            <td style='padding: 15px 20px;'>
                <p style='margin: 0; color: #856404; font-size: 14px; line-height: 1.6;'>
                    <strong>⚠️ Catatan Penting:</strong><br/>
                    Harap berkoordinasi dengan pemohon terkait jadwal keberangkatan.
                </p>
            </td>
        </tr>
    </table>
</div>";

            var body = GetEmailTemplate("Penugasan Driver", content, "#17a2b8");
            await SendEmailAsync(driverEmail, subject, body);
        }
    }
}
