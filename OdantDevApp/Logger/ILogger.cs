using System;

namespace OdantDev.Model;
public interface ILogger
{
    public void Info(string message);

    public void Error(string message);

    public void Exception(Exception ex);
}
