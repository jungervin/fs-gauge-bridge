using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using BridgeClient;
using Newtonsoft.Json;
using BridgeClient.DataModel;
using System.Net.WebSockets;


public class WSMessage
{
    public string type { get; set; }
    public WSValue[] values { get; set; }
}


public class WSValue
{
    public string name { get; set; }
    public string unit { get; set; }
    public object value { get; set; }
}

class CfgData
{
    public VCockpitConfigEntry[] gauges { get; set; }
    public Dictionary<string, string> cockpitcfg { get; set; }
}

class SimpleHTTPServer
{
    public string Url { get; private set; }

    private WebserverSettings _settings;
    private VFS _vfs;
    private int _port;
   static  private List<Action<string>> m_sockets = new List<Action<string>>();

    public SimpleHTTPServer(VFS vfs, WebserverSettings settings)
    {
        _vfs = vfs;
        _settings = settings;
        _port = settings.Port ?? AssignPort();

        var serverThread = new Thread(this.ServerProc);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    private void ServerProc()
    {
        Url = "http://127.0.0.1:" + _port.ToString() + "/";
        Trace.WriteLine($"Server starting at {Url}");

        var listener = new HttpListener();
        listener.Prefixes.Add(Url);
        listener.Start();
        while (true)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                Process(context);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Server request error (handed): " + ex);
            }
        }
    }

    private void Process(HttpListenerContext context)
    {
        string filename = context.Request.Url.AbsolutePath.Substring(1).ToLower();

        context.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");

        context.Response.AddHeader("Pragma", "no-cache");
        context.Response.AddHeader("expires", "0");

        if (filename == "ws")
        {
            if (context.Request.IsWebSocketRequest)
            {
                HandleWebSocketRequest(context);
                return;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
        else if (filename == "all_cfg")
        {
            if (SimConnectViewModel.Instance.Title != null)
            {
                var aircraftFolder = CfgManager.titleToAircraftDirectoryName[SimConnectViewModel.Instance.Title];

                var data = new CfgData();
                data.gauges = CfgManager.aircraftDirectoryNameToGaugeList[aircraftFolder].ToArray();
                data.cockpitcfg = CfgManager.aircraftDirectoryNameToCockpitCfg[aircraftFolder].Values["AIRSPEED"];

                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "text/json";
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.OutputStream.Flush();
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        else
        {
            var pathOnDisk = _vfs.Resolve(Path.Combine("html_ui", filename));
            if (pathOnDisk == null && filename.StartsWith("vfs/"))
            {
                filename = filename.Remove(0, 4);
         
                    pathOnDisk = _vfs.Resolve(filename);
                    Trace.WriteLine("VFS: " + pathOnDisk);
            
            }

            if (pathOnDisk != null)
            {
                //  Trace.WriteLine($"Serving {filename} -> {pathOnDisk}");
                try
                {
                    Stream input = new FileStream(pathOnDisk, FileMode.Open);

                    string mime;
                    context.Response.ContentType = MimeTypes.Mappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("## Failed reading file " + ex);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                if (!filename.Contains(".js.map") &&
                    filename != "favicon.ico")
                {
                    Console.WriteLine("## 404: " + filename);
                }
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        context.Response.OutputStream.Close();
    }


    private async void HandleWebSocketRequest(HttpListenerContext context)
    {
        try
        {
            Trace.WriteLine("WS: Connecting to ws client");
            var socketContext = await context.AcceptWebSocketAsync(null);
            var sock = socketContext.WebSocket;
            Action<string> writeSock = (txtToWrite) =>
            {
              //  Trace.WriteLine("WS: Send: " + txtToWrite.Length);

                var writeBytes = context.Request.ContentEncoding.GetBytes(txtToWrite);
                sock.SendAsync(new ArraySegment<byte>(writeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            };
            m_sockets.Add(writeSock);
            try
            {
                while (sock.State == WebSocketState.Open)
                {
                    byte[] receiveBuffer = new byte[1024 * 50];
                    int bufferStart = 0;
                   
                    WebSocketReceiveResult receiveResult = null;
                    do
                    {
                        receiveResult = await sock.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, bufferStart, receiveBuffer.Length - bufferStart), CancellationToken.None);
                        bufferStart += receiveResult.Count;

                    } while (!receiveResult.EndOfMessage);


                    var ret = context.Request.ContentEncoding.GetString(receiveBuffer);
                    if (!string.IsNullOrWhiteSpace(ret))
                    {
                        try
                        {
                            var msg = JsonConvert.DeserializeObject<WSMessage>(ret);
                            if (msg != null)
                            {
                                OnGotSocketMessage(msg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("-----------------");
                            Trace.WriteLine(ret);
                            Trace.WriteLine("-----------------");

                            Trace.WriteLine("WS: Error: " + ex);
                            Trace.WriteLine("-----------------");

                        }
                    }
                }
            }
            finally
            {
                Trace.WriteLine("WS: Lost client");

                m_sockets.Remove(writeSock);
            }
        }
        catch(WebSocketException ex)
        {
            context.Response.StatusCode = 500;

        }
        catch (Exception ex)
        {
            Trace.WriteLine("WS: Error: " + ex);
            context.Response.StatusCode = 500;

            context.Response.Close();
        }
    }

    public static void TakeOperation(WSValue[] values)
    {
        WSMessage data = new WSMessage();
        data.type = "data";
        data.values = values;
        Broadcast(data);
    }


    private void OnGotSocketMessage(WSMessage msg)
    {
        try
        {
            switch (msg.type)
            {
                case "hello":
                case "read":
                    SimConnectViewModel.Instance.AdviseVariables(msg.values.Select(v => new WSValue { name = v.name, unit = v.unit }).ToList());
                    break;
                case "write":
                    {
                        foreach (var v in msg.values)
                        {
                            v.value = double.Parse((string)v.value);
                            SimConnectViewModel.Instance.Write(v);
                        }
                    }
                    // SimConnectViewModel.Instance.Write((string)data["name"], (string)data["unit"], double.Parse((string)data["value"]));
                    break;
                default: throw new NotImplementedException();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("WS: " + ex);
        }
    }

    public static void Broadcast(WSMessage msg)
    {
        var msgText = JsonConvert.SerializeObject(msg);
        foreach (var client in m_sockets.ToArray())
        {
            client(msgText);
        }
    }

    private int AssignPort()
    {
        Trace.WriteLine("Probing for an empty port...");
        // get an empty port
        var temporaryListener = new TcpListener(IPAddress.Loopback, 0);
        temporaryListener.Start();
        var port = ((IPEndPoint)temporaryListener.LocalEndpoint).Port;
        temporaryListener.Stop();
        return port;
    }
}