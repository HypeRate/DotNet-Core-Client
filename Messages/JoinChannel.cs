public class JoinChannel
{
    public readonly string topic;
    public readonly string @event = "phx_join";
    public readonly Dictionary<string, string> payload = new Dictionary<string, string>();
    public readonly uint @ref = 0;

    public JoinChannel(string channel_to_join)
    {
        this.topic = $"hr:{channel_to_join}";
    }
}
