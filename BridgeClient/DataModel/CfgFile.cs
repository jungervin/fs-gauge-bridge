using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeClient.DataModel
{
    class CfgFile
    {
        public Dictionary<string, Dictionary<string, string>> Values { get; } = new Dictionary<string, Dictionary<string, string>>();

        public CfgFile(string path, bool caseSensitive = false)
        {
            var lines = File.ReadAllLines(path);

            Dictionary<string, string> current = null;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.StartsWith("["))
                    {
                        current = new Dictionary<string, string>();
                        Values[line.Replace("[", "").Replace("]", "").ToUpper()] = current;
                    }
                    else
                    {
                        var strippedLine = line.Split(';')[0].Trim();
                        if (!string.IsNullOrWhiteSpace(strippedLine) && strippedLine.Contains("=") && !strippedLine.StartsWith("//"))
                        {
                            var parts = strippedLine.Split('=');
                            var key = parts[0];
                            var value = strippedLine.Substring(key.Length + 1).Trim();
                            current[caseSensitive ? key.Trim() : key.Trim().ToLower()] = value;
                        }
                    }
                }
            }
        }

        public void ReadMultipleSections(string section, Action<Dictionary<string,string>, string> eachSectionCallback, int startId = 0, int minId = 20)
        {
            int id = startId;
            while (true)
            {
                var sectionTitle = $"{section}{id}";
                if (Values.ContainsKey(sectionTitle))
                {
                    eachSectionCallback(Values[sectionTitle], sectionTitle);
                    id++;
                }
                else
                {
                    if (id++ > minId)
                    {
                        break;
                    }
                }
            }
        }
    }
}
