using System;
using System.IO;
using System.Runtime.InteropServices;
using Python.Deployment;
using Python.Runtime;

namespace Sudoku.Shared
{
    public abstract class PythonSolverBase : ISolverSudoku
    {

        static PythonSolverBase()
        {
           
            
        }

        public PythonSolverBase()
        {
            if (!pythonInstalled)
            {
                InstallPythonComponents();
                pythonInstalled = true;
            }
            InitializePythonComponents();
        }

        private static bool pythonInstalled = false;

        protected static void InstallPythonComponents()
        {
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                InstallMac();
            }
            else
            {
                InstallEmbedded();
            }
        }

        protected void InstallPipModule(string moduleName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                MacInstaller.PipInstallModule(moduleName);
            }
            else
            {
                Installer.PipInstallModule(moduleName);
            }
        }

        private static void InstallMac()
        {

            Console.WriteLine($"PythonDll={MacInstaller.LibFileName}");
            Runtime.PythonDLL = MacInstaller.LibFileName;

            MacInstaller.LogMessage += Console.WriteLine;
            // Installer.SetupPython().Wait();

            //MacInstaller.InstallPath = "/Library/Frameworks/Python.framework/Versions";
            //MacInstaller.PythonDirectoryName = "3.7/";

            var localInstallPath = MacInstaller.InstallPythonHome;
            var existingPathEnvVar = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);

            var path = $"{localInstallPath}/lib;{localInstallPath};{existingPathEnvVar}";
            var pythonPath = $"{localInstallPath}/lib/site-packages;{localInstallPath}/lib";

            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            Console.WriteLine($"Path={Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process)}");

            // Environment.SetEnvironmentVariable("PYTHONHOME", localInstallPath, EnvironmentVariableTarget.Process);
            // Console.WriteLine($"PYTHONHOME={Environment.GetEnvironmentVariable("PYTHONHOME", EnvironmentVariableTarget.Process)}");
            // Environment.SetEnvironmentVariable("PythonPath", pythonPath, EnvironmentVariableTarget.Process);
            // Console.WriteLine($"PythonPath={Environment.GetEnvironmentVariable("PythonPath", EnvironmentVariableTarget.Process)}");


            var aliasPath = $"/usr/local/lib/{MacInstaller.LibFileName}";
            if (!File.Exists(aliasPath))
            {
                var libPath = $"{localInstallPath}/lib/{MacInstaller.LibFileName}";
                var aliasCommand =
                    $"sudo ln -s {libPath} {aliasPath}";
                Console.WriteLine($"run command={aliasCommand}");
                MacInstaller.RunCommand(aliasCommand);
            }
            
            
            
            var dynamicLinkingCommnad = $@"export DYLD_LIBRARY_PATH={localInstallPath}/lib"; 
            Console.WriteLine($"run command={dynamicLinkingCommnad}");
            MacInstaller.RunCommand(dynamicLinkingCommnad);

            // PythonEngine.PythonHome = localInstallPath;
            // PythonEngine.PythonPath = pythonPath;

             MacInstaller.TryInstallPip();
        }




        private static void InstallEmbedded()
        {

            Runtime.PythonDLL = "python37.dll";

            // // set the download source
            // Python.Deployment.Installer.Source = new Installer.DownloadInstallationSource()
            // {
            //     DownloadUrl = @"https://www.python.org/ftp/python/3.7.3/python-3.7.3-embed-amd64.zip",
            // };
            //
            // // install in local directory. if you don't set it will install in local app data of your user account
            // Python.Deployment.Installer.InstallPath = Path.GetFullPath(".");
            //
            // see what the installer is doing

            Installer.LogMessage += Console.WriteLine;

            //
            // install from the given source
            Python.Deployment.Installer.SetupPython().Wait();

            Installer.TryInstallPip();

        }


        protected virtual void InitializePythonComponents()
        {

           
            PythonEngine.Initialize();
            // dynamic sys = PythonEngine.ImportModule("sys");
            // Console.WriteLine("Python version: " + sys.version);
        }

        
        public abstract Shared.GridSudoku Solve(Shared.GridSudoku s);

    }

}