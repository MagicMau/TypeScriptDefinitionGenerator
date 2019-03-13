using System.Diagnostics.CodeAnalysis;

namespace TypeScriptDefinitionGenerator
{
    public class IntellisenseProperty
    {
        public IntellisenseProperty()
        {

        }
        public IntellisenseProperty(IntellisenseType type, string propertyName)
        {
            Type = type;
            Name = propertyName;
        }

        public string Name { get; set; }

        public string NameWithOption
        {
            get
            {
                if (Type != null && Type.IsOptional)
                    return Name + "?";
                return Name;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
            Justification = "Unambiguous in this context.")]
        public IntellisenseType Type { get; set; }

        public string Summary { get; set; }
        public string InitExpression { get; set; }
        public bool IsRequired { get; set; }
    }
}