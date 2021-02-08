using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

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

            foreach (var path in _settings.Source)
            {
                var resolvedPath = Environment.ExpandEnvironmentVariables(path);
                bool isSuccess = false;
                try
                {
                    isSuccess = Directory.Exists(resolvedPath);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"VFS: Error acessing {path}: {ex}");
                }

                if (!isSuccess)
                {
                    MessageBox.Show($"Error: Path not accessible:\n\n'{path}'\n\n'{resolvedPath}'\n\nEdit settings.json with the correct path");
                    Environment.Exit(0);
                }

                if (isSuccess)
                {
                    if (!IsPathPackageSource(resolvedPath))
                    {
                        var warning = $"Warning: Path does not appear to be a packace source:\n\n{resolvedPath}\n\nEdit settings.json with the correct path";
                        Trace.WriteLine("VFS: " + warning);
                        MessageBox.Show(warning);
                    }
                }
            }

            MapAndAddPackagesFromSettings();
        }

        // Has at least one package and no exceptions
        private bool IsPathPackageSource(string path)
        {
            bool isValid = false;
            try
            {
                var packagesDirs = Directory.GetDirectories(path);
                foreach (var pkgDir in packagesDirs)
                {
                    isValid = isValid || File.Exists(Path.Combine(pkgDir, "layout.json"));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                isValid = false;
            }
            return isValid;
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
            foreach (var filePath in Directory.GetFiles(pathToAdd))
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

        private string SubstExec(string cmd)
        {
            var ret = ProcessHelper.RunAndGetResult("subst", cmd).Trim();
            if (string.IsNullOrWhiteSpace(ret))
            {
                ret = "OK";
            }
            return ret;
        }

        private void Subst(string cmd)
        {
            Trace.WriteLine($"VFS: 'subst {cmd}' -> {SubstExec(cmd)}");
        }

        private void MapAndAddPackageDirectory(string source, string mapped)
        {
            Trace.WriteLine($"VFS: Mapping {source} to {mapped}");

            Subst($"{mapped}: /D");

            try
            {
                if (Directory.Exists(mapped + ":"))
                {
                    MessageBox.Show($"Warning: Map target at {mapped}: already exists\n\nUpdate settings.json with unused drive letters");
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            Subst($"{mapped}: \"{source}\"");

            AddPackageDirectory($"{mapped}:\\");
        }

        public void UnmapSources()
        {
            if (_settings.SkipUnmapOnShutdown)
            {
                return;
            }
            Trace.WriteLine($"VFS: UnmapSources");

            foreach (var mapLetter in _settings.Mapped)
            {
                Subst($"{mapLetter}: /D");
            }
        }
    }
}
