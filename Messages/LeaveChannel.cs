public class LeaveChannel
{
    public readonly string topic;
    public readonly string @event = "phx_leave";
    public readonly Dictionary<string, string> payload = new Dictionary<string, string>();
    public readonly uint @ref = 0;

    public LeaveChannel(string channel_to_leave)
    {
        this.topic = $"hr:{channel_to_leave}";
    }
}
