using System.Windows.Markup;
namespace OdantDev;
public class TextExtension(string fileName) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            var path = Path.Combine(VsixEx.VsixPath.FullName, fileName);
            return System.IO.File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}