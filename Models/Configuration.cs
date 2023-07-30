namespace MetalPriceConsole.Models;

public class Configuration
{
    public ApiServer ApiServer { get; set; }
    public Logging Logging { get; set; }
    public ConnectionStrings ConnectionStrings { get; set; }
    public string AllowedHosts { get; set; }
}

public class ApiServer
{
    public string BaseUrl { get; set; }
    public string Token { get; set; }
    public string Gold { get; set; }
    public string Palladium { get; set; }
    public string Platinum { get; set; }
    public string Silver { get; set; }
    public string Currency { get; set; }
    public string MonthlyAllowance { get; set; }
}

public class ConnectionStrings
{
    public string DefaultDB { get; set; }
}

public class Logging
{
    public LogLevel LogLevel { get; set; }
}

public class LogLevel
{
    public string Default { get; set; }
    public string Microsoft { get; set; }

    public string MicrosoftHostingLifetime { get; set; }
}
