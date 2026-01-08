using FluentValidation;
using PelindoCarLoan.API.DTOs;

namespace PelindoCarLoan.API.Validators
{
    public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequestDto>
    {
        public CreateLoanRequestValidator()
        {
            RuleFor(x => x.ServiceLetterBasis)
                .NotEmpty().WithMessage("Service letter basis is required")
                .MaximumLength(200).WithMessage("Service letter basis cannot exceed 200 characters");

            RuleFor(x => x.Purpose)
                .NotEmpty().WithMessage("Purpose is required")
                .MaximumLength(500).WithMessage("Purpose cannot exceed 500 characters");

            RuleFor(x => x.Destination)
                .NotEmpty().WithMessage("Destination is required")
                .MaximumLength(255).WithMessage("Destination cannot exceed 255 characters");

            RuleFor(x => x.GuestList)
                .NotEmpty().WithMessage("Guest list is required")
                .MaximumLength(500).WithMessage("Guest list cannot exceed 500 characters");

            RuleFor(x => x.HotelAccommodation)
                .MaximumLength(200).WithMessage("Hotel accommodation cannot exceed 200 characters");

            // VehicleId and DriverId are optional now - can be assigned by approver
            RuleFor(x => x.VehicleId)
                .GreaterThan(0).When(x => x.VehicleId.HasValue).WithMessage("Vehicle ID must be greater than 0");

            RuleFor(x => x.DriverId)
                .GreaterThan(0).When(x => x.DriverId.HasValue).WithMessage("Driver ID must be greater than 0");

            RuleFor(x => x.StartDatetime)
                .NotEmpty().WithMessage("Start datetime is required")
                .GreaterThan(DateTime.Now).WithMessage("Start datetime must be in the future");

            RuleFor(x => x.EndDatetime)
                .NotEmpty().WithMessage("End datetime is required")
                .GreaterThan(x => x.StartDatetime).WithMessage("End datetime must be after start datetime");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        }
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        }
    }

    public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(150).WithMessage("Email cannot exceed 150 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required")
                .Must(role => new[] { "PEMOHON", "PIC_APPROVAL_L1", "PIC_APPROVAL_L2", "DRIVER", "ADMIN" }.Contains(role))
                .WithMessage("Invalid role");

            RuleFor(x => x.Division)
                .MaximumLength(100).WithMessage("Division cannot exceed 100 characters");
        }
    }

    public class ProcessApprovalValidator : AbstractValidator<ProcessApprovalDto>
    {
        public ProcessApprovalValidator()
        {
            RuleFor(x => x.LoanRequestId)
                .GreaterThan(0).WithMessage("Invalid loan request ID");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(status => new[] { "APPROVED", "REJECTED" }.Contains(status))
                .WithMessage("Status must be APPROVED or REJECTED");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
        }
    }

    public class AssignScheduleValidator : AbstractValidator<AssignScheduleDto>
    {
        public AssignScheduleValidator()
        {
            RuleFor(x => x.LoanRequestId)
                .GreaterThan(0).WithMessage("Invalid loan request ID");

            RuleFor(x => x.DriverId)
                .GreaterThan(0).WithMessage("Invalid driver ID");

            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("Invalid vehicle ID");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
        }
    }

    public class CreateVehicleValidator : AbstractValidator<CreateVehicleDto>
    {
        public CreateVehicleValidator()
        {
            RuleFor(x => x.PlateNumber)
                .NotEmpty().WithMessage("Plate number is required")
                .MaximumLength(20).WithMessage("Plate number cannot exceed 20 characters");

            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("Brand is required")
                .MaximumLength(50).WithMessage("Brand cannot exceed 50 characters");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Type is required")
                .MaximumLength(50).WithMessage("Type cannot exceed 50 characters");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be at least 1")
                .LessThanOrEqualTo(50).WithMessage("Capacity cannot exceed 50");
        }
    }

    public class CreateDriverValidator : AbstractValidator<CreateDriverDto>
    {
        public CreateDriverValidator()
        {
            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("License number is required")
                .MaximumLength(50).WithMessage("License number cannot exceed 50 characters");

            RuleFor(x => x.LicenseExpiry)
                .NotEmpty().WithMessage("License expiry is required")
                .GreaterThan(DateTime.Now).WithMessage("License must not be expired");

            RuleFor(x => x.ExperienceYears)
                .GreaterThanOrEqualTo(0).When(x => x.ExperienceYears.HasValue)
                .WithMessage("Experience years cannot be negative");
        }
    }
}
