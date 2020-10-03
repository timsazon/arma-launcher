using System;
using System.Globalization;
using System.Windows.Controls;
using arma_launcher.Properties;

namespace arma_launcher.View.Dialog.Rule
{
    public class FtpValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var input = (value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("ftp://") || !input.EndsWith(".7z") ||
                !Uri.IsWellFormedUriString(input, UriKind.Absolute))
            {
                return new ValidationResult(false, Resources.InvalidAddress);
            }

            return ValidationResult.ValidResult;
        }
    }
}