using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;


public class CPHInline
{
    ConnectionManager cm = new ConnectionManager();

    public void Init()
    {
        cm.InitializeClients();
    }

    public bool Execute()
    {
        return SendConsole();
    }
    
    public bool SendConsole()
    {
        if (!CPH.TryGetArg("consoleCommand", out string command))
        {
            CPH.LogError("Missing required 'consoleCommand' argument.");
            return false;
        }
        
        cm.SendMessage(command);
        return true;
    }
}

//////////////////////////////////////////////////////////////////////
///
/// Original FiveM integration code copied from https://github.com/josh-tf/fxcommands/
///
class ConnectionManager
{
    private static Dictionary<string, TcpClient> tcpClients = new Dictionary<string, TcpClient>(10);

    public void SendMessage(string message, bool canRetry = true)
    {
        string ipAddress = "127.0.0.1";
        int port = 29200; // fx console

        byte[] b_header = "43:4d:4e:44:00:d2:00:00".Split(':').Select(s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber)).ToArray();
        byte[] b_command = Encoding.UTF8.GetBytes(message + "\n");
        byte[] b_padding = { 0, 0 };
        byte[] b_length = BitConverter.GetBytes((message.Length + 13));
        byte[] b_terminator = { 00 };

        Array.Reverse(b_length);

        byte[] data = b_header.Concat(b_length).Concat(b_padding).Concat(b_command).Concat(b_terminator).ToArray();

        string tcpClientIdentifier = $"{ipAddress}::{port}";

        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAddress), port);

            if (!tcpClients.ContainsKey(tcpClientIdentifier))
            {
                tcpClients.Add(tcpClientIdentifier, new TcpClient() { NoDelay = true });
            }
            else
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections()
                    .Where(x => x.LocalEndPoint.Equals(tcpClients[tcpClientIdentifier].Client.LocalEndPoint) &&
                           x.RemoteEndPoint.Equals(tcpClients[tcpClientIdentifier].Client.RemoteEndPoint))
                    .ToArray();

                if (tcpConnections == null || tcpConnections.Length == 0 || tcpConnections.First().State != TcpState.Established)
                {
                    tcpClients[tcpClientIdentifier].Close();
                    tcpClients[tcpClientIdentifier] = new TcpClient() { NoDelay = true };
                }
            }

            if (!tcpClients[tcpClientIdentifier].Connected)
            {
                tcpClients[tcpClientIdentifier].Connect(ep);
            }

            if (tcpClients[tcpClientIdentifier].Connected)
            {
                var tcpStream = tcpClients[tcpClientIdentifier].GetStream();
                tcpStream.Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            tcpClients[tcpClientIdentifier].Close();
            tcpClients[tcpClientIdentifier] = new TcpClient() { NoDelay = true };
            Console.WriteLine(ex.ToString());
        }
    }

    public void InitializeClients()
    {
        tcpClients = new Dictionary<string, TcpClient>();
    }

    public void Dispose()
    {
        foreach (var client in tcpClients.Values)
        {
            client.Close();
            client.Dispose();
        }
    }
}

//////////////////////////////////////////////////////////////////////
///
/// LICENSE
///
/*

MIT License

Copyright (c) 2025 josh-tf

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
  
*/
