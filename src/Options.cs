﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;

namespace TypeScriptDefinitionGenerator
{
    public class OptionsDialogPage : DialogPage
    {
        internal const bool _defCamelCaseEnumerationValues = true;
        internal const bool _defCamelCasePropertyNames = true;
        internal const bool _defCamelCaseTypeNames = false;
        internal const bool _defClassInsteadOfInterface = false;
        internal const bool _defGlobalScope = false;
        internal const bool _defWebEssentials2015 = true;
        internal const bool _defOptionalByDefault = false;
        internal const bool _defEmitEnumsAsModule = false;
        internal const string _defModuleName = "server";
        internal const string _defNodeModulePath = "";

        [Category("Casing")]
        [DisplayName("Camel case enum values")]
        [DefaultValue(_defCamelCaseEnumerationValues)]
        public bool CamelCaseEnumerationValues { get; set; } = _defCamelCaseEnumerationValues;

        [Category("Casing")]
        [DisplayName("Camel case property names")]
        [DefaultValue(_defCamelCasePropertyNames)]
        public bool CamelCasePropertyNames { get; set; } = _defCamelCasePropertyNames;

        [Category("Casing")]
        [DisplayName("Camel case type names")]
        [DefaultValue(_defCamelCaseTypeNames)]
        public bool CamelCaseTypeNames { get; set; } = _defCamelCaseTypeNames;

        [Category("Settings")]
        [DisplayName("Default Module name")]
        [Description("Set the top-level module name for the generated .d.ts file. Default is \"server\"")]
        public string DefaultModuleName { get; set; } = _defModuleName;

        [Category("Settings")]
        [DisplayName("Class instead of Interface")]
        [Description("Controls whether to generate a class or an interface: default is an Interface")]
        [DefaultValue(_defClassInsteadOfInterface)]
        public bool ClassInsteadOfInterface { get; set; } = _defClassInsteadOfInterface;

        [Category("Settings")]
        [DisplayName("Make properties optional by default")]
        [Description("If checked, all properties are marked optional, unless a [Required] attribute is present")]
        [DefaultValue(_defOptionalByDefault)]
        public bool OptionalByDefault { get; set; } = _defOptionalByDefault;

        [Category("Settings")]
        [DisplayName("Generate Node modules for enums")]
        [Description("If checked, all enums are emitted as a separate .ts file to be consumed as a Node module")]
        [DefaultValue(_defEmitEnumsAsModule)]
        public bool EmitEnumsAsModule { get; set; } = _defEmitEnumsAsModule;

        [Category("Settings")]
        [DisplayName("Path (relative to project base) to place generated Node module files")]
        [Description("Node modules often need to be in a specific location to be imported.")]
        [DefaultValue(_defNodeModulePath)]
        public string NodeModulePath { get; set; } = _defNodeModulePath;

        [Category("Settings")]
        [DisplayName("Generate in global scope")]
        [Description("Controls whether to generate types in Global scope or wrapped in a module")]
        [DefaultValue(_defGlobalScope)]
        public bool GlobalScope { get; set; } = _defGlobalScope;

        [Category("Compatibilty")]
        [DisplayName("Web Esentials 2015 file names")]
        [Description("Web Essentials 2015 format is <filename>.cs.d.ts instead of <filename>.d.ts")]
        [DefaultValue(_defWebEssentials2015)]
        public bool WebEssentials2015 { get; set; } = _defWebEssentials2015;
    }

    public class Options
    {
        const string OVERRIDE_FILE_NAME = "tsdefgen.json";
        static OptionsOverride overrides { get; set; } = null;
        static public bool CamelCaseEnumerationValues
        {
            get
            {
                return overrides != null ? overrides.CamelCaseEnumerationValues : DtsPackage.Options.CamelCaseEnumerationValues;
            }
        }

        static public bool CamelCasePropertyNames
        {
            get
            {
                return overrides != null ? overrides.CamelCasePropertyNames : DtsPackage.Options.CamelCasePropertyNames;
            }
        }

