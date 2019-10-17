using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SignalRStreaming
{
    public class HubBuffer
    {
        public ConcurrentDictionary<string, ChannelReader<string>> BufferChannel { get; set; } = new ConcurrentDictionary<string, ChannelReader<string>>();
    }
}
