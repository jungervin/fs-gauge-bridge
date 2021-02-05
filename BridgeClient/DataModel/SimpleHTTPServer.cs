// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

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

class GetSimVarValueData
{
    public string name { get; set; }
    public string unit { get; set; }
}

class CfgData
{
    public string[] gauges { get; set; }
    public Dictionary<string,string> cockpitcfg { get; set; }
}

class SimpleHTTPServer
{
    public string Url { get; private set; }

    private WebserverSettings _settings;
    private VFS _vfs;
    private int _port;

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

        if (filename == "getsimvarvalue") 
        {
            string text;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                text = reader.ReadToEnd();
            }
            context.Response.ContentType = "text/json";

            var names = JsonConvert.DeserializeObject<GetSimVarValueData[]>(text);

            SimConnectViewModel.Instance.Read(names.ToList());

            var json = SimConnectViewModel.Instance.GetJson();
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Flush();
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        else if (filename == "setsimvarvalue")
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                var text = reader.ReadToEnd();
                context.Response.ContentType = "text/json";

                var parts = JsonConvert.DeserializeObject<string[]>(text);

                SimConnectViewModel.Instance.Write(parts[0], parts[1], double.Parse(parts[2]));
                var json = SimConnectViewModel.Instance.GetJson();

                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.OutputStream.Flush();

                context.Response.StatusCode = (int)HttpStatusCode.OK;
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
                var bytes = Encoding.UTF8.GetBytes( json);

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
                if (SimConnectViewModel.Instance.Title != null)
                {
                    var aircraftFolder = CfgManager.titleToAircraftDirectoryName[SimConnectViewModel.Instance.Title];
                    filename = filename.Replace("vfs/", ""); // risky

                    pathOnDisk = _vfs.Resolve(
                        Path.Combine(@"simobjects\airplanes\",
                        aircraftFolder,
                        "panel",
                        filename));

                    Trace.WriteLine("VFS: " + pathOnDisk);
                }
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