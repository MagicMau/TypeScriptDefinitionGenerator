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

        string originalExt { get; set; }

        public override string GetDefaultExtension()
        {
            if (Options.WebEssentials2015)
            {
                return this.originalExt + Constants.FileExtension;
            }
            else
            {
                return Constants.FileExtension;
            }
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            ProjectItem item = Dte.Solution.FindProjectItem(inputFileName);
            originalExt = Path.GetExtension(inputFileName);
            if (item != null)
            {
                try
                {
                    var output = GenerationService.ConvertToTypeScript(item);
                    string dts = output.Item1;

                    Telemetry.TrackOperation("FileGenerated");

                    if (Options.EmitEnumsAsModule)
                    {
                        string nodeModule = output.Item2;
                        if (!string.IsNullOrEmpty(nodeModule))
                        {
                            string nodeModuleFile = Path.ChangeExtension(inputFileName, ".ts");
                            if (!string.IsNullOrEmpty(Options.NodeModulePath))
                            {
                                string projectPath = Path.GetDirectoryName(item.ContainingProject.FileName);
                                nodeModuleFile = nodeModuleFile.Substring(projectPath.Length + 1); // strip the initial part of the path
                                nodeModuleFile = Path.Combine(projectPath, Options.NodeModulePath, nodeModuleFile);
                                Directory.CreateDirectory(Path.GetDirectoryName(nodeModuleFile));
                            }
                            VSHelpers.CheckFileOutOfSourceControl(nodeModuleFile);
                            File.WriteAllText(nodeModuleFile, nodeModule);
                        }
                    }

                    return Encoding.UTF8.GetBytes(dts);
                }
                catch (Exception ex)
                {
                    Telemetry.TrackOperation("FileGenerated", Microsoft.VisualStudio.Telemetry.TelemetryResult.Failure);
                    Telemetry.TrackException("FileGenerated", ex);
                }
            }

            return new byte[0];
        }
    }
}
