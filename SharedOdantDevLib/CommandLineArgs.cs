namespace OdantDev;

[Serializable]
public class CommandLineArgs
{
    public int ProcessId { get; set; }

    public List<string> ExternalAssemblies { get; set; } 
}
