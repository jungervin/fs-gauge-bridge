using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BridgeClient.DataModel
{
    class CfgManager
    {
        public static Dictionary<string, string> titleToAircraftDirectoryName = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> aircraftDirectoryNameToGaugeList = new Dictionary<string, List<string>>();
        public static Dictionary<string, CfgFile> aircraftDirectoryNameToCockpitCfg = new Dictionary<string, CfgFile>();

        public static void Initialize(VFS vfs)
        {
            var aircraftCfgs = vfs.FindFiles((f) => f.StartsWith(@"simobjects\airplane") && f.EndsWith("aircraft.cfg")).ToList();
            foreach (var cfg in aircraftCfgs)
            {
                var airplaneDirectoryName = Path.GetFileName(Path.GetDirectoryName(cfg));

                // Load aircraft.cfg
                var aircraftCfg = new CfgFile(vfs.Resolve(cfg));
                aircraftCfg.ReadMultipleSections("FLTSIM.", (section) =>
                {
                    if (section.ContainsKey("title"))
                    Trace.WriteLine($"Title: {section["title"]}");
                    titleToAircraftDirectoryName[section["title"].Replace("\"", "")] = airplaneDirectoryName;
                });

                // Load panel.cfg
                var relativePanelCfg = Path.Combine(@"simobjects\airplanes", airplaneDirectoryName, @"panel\panel.cfg");
                var resolvedPanelCfg = vfs.Resolve(relativePanelCfg);
                if (resolvedPanelCfg != null)
                {
                    var panelCfg = new CfgFile(resolvedPanelCfg);
                    var gauges = new List<string>();
                    panelCfg.ReadMultipleSections("VCOCKPIT0", (section) =>
                    {
                        var key = section[section.Keys.FirstOrDefault(j => j.StartsWith("htmlgauge"))];
                        gauges.Add(key);
                        Trace.WriteLine($"htmlgauge: {key}");
                    }, 1);
                    aircraftDirectoryNameToGaugeList[airplaneDirectoryName] = gauges;
                }
                else
                {
                    Trace.WriteLine("No panel.cfg found");
                }
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
        }
    }
}
