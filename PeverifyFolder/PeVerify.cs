using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PeverifyFolder
{
    public class PeVerify
    {
        //make _peVerifyPath null if you want it to attempt autodiscover the path
        private static string _peVerifyPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\x64\peverify.exe";

        public static string PeVerifyPath
        {
            get
            {
                if (_peVerifyPath == null)
                {
                    var sdk = new DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft SDKs\Windows"));
                    if (sdk.Exists)
                    {
                        foreach (var sdkVersion in sdk.GetDirectories())
                        {
                            var peverify = Path.Combine(Path.Combine(sdkVersion.FullName, "bin"), "peverify.exe");
                            if (File.Exists(peverify))
                            {
                                _peVerifyPath = peverify;
                                break;
                            }
                        }
                    }
                    if (_peVerifyPath == null)
                    {
                        sdk = new DirectoryInfo(sdk.FullName.Replace(" (x86)", ""));
                        if (sdk.Exists)
                        {
                            foreach (var sdkVersion in sdk.GetDirectories())
                            {
                                var peverify = Path.Combine(Path.Combine(sdkVersion.FullName, "bin"), "peverify.exe");
                                if (File.Exists(peverify))
                                {
                                    _peVerifyPath = peverify;
                                    break;
                                }
                            }
                        }
                    }
                    if (_peVerifyPath == null)
                        throw new FileNotFoundException(@"could not find peverify.exe under %programfiles%\Microsoft SDKs\Windows\...");
                }

                return _peVerifyPath;
            }
        }

        public static PeVerifyResult VerifyAssembly(string assemblyName)
        {
            PeVerifyResult result = new PeVerifyResult();
            result.AssemblyName = assemblyName;

            string stdOut, stdErr;
            result.ExitCode = StartAndWaitForResult(PeVerifyPath, assemblyName + " /UNIQUE /IL /NOLOGO", out stdOut, out stdErr);
            //result.ExitCode = StartAndWaitForResult(PeVerifyPath, assemblyName + " /IL /NOLOGO", out stdOut, out stdErr);

            if (stdOut.Contains("The assembly is built by a runtime newer than the currently loaded runtime"))
            {
                Console.WriteLine("********* You should use a higher .NET version of PeVerify.exe");
                Console.ReadLine();
            }
            ParseErrors(result, stdOut);

            return result;
        }

        private static int StartAndWaitForResult(string peVerifyPath, string arguments, out string stdOut, out string stdErr)
        {
            ProcessStartInfo info = new ProcessStartInfo(peVerifyPath, arguments);
            info.UseShellExecute = false;
            info.ErrorDialog = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            using (Process p = Process.Start(info))
            {
                stdOut = p.StandardOutput.ReadToEnd();
                stdErr = p.StandardError.ReadToEnd();
                return p.ExitCode;
            }
        }

        private static void ParseErrors(PeVerifyResult result, string stdOut)
        {
            result.Errors = new List<string>();

            int startIndex = 0;
            while (startIndex < stdOut.Length)
            {
                startIndex = stdOut.IndexOf("[IL]:", startIndex);
                if (startIndex == -1) break;

                int endIndex = stdOut.IndexOf("[IL]:", startIndex + 1);
                if (endIndex == -1)
                {
                    // Look for the last line...
                    endIndex = stdOut.IndexOf("\r\n", startIndex + 1);
                }

                result.Errors.Add(result.NormalizeErrorString(stdOut.Substring(startIndex, endIndex - startIndex)));
                startIndex = endIndex;
            }
        }
    }

    public class PeVerifyResult
    {
        public int ExitCode;
        public string AssemblyName;
        public List<string> Errors;

        public string NormalizeErrorString(string error)
        {
            // Lets remove any path information.
            string path = Path.GetDirectoryName(AssemblyName);

            StringBuilder b = new StringBuilder(error);
            if (path.Length > 0)
            {
                b.Replace(path, "<path>");
            }
            b.Replace("\r\n", " ");

            return b.ToString();
        }
    }
}