        static public bool CamelCaseTypeNames
        {
            get
            {
                return overrides != null ? overrides.CamelCaseTypeNames : DtsPackage.Options.CamelCaseTypeNames;
            }
        }

        static public string DefaultModuleName
        {
            get
            {
                return overrides != null ? overrides.DefaultModuleName : DtsPackage.Options.DefaultModuleName;
            }
        }

        static public bool ClassInsteadOfInterface
        {
            get
            {
                return overrides != null ? overrides.ClassInsteadOfInterface : DtsPackage.Options.ClassInsteadOfInterface;
            }
        }

        static public bool GlobalScope
        {
            get
            {
                return overrides != null ? overrides.GlobalScope : DtsPackage.Options.GlobalScope;
            }
        }

        static public bool WebEssentials2015
        {
            get
            {
                return overrides != null ? overrides.WebEssentials2015 : DtsPackage.Options.WebEssentials2015;
            }
        }

        static public bool OptionalByDefault
        {
            get
            {
                return overrides != null ? overrides.OptionalByDefault : DtsPackage.Options.OptionalByDefault;
            }
        }

        static public bool EmitEnumsAsModule
        {
            get
            {
                return overrides != null ? overrides.EmitEnumsAsModule : DtsPackage.Options.EmitEnumsAsModule;
            }
        }

        static public string NodeModulePath => overrides?.NodeModulePath ?? DtsPackage.Options.NodeModulePath;

        public static void ReadOptionOverrides(ProjectItem sourceItem, bool display = true)
        {
            Project proj = sourceItem.ContainingProject;

            string jsonName = "";

            foreach (ProjectItem item in proj.ProjectItems)
            {
                if (item.Name.ToLower() == OVERRIDE_FILE_NAME.ToLower())
                {
                    jsonName = item.FileNames[0];
                    break;
                }
            }

            if (!string.IsNullOrEmpty(jsonName))
            {
                // it has been modified since last read - so read again
                try
                {
                    overrides = JsonConvert.DeserializeObject<OptionsOverride>(File.ReadAllText(jsonName));
                    if (display)
                    {
                        VSHelpers.WriteOnOutputWindow(string.Format("Override file processed: {0}", jsonName));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Override file processed: {0}", jsonName));
                    }
                }
                catch (Exception e) when (e is Newtonsoft.Json.JsonReaderException || e is Newtonsoft.Json.JsonSerializationException)
                {
                    overrides = null; // incase the read fails
                    VSHelpers.WriteOnOutputWindow(string.Format("Error in Override file: {0}", jsonName));
                    VSHelpers.WriteOnOutputWindow(e.Message);
                    throw;
                }
            }
            else
            {
                if (display)
                {
                    VSHelpers.WriteOnOutputWindow("Using Global Settings");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Using Global Settings");
                }
                overrides = null;
            }
        }

    }

    internal class OptionsOverride
    {
        //        [JsonRequired]
        public bool CamelCaseEnumerationValues { get; set; } = OptionsDialogPage._defCamelCaseEnumerationValues;

        //        [JsonRequired]
        public bool CamelCasePropertyNames { get; set; } = OptionsDialogPage._defCamelCasePropertyNames;

        //        [JsonRequired]
        public bool CamelCaseTypeNames { get; set; } = OptionsDialogPage._defCamelCaseTypeNames;

        //        [JsonRequired]
        public string DefaultModuleName { get; set; } = OptionsDialogPage._defModuleName;

        //        [JsonRequired]
        public bool ClassInsteadOfInterface { get; set; } = OptionsDialogPage._defClassInsteadOfInterface;

        //        [JsonRequired]
        public bool GlobalScope { get; set; } = OptionsDialogPage._defGlobalScope;

        //        [JsonRequired]
        public bool WebEssentials2015 { get; set; } = OptionsDialogPage._defWebEssentials2015;

        public bool OptionalByDefault { get; set; } = OptionsDialogPage._defOptionalByDefault;

        public bool EmitEnumsAsModule { get; set; } = OptionsDialogPage._defEmitEnumsAsModule;

        public string NodeModulePath { get; set; } = OptionsDialogPage._defNodeModulePath;
    }

}
