using CineAboutApp.Models.Requests.User;
using CineAboutApp.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Validators
{
    public class UserInsertOrUpdateValidator : AbstractValidator<UserInsertOrUpdateRequest>
    {
        public UserInsertOrUpdateValidator(ApplicationDbContext db, int? userId = null, bool isAdmin = false)
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Korisničko ime je obavezno.")
                .MinimumLength(3).WithMessage("Korisničko ime mora imati najmanje 3 karaktera.")
                .MaximumLength(50).WithMessage("Korisničko ime ne smije biti duže od 50 karaktera.")
                .Matches("^[A-Za-z0-9.]+$").WithMessage("Korisničko ime može sadržavati samo slova engleskog alfabeta (A-Z, a-z), brojeve i tačku.");

            RuleFor(x => x.CountryId)
                .GreaterThan(0).WithMessage("CountryId must be a positive integer.")
                .MustAsync(async (countryId, cancellation) => 
                    await db.Countries.Where(c => c.Id == countryId).FirstOrDefaultAsync() != null)
                .WithMessage("Country with the specified ID does not exist.");

            RuleFor(x => x.RoleIds).ForEach(roleId =>
            {
                roleId.GreaterThan(0).WithMessage("Role ID must be a positive integer.");
                roleId.MustAsync(async (roleId, cancellation) => 
                    await db.Roles.Where(r => r.Id == roleId).FirstOrDefaultAsync() != null)
                    .WithMessage("Role with the specified ID does not exist.");
            });

            if(userId == null)
            {
                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Lozinka je obavezna.")
                    .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 karaktera.")
                    .MaximumLength(100).WithMessage("Lozinka ne smije biti duža od 100 karaktera.")
                    .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,100}$")
                    .WithMessage("Lozinka mora sadržavati najmanje jedno veliko slovo, jedno malo slovo, jedan broj i jedan specijalni znak (npr. Lozinka123!).");

                RuleFor(x => x.PasswordConfirmation)
                    .Equal(x => x.Password).WithMessage("Lozinka i potvrda lozinke se ne podudaraju.")
                    .When(x => !string.IsNullOrEmpty(x.PasswordConfirmation));
            }
            else
            {
                RuleFor(x => x.Password)
                    .MinimumLength(8).WithMessage("Nova lozinka mora imati najmanje 8 karaktera.")
                    .MaximumLength(100).WithMessage("Nova lozinka ne smije biti duža od 100 karaktera.")
                    .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,100}$")
                    .WithMessage("Nova lozinka mora sadržavati najmanje jedno veliko slovo, jedno malo slovo, jedan broj i jedan specijalni znak.")
                    .When(x => !string.IsNullOrEmpty(x.Password));

                RuleFor(x => x.PasswordConfirmation)
                    .Equal(x => x.Password).WithMessage("Lozinka i potvrda lozinke se ne podudaraju.")
                    .When(x => !string.IsNullOrEmpty(x.Password));
            }

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(75).WithMessage("First name must not exceed 75 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(75).WithMessage("Last name must not exceed 75 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email adresa je obavezna.")
                .EmailAddress().WithMessage("Unesite ispravnu email adresu u formatu: ime@domena.com")
                .MaximumLength(150).WithMessage("Email adresa ne smije biti duža od 150 karaktera.");

                RuleFor(x => x).MustAsync(async (x, cancellation) =>
                        await db.MyAppUsers.Where(u => u.Email.ToLower() == x.Email.ToLower() && (u.Id != userId || userId == null)).FirstOrDefaultAsync() == null).WithMessage("Email adresa je već registrovana na drugom računu.")
                        .MustAsync(async (x, cancellation) =>
                        await db.MyAppUsers.Where(u => u.Username.ToLower() == x.Username.ToLower() && (u.Id != userId || userId == null)).FirstOrDefaultAsync() == null).WithMessage("Korisničko ime je zauzeto.");

        }
    }
}
