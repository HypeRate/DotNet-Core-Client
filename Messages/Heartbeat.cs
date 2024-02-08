public class Heartbeat
{
    public readonly string topic = "phoenix";

    public readonly string @event = "heartbeat";

    public readonly Dictionary<string, string> payload = new Dictionary<string, string>();

    public readonly uint @ref = 0;
}
