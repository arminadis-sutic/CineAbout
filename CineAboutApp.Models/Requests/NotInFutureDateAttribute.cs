using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class NotInFutureDateAttribute(int minYear = 1850) : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is not DateTime date)
            {
                return false;
            }

            return date >= new DateTime(minYear, 1, 1) && date.Date <= DateTime.UtcNow.Date;
        }

        public override string FormatErrorMessage(string name) => ErrorMessage ?? $"Datum mora biti između {minYear}. godine i danas.";
    }
}
