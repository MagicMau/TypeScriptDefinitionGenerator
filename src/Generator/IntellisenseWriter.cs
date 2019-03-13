using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TypeScriptDefinitionGenerator.Helpers;

namespace TypeScriptDefinitionGenerator
{
    internal static class IntellisenseWriter
    {
        private static readonly Regex _whitespaceTrimmer = new Regex(@"^\s+|\s+$|\s*[\r\n]+\s*", RegexOptions.Compiled);

        public static string WriteTypeScript(IEnumerable<IntellisenseObject> objects)
        {
            var sb = new StringBuilder();
            var imports = new HashSet<string>();
            var locallyDefined = new HashSet<string>();
            bool isNodeModule = !string.IsNullOrWhiteSpace(Options.NodeModulePath);

            string indent = "";

            foreach (var ns in objects.GroupBy(o => o.Namespace))
            {
                if (!Options.GlobalScope && !isNodeModule)
                {
                    sb.AppendFormat("declare module {0} {{\r\n", ns.Key);
                    indent = "    ";
                }

                foreach (IntellisenseObject io in ns)
                {
                    if (!string.IsNullOrEmpty(io.Summary))
                        sb.AppendLine(indent + "/** " + _whitespaceTrimmer.Replace(io.Summary, "") + " */");

                    string name = Utility.CamelCaseClassName(io.Name);
                    locallyDefined.Add(name);

                    if (io.IsEnum)
                    {
                        sb.AppendLine(indent + (isNodeModule ? "export " : "") + "const enum " + name + " {");

                        string ind = indent + "    ";
                        foreach (var p in io.Properties)
                        {
                            WriteTypeScriptComment(p, sb);

                            if (p.InitExpression != null)
                            {
                                sb.AppendLine(ind + Utility.CamelCaseEnumValue(p.Name) + " = " + CleanEnumInitValue(p.InitExpression) + ",");
                            }
                            else
                            {
                                sb.AppendLine(ind + Utility.CamelCaseEnumValue(p.Name) + ",");
                            }
                        }

                        sb.AppendLine(indent + "}");
                    }
                    else
                    {
                        string pre = indent + (isNodeModule ? "export " : "");
                        string type = Options.ClassInsteadOfInterface ? pre + "class " : pre + "interface ";
                        
                        sb.Append(type).Append(name).Append(" ");

                        if (!string.IsNullOrEmpty(io.BaseName))
                        {
                            sb.Append("extends ");

                            if (!isNodeModule && !string.IsNullOrEmpty(io.BaseNamespace) && io.BaseNamespace != io.Namespace)
                                sb.Append(io.BaseNamespace).Append(".");

                            sb.Append(Utility.CamelCaseClassName(io.BaseName)).Append(" ");
                        }

                        WriteTSInterfaceDefinition(sb, imports, locallyDefined, indent + "", io.Properties);
                        sb.AppendLine();
                    }
                }

                if (!Options.GlobalScope && !isNodeModule)
                {
                    sb.AppendLine("}");
                }
            }

            var sbImports = new StringBuilder();
            foreach (var import in imports.OrderBy(x => x))
            {
                sbImports.AppendLine(import);
            }

            if (sbImports.Length > 0)
                sbImports.AppendLine();

            return sbImports.ToString() + sb.ToString();
        }

        private static string CleanEnumInitValue(string value)
        {
            value = value.TrimEnd('u', 'U', 'l', 'L'); //uint ulong long
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return value;
            var trimedValue = value.TrimStart('0'); // prevent numbers to be parsed as octal in js.
            if (trimedValue.Length > 0) return trimedValue;
            return "0";
        }


        private static void WriteTypeScriptComment(IntellisenseProperty p, StringBuilder sb)
        {
            if (string.IsNullOrEmpty(p.Summary)) return;
            sb.AppendLine("        /** " + _whitespaceTrimmer.Replace(p.Summary, "") + " */");
        }

        private static void WriteTSInterfaceDefinition(StringBuilder sb, HashSet<string> imports, HashSet<string> locallyDefined, 
            string prefix, IEnumerable<IntellisenseProperty> props)
        {
            sb.AppendLine("{");

            foreach (var p in props)
            {
                WriteTypeScriptComment(p, sb);
                sb.AppendFormat("{0}    {1}: ", prefix, Utility.CamelCasePropertyName(p.NameWithOption));

                if (p.Type.IsKnownType)
                {
                    string typeScriptName = p.Type.TypeScriptName;
                    sb.Append(typeScriptName);
                    if (!string.IsNullOrWhiteSpace(Options.NodeModulePath) && !p.Type.IsSimpleType && !locallyDefined.Contains(typeScriptName))
                    {
                        imports.Add($"import {{ {typeScriptName} }} from './{typeScriptName}';");
                    }
                }
                else
                {
                    if (p.Type.Shape == null) sb.Append("any");
                    else WriteTSInterfaceDefinition(sb, imports, locallyDefined, prefix + "    ", p.Type.Shape);
                }
                if (p.Type.IsArray) sb.Append("[]");

                sb.AppendLine(";");
            }

            sb.Append(prefix).Append("}");
        }
    }
}
