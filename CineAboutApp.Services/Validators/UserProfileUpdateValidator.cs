using CineAboutApp.Models.Requests.User;
using FluentValidation;

namespace CineAboutApp.Services.Validators
{
    public class UserProfileUpdateValidator : AbstractValidator<UserProfileUpdateRequest>
    {
        public UserProfileUpdateValidator()
        {
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio ne smije biti duži od 500 znakova.");

            RuleFor(x => x.CityId)
                .GreaterThanOrEqualTo(0).WithMessage("Odabrani grad nije ispravan.");

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9\s\-\/\(\)]{7,20}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Neispravan format telefona. Unesite u formatu: +387 61 123 456 ili 061123456.");

            RuleFor(x => x.DateOfBirth)
                .Must(d => d!.Value.Date <= DateTime.UtcNow.Date && d.Value.Year >= 1900)
                .When(x => x.DateOfBirth.HasValue)
                .WithMessage("Datum rođenja mora biti između 1900. godine i danas.");
        }
    }
}
