using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BridgeClient.DataModel
{
    class HtmlGauge
    {
        public string path { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double width { get; set; }
        public double height { get; set; }

    }

    class VCockpitConfigEntry
    {
        public double size_mm_w { get; set; }
        public double size_mm_h { get; set; }
        public double pixel_size_w { get; set; }
        public double pixel_size_h { get; set; }

        public string panel_path { get; set; }
        public HtmlGauge htmlgauge00 { get; set; } = new HtmlGauge();

    }

    class CfgManager
    {
        public static Dictionary<string, string> titleToAircraftDirectoryName = new Dictionary<string, string>();
        public static Dictionary<string, List<VCockpitConfigEntry>> aircraftDirectoryNameToGaugeList = new Dictionary<string, List<VCockpitConfigEntry>>();
        public static Dictionary<string, CfgFile> aircraftDirectoryNameToCockpitCfg = new Dictionary<string, CfgFile>();

        public static void Initialize(VFS vfs)
        {
            var aircraftCfgs = vfs.FindFiles((f) => f.StartsWith(@"simobjects\airplane") && f.EndsWith("aircraft.cfg")).ToList();
            foreach (var cfg in aircraftCfgs)
            {
                var airplaneDirectoryName = Path.GetFileName(Path.GetDirectoryName(cfg));
                Trace.WriteLine($"Loading from {cfg}");
                try
                {
                    // Load aircraft.cfg
                    var aircraftCfg = new CfgFile(vfs.Resolve(cfg));
                    aircraftCfg.ReadMultipleSections("FLTSIM.", (section) =>
                    {
                        if (section.ContainsKey("title"))
                            Trace.WriteLine($"Title: {section["title"]}");
                        titleToAircraftDirectoryName[section["title"].Replace("\"", "")] = airplaneDirectoryName;
                    });
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failed to load panel.cfg " + ex);
                }
                try
                {
                    // Load panel.cfg
                    var relativePanelCfg = Path.Combine(@"simobjects\airplanes", airplaneDirectoryName, @"panel\panel.cfg");
                    var relativePanelXml = Path.Combine(@"simobjects\airplanes", airplaneDirectoryName, @"panel\panel.xml");
                    var resolvedPanelCfg = vfs.Resolve(relativePanelCfg);
                    if (resolvedPanelCfg != null)
                    {
                        var panelCfg = new CfgFile(resolvedPanelCfg);
                        var gauges = new List<VCockpitConfigEntry>();
                        panelCfg.ReadMultipleSections("VCOCKPIT0", (section) =>
                        {
                            var cfgEntry = new VCockpitConfigEntry();
                            var gaugeKey = section.Keys.FirstOrDefault(j => j.StartsWith("htmlgauge"));
                            if (string.IsNullOrWhiteSpace(gaugeKey))
                            {
                                Trace.WriteLine("Gauge key not found!");
                            }
                            else
                            {
                                var key = section[gaugeKey].Split(',').Select(x => x.Trim()).ToArray();

                                cfgEntry.htmlgauge00.path = key[0];
                                cfgEntry.htmlgauge00.x = int.Parse(key[1]);
                                cfgEntry.htmlgauge00.y = int.Parse(key[2]);
                                cfgEntry.htmlgauge00.width = int.Parse(key[3]);
                                cfgEntry.htmlgauge00.height = int.Parse(key[4]);
                                cfgEntry.panel_path = relativePanelXml;

                                gauges.Add(cfgEntry);
                                Trace.WriteLine($"htmlgauge: {cfgEntry.htmlgauge00.path}");
                            }



                        }, 1);
                        aircraftDirectoryNameToGaugeList[airplaneDirectoryName] = gauges;
                    }
                    else
                    {
                        Trace.WriteLine("No panel.cfg found");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failed to load panel.cfg " + ex);
                }
                try
                {
                    // Load cockpit.cfg
                    var relativeCockpitCfg = Path.Combine(@"simobjects\airplanes", airplaneDirectoryName, @"cockpit.cfg");
                    var resolvedCockpitCfg = vfs.Resolve(relativeCockpitCfg);
                    if (resolvedCockpitCfg != null)
                    {
                        var cockpitCfg = new CfgFile(resolvedCockpitCfg, true);

                        aircraftDirectoryNameToCockpitCfg[airplaneDirectoryName] = cockpitCfg;
                    }
                    else
                    {
                        Trace.WriteLine("No cockpit.cfg found");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failed to load panel.cfg " + ex);
                }
            }
        }
    }
}
