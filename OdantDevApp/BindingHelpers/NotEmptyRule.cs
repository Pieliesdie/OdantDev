using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OdantDev;

public class NotEmptyRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        switch (value)
        {
            case null:
            case "MyToolWindow":
                return new ValidationResult(false, "Value can't be null");
            default:
                return new ValidationResult(true, null);
        }
    }
}
