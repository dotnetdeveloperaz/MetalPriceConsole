using System.Diagnostics.Tracing;

namespace MetalPriceConsole;

[EventSource(Name = "Metal Price Console")]
public class MetalPriceEventSource : EventSource
{
    public static MetalPriceEventSource Log = new MetalPriceEventSource();

    public void Trace(string message)
    {
        if (IsEnabled())
            WriteEvent(1, message);
    }
}