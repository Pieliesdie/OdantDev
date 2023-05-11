using System.IO;
using System.Text;
using System.Windows.Markup;
using System;
using System.Windows;
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
            return File.ReadAllText(fileName);
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