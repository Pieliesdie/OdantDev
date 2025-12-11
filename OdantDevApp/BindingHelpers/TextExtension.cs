using System;
using System.IO;
using System.Windows.Markup;
namespace OdantDev;
public class TextExtension(string fileName) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            var path = Path.Combine(VsixEx.VsixPath.FullName, fileName);
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}