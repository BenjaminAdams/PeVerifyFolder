using System;
using System.Collections.Generic;
using System.IO;

using System.Text;

namespace PeverifyFolder
{
    internal class Program
    {
        private static string _binDirectoryPath = @"C:\Users\benjamin_c_adams\Downloads\Api\Api\bin";
        private static string csvResultsPath = @"results.txt";

        public static void Main(string[] args)
        {
            var output = new StringBuilder();
            ProcessDirectory(_binDirectoryPath, output);

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
            {
                ProcessFile(fileName, output);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory, output);
            }
        }

        // Process an individual file
        public static void ProcessFile(string path, StringBuilder output)
        {
            if (!path.EndsWith(".dll") && !path.EndsWith(".exe")) return;

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

            output.AppendLine();
        }

        public static void listToString(List<string> lst, StringBuilder output)
        {
            foreach (var str in lst)
            {
                output.AppendLine(str);
            }
        }
    }
}