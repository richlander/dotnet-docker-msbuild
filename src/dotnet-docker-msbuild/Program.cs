using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Console;

namespace dotnet_docker_msbuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var artifacts = new List<string>();

            var command = new ProcessStartInfo("dotnet", "build /pp");
            command.RedirectStandardOutput = true;
            var proc = Process.Start(command);

            var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
            var sharedIndex = runtimeDir.IndexOf("dotnet");
            var runtimeinstallRoot = runtimeDir.Substring(0, sharedIndex + 6);

            var currentDir = Directory.GetCurrentDirectory();
            var currentDirLength = currentDir.Length + 1;

            var bin = Path.Combine(currentDir, "bin");
            var obj = Path.Combine(currentDir, "obj");

            var stream = proc.StandardOutput;
            string line = null;

            while ((line = stream.ReadLine()) != null)
            {
                // validate line has content
                if (line != null && line.Length > 1)
                {

                    // skip comment lines
                    if (line.StartsWith("//")) { continue; }

                    // validate that the line is path-like
                    if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && line[1] == ':') |
                        (line[0] == '/'))
                    {
                        // skip SDK artifacts
                        if (!line.StartsWith(runtimeinstallRoot))
                        {
                            // skip generated assets
                            if (line.StartsWith(bin) || line.StartsWith(obj)) { continue; }

                            var artifact = string.Empty;

                            if (line.StartsWith(currentDir))
                            {
                                artifact = line.Substring(currentDir.Length + 1);
                            }
                            else
                            {
                                artifact = line;
                            }

                            if (!artifacts.Contains(artifact))
                            {
                                artifacts.Add(artifact);
                            }
                        }
                    }
                }
            }

            if (artifacts.Count > 0)
            {
                WriteLine();
                WriteLine("Add the following lines to your Dockerfile (full paths suggest incorrect Dockerfile root)");
                WriteLine();

                foreach (var artifact in artifacts)
                {
                    WriteLine($"COPY {artifact} .");
                }
            }
            else
            {
                WriteLine("No msbuild artifacts found.");
            }
        }
    }
}
