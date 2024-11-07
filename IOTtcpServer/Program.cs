using System.Net;
using System.Net.Sockets;
using System.Text;

int         port     = 15323;
TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"Server listening on port {port}...");

TcpClient?      esp32Client    = null;                  // 用于保存ESP32的连接
List<TcpClient> controlClients = new List<TcpClient>(); // 控制客户端列表

while (true)
{
    TcpClient client = listener.AcceptTcpClient();
    Console.WriteLine("Client connected.");
    Thread clientThread = new Thread(()=>HandleClient(client));
    clientThread.Start();
}

void HandleClient(TcpClient client)
{
    NetworkStream stream = client.GetStream();
    byte[]        buffer = new byte[1024];
    int           bytesRead;

    try
    {
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string clientType = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        if (clientType == "ESP32")
        {
            // 如果是ESP32，保存为esp32Client
            esp32Client = client;
            Console.WriteLine("ESP32 connected.");
        }
        else if (clientType == "CONTROL")
        {
            // 如果是控制客户端，将其加入列表
            lock (controlClients)
            {
                controlClients.Add(client);
            }
            Console.WriteLine("Control client connected.");
        }

        // 监听并处理控制端的消息
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine("Received: " + receivedMessage);

            if (clientType == "CONTROL" && receivedMessage.StartsWith("COMMAND:"))
            {
                // 将命令转发给ESP32
                if (esp32Client != null && esp32Client.Connected)
                {
                    SendMessage(esp32Client.GetStream(), receivedMessage);
                    Console.WriteLine("Forwarded to ESP32: " + receivedMessage);
                }
                else
                {
                    Console.WriteLine("ESP32 is not connected.");
                }
            }
            else if (clientType == "ESP32")
            {
                // 处理ESP32的响应，或将响应转发给控制客户端（如需要）
                Console.WriteLine("Message from ESP32: " + receivedMessage);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
    finally
    {
        Console.WriteLine("Client disconnected.");
        client.Close();

        if (client == esp32Client)
        {
            esp32Client = null;
        }
        else
        {
            lock (controlClients)
            {
                controlClients.Remove(client);
            }
        }
    }
}

// 发送消息方法
void SendMessage(NetworkStream stream, string message)
{
    byte[] data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
    Console.WriteLine("Sent: " + message);
}