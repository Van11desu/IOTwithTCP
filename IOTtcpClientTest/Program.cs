using System.Net.Sockets;
using System.Text;

//string serverIp = "frp-fly.top";
//int    port     = 28208;
string serverIp = "127.0.0.1";
int    port     = 15323;

try
{
    using TcpClient client = new TcpClient(serverIp, port);
    Console.WriteLine("Connected to server.");

    NetworkStream stream = client.GetStream();

    // 发送客户端类型
    SendMessage(stream, "CONTROL");

    while (true)
    {
        Console.Write("Enter command (e.g., COMMAND:LED:ON): ");
        string command = Console.ReadLine();
        if (string.IsNullOrEmpty(command)) break;

        SendMessage(stream, command);
        Console.WriteLine("Command sent.");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

void SendMessage(NetworkStream stream, string message)
{
    byte[] data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
}