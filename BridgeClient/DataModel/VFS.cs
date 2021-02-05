using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BridgeClient.DataModel
{
    class VFS
    {
        private readonly Dictionary<string, string> m_vfsPaths = new Dictionary<string, string>();
        private readonly VFSSettings _settings;

        public VFS(VFSSettings settings)
        {
            _settings = settings;

            DetectFSInstallationType();
            MapAndAddPackagesFromSettings();
        }

        public IEnumerable<string> FindFiles(Func<string, bool> isFileIncluded)
        {
            var files = m_vfsPaths.Keys.Where((f) => isFileIncluded(f));
            return files;
        }

        public string Resolve(string path)
        {
            path = path.Replace("/", @"\").ToLower().Trim();
            return m_vfsPaths.ContainsKey(path) ? m_vfsPaths[path] : null;
        }

        public void AddPackageDirectory(string dirPath)
        {
            dirPath = Environment.ExpandEnvironmentVariables(dirPath);
            Trace.WriteLine($"VFS: Loading packages from {dirPath}");

            var packageDirs = Directory.GetDirectories(dirPath);
            foreach (var packageFolder in packageDirs)
            {
                AddImpl("", packageFolder);
            }
            Trace.WriteLine($"VFS: Added {packageDirs.Length} packages");
        }

        private void AddImpl(string vfsPath, string pathToAdd)
        {
            foreach(var filePath in Directory.GetFiles(pathToAdd))
            {
                var vfsFilePath = Path.Combine(vfsPath, Path.GetFileName(filePath)).ToLower().Trim();
                m_vfsPaths[vfsFilePath] = filePath;
            }

            foreach (var subDirectoryPath in Directory.GetDirectories(pathToAdd))
            {
                var vfsFolderPath = Path.Combine(vfsPath, Path.GetFileName(subDirectoryPath));
                AddImpl(vfsFolderPath, subDirectoryPath);
            }
        }

        private void DetectFSInstallationType()
        {
            if (_settings.Source == null)
            {
                Trace.WriteLine("VFS: settings.json: No explicit VFS.Source specified");
                var storePath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe");
                var isStore = Directory.Exists(storePath);
                Trace.WriteLine("VFS: FS Installation type is detected as: " + (isStore ? "Windows Store" : "Steam"));

                _settings.Source = isStore ? _settings.Templates.WindowsStore : _settings.Templates.Steam;
            }
        }

        private void MapAndAddPackagesFromSettings()
        {
            for (var i = 0; i < 3; i++)
            {
                var source = Environment.ExpandEnvironmentVariables(_settings.Source[i]);
                var mapped = Environment.ExpandEnvironmentVariables(_settings.Mapped[i]);

                MapAndAddPackageDirectory(source, mapped);
            }

            var parentPathToSelf = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            AddPackageDirectory(Path.Combine(parentPathToSelf, "ExternalPackages"));
        }

        private void MapAndAddPackageDirectory(string source, string mapped)
        {
            Trace.WriteLine($"VFS: Mapping {source} to {mapped}");
            var cmd = $"{mapped}: /D";
            var subst = ProcessHelper.RunAndGetResult("subst", cmd).Trim();
            Trace.WriteLine($"VFS: subst {cmd} -> {subst}");

            cmd = $"{mapped}: \"{source}\"";
            subst = ProcessHelper.RunAndGetResult("subst", cmd).Trim();
            Trace.WriteLine($"VFS: subst {cmd} -> {subst}");

            AddPackageDirectory($"{mapped}:\\");
        }
    }
}
