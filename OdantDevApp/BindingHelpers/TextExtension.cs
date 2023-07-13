using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Markup;
namespace OdantDev;
public class TextExtension : MarkupExtension
{
    private readonly string fileName;

    public TextExtension(string fileName)
    {
        this.fileName = fileName;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            var path = Path.Combine(VsixExtension.VSIXPath.FullName, fileName);
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}