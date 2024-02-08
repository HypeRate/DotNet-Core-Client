using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class HypeRateClient : IDisposable
{
    private readonly Uri server_url;

    private readonly ClientWebSocket websocket_client;

    private readonly Thread heartbeat_thread;

    private readonly List<string> send_buffer;
    private readonly Thread send_thread;

    private readonly Thread receive_thread;

    public HypeRateClient(string websocketToken,
                          int heartbeatIntervallInMilliseconds = 10_000,
                          string? websocketUrl = null)
    {
        server_url = new Uri(
            string.Format("{0}?token={1}",
            websocketUrl ?? "wss://app.hyperate.io/socket/websocket",
            websocketToken)
        );
        websocket_client = new ClientWebSocket();
        send_buffer = [];

        heartbeat_thread = new Thread(() =>
        {
            while (true)
            {
                send_buffer.Add(JsonConvert.SerializeObject(new Heartbeat()));
                Thread.Sleep(heartbeatIntervallInMilliseconds);
            }
        });

        send_thread = new Thread(async () =>
        {
            while (true)
            {
                if (websocket_client.State != WebSocketState.Open)
                {
                    continue;
                }

                if (send_buffer.Count == 0)
                {
                    continue;
                }

                string item_to_send = send_buffer[0];

                if (item_to_send == null)
                {
                    continue;
                }

                send_buffer.RemoveAt(0);
                ArraySegment<byte> buffer = new(System.Text.Encoding.UTF8.GetBytes(item_to_send));

                await websocket_client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

                Thread.Sleep(1);
            }
        });

        receive_thread = new Thread(async () =>
        {
            try
            {
                ArraySegment<byte> input_buffer = new(new byte[1024]);
                ArraySegment<byte> temp_input_buffer = new(new byte[1024]);

                while (true)
                {
                    if (websocket_client.State != WebSocketState.Open)
                    {
                        continue;
                    }

                    WebSocketReceiveResult receive_result = await websocket_client.ReceiveAsync(temp_input_buffer, CancellationToken.None);

                    byte[] result_array = [];

                    if (input_buffer.Array != null && temp_input_buffer.Array != null)
                    {
                        result_array = [.. input_buffer.Array, .. temp_input_buffer.Array];
                    }

                    if (receive_result.EndOfMessage)
                    {
                        HandleBuffer(result_array, receive_result.Count);
                        input_buffer = new ArraySegment<byte>(new byte[1024]);
                        temp_input_buffer = new ArraySegment<byte>(new byte[1024]);
                    }
                    else
                    {
                        temp_input_buffer = new ArraySegment<byte>(new byte[1024]);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        });
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        await websocket_client.ConnectAsync(server_url, cancellationToken);

        send_thread.Start();
        receive_thread.Start();
        heartbeat_thread.Start();
    }

    public async void Disconnect(CancellationToken cancellationToken)
    {
        await websocket_client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
    }

    public static void HandleBuffer(byte[] buffer, int byteCount)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        string utf8_string = System.Text.Encoding.UTF8.GetString(buffer);

        if (utf8_string.Length == 0)
        {
            return;
        }

        JObject json_object = JObject.Parse(utf8_string);

        string? @event = (string?)json_object["event"];

        switch (@event)
        {
            case "hr_update":
                IEnumerable<char>? id = string.Join("", ((string?)json_object["topic"])?.Skip(3));
                int? heartbeat = (int?)json_object["payload"]["hr"];

                Console.WriteLine($"Got new heartbeat for ID {id}: {heartbeat}");
                break;
            case "phx_reply":
                break;
            case "phx_close":
                break;
            default:
                Console.WriteLine($"Unknown incomming event {@event}");
                break;
        }
    }

    public void JoinChannel(string channelToJoin)
    {
        send_buffer.Add(JsonConvert.SerializeObject(new JoinChannel(channelToJoin)));
    }

    public void LeaveChannel(string channelToLeave)
    {
        send_buffer.Add(JsonConvert.SerializeObject(new LeaveChannel(channelToLeave)));
    }

    public void Dispose()
    {
        websocket_client.Dispose();
    }
}
