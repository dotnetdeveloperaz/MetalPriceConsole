using System.Diagnostics.Tracing;
using System.Xml.Linq;

namespace GoldPriceConsole;

[EventSource(Name = "Gold Price Console")]
public class GoldPriceEventSource : EventSource
{
    public static GoldPriceEventSource Log = new GoldPriceEventSource();

    public void Trace(string message)
    {
        if (IsEnabled())
            WriteEvent(1, message);
    }
}