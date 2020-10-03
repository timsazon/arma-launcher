using System.Globalization;
using System.Windows.Controls;
using arma_launcher.Properties;

namespace arma_launcher.View.Dialog.Rule
{
    public class TeamSpeakValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var input = (value ?? "").ToString();
            return string.IsNullOrWhiteSpace(input)
                ? new ValidationResult(false, Resources.InvalidAddress)
                : ValidationResult.ValidResult;
        }
    }
}