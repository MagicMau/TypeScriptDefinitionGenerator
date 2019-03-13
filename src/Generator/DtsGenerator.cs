using EnvDTE;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TypeScriptDefinitionGenerator
{
    [Guid("d1e92907-20ee-4b6f-ba64-142297def4e4")]
    public sealed class DtsGenerator : BaseCodeGeneratorWithSite
    {
        public const string Name = nameof(DtsGenerator);
        public const string Description = "Automatically generates the .d.ts file based on the C#/VB model class.";

        private string OriginalExt { get; set; }

        public override string GetDefaultExtension()
        {
            if (Options.WebEssentials2015)
                return OriginalExt + Constants.FileExtension;

            return Constants.FileExtension;
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            ProjectItem item = Dte.Solution.FindProjectItem(inputFileName);
            OriginalExt = Path.GetExtension(inputFileName);
            if (item != null)
            {
                try
                {
                    Telemetry.TrackOperation("FileGenerated");

                    string output = GenerationService.ConvertToTypeScript(item);

                    if (!string.IsNullOrWhiteSpace(Options.NodeModulePath))
                    {
                        // generate a Node module instead of a d.ts file.
                        string outputFile = Path.ChangeExtension(inputFileName, ".ts");
                        string projectPath = Path.GetDirectoryName(item.ContainingProject.FileName);
                        outputFile = outputFile.Substring(projectPath.Length + 1); // strip the initial part of the path
                        outputFile = Path.Combine(projectPath, Options.NodeModulePath, outputFile);
                        var di = Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                        if (di != null && di.Exists)
                        {
                            VSHelpers.CheckFileOutOfSourceControl(outputFile);
                            File.WriteAllText(outputFile, output);

                            output = $"// Node module file generated at {MakeRelativePath(InputFilePath, outputFile)}";
                        }
                    }

                    return Encoding.UTF8.GetBytes(output);
                }
                catch (Exception ex)
                {
                    VSHelpers.WriteOnOutputWindow(string.Format("{0} - File Generation Failure", inputFileName));
                    VSHelpers.WriteOnOutputWindow(ex.StackTrace);
                    Telemetry.TrackOperation("FileGenerated", Microsoft.VisualStudio.Telemetry.TelemetryResult.Failure);
                    Telemetry.TrackException("FileGenerated", ex);
                }
            }

            return new byte[0];
        }


        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// Copied from https://stackoverflow.com/a/340454/3131828
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
