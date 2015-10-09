using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeverifyFolder
{
    internal class Program
    {
        private static string binDirectoryPath = @"C:\Users\benjamin_c_adams\Downloads\Api\Api\bin";
        private static string csvResultsPath = @"results.txt";

        public static void Main(string[] args)
        {
            PeVerify.VerifyAssembly(@"C:\Users\benjamin_c_adams\Downloads\Api\Api\bin\Swashbuckle.Core.dll");
            var output = new StringBuilder();
            ProcessDirectory(binDirectoryPath, output);

            File.WriteAllText(csvResultsPath, output.ToString());
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory, StringBuilder output)
        {
            if (!Directory.Exists(targetDirectory))
            {
                return;
            }

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, output);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, output);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path, StringBuilder output)
        {
            output.AppendLine(string.Format("File: {0}", path));
            try
            {
                var result = PeVerify.VerifyAssembly(path);
                if (result.Errors.Count > 0)
                {
                    listToString(result.Errors, output);
                }

                output.AppendLine(string.Format("Errors: {0}", result.Errors.Count));

                Console.WriteLine("Processed file '{0}'.", path);
            }
            catch
            {
                output.AppendLine("Failed checking file");
                Console.WriteLine("FAILED: file '{0}'.", path);
            }
        }

        public static void listToString(List<string> lst, StringBuilder output)
        {
            foreach (var str in lst)
            {
                output.AppendLine(str);
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

    public class PeVerify
    {
        private const int PeVerifyExpectedExitCode = 0;

        private static string _peVerify;

        public static string PeVerifyPath
        {
            get
            {
                if (_peVerify == null)
                {
                    var sdk =
                        new DirectoryInfo(
                            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft SDKs\Windows"));
                    if (sdk.Exists)
                    {
                        foreach (var sdkVersion in sdk.GetDirectories())
                        {
                            var peverify = Path.Combine(Path.Combine(sdkVersion.FullName, "bin"), "peverify.exe");
                            if (File.Exists(peverify))
                            {
                                _peVerify = peverify;
                                break;
                            }
                        }
                    }
                    if (_peVerify == null)
                    {
                        sdk = new DirectoryInfo(sdk.FullName.Replace(" (x86)", ""));
                        if (sdk.Exists)
                        {
                            foreach (var sdkVersion in sdk.GetDirectories())
                            {
                                var peverify = Path.Combine(Path.Combine(sdkVersion.FullName, "bin"), "peverify.exe");
                                if (File.Exists(peverify))
                                {
                                    _peVerify = peverify;
                                    break;
                                }
                            }
                        }
                    }
                    if (_peVerify == null)
                        throw new FileNotFoundException(@"could not find peverify.exe under %programfiles%\Microsoft SDKs\Windows\...");
                }

                return _peVerify;
            }
        }

        public static PeVerifyResult VerifyAssembly(string assemblyName)
        {
            PeVerifyResult result = new PeVerifyResult();
            result.AssemblyName = assemblyName;

            string stdOut, stdErr;
            //result.ExitCode = StartAndWaitForResult(PeVerifyPath, assemblyName + " /UNIQUE /IL /NOLOGO", out stdOut, out stdErr);
            result.ExitCode = StartAndWaitForResult(PeVerifyPath, assemblyName, out stdOut, out stdErr);
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
}