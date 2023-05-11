using System;
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
            //var uri = new Uri("pack://application:,,,/" + fileName);
            //using var stream = Application.GetResourceStream(uri).Stream;
            //using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            //return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}