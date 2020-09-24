using System;
using System.Globalization;
using System.Windows.Controls;
using arma_launcher.Properties;

namespace arma_launcher
{
    public class TeamSpeakValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var input = (value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(input) || !Uri.IsWellFormedUriString(input, UriKind.RelativeOrAbsolute))
            {
                return new ValidationResult(false, Resources.InvalidAddress);
            }

            return ValidationResult.ValidResult;
        }
    }
}