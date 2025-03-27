using System;
using System.Reflection;

namespace PandorasBox.FeaturesSetup
{
    [AttributeUsage(AttributeTargets.All)]
    public class FeatureConfigOptionAttribute : Attribute
    {

        public string Name { get; }

        public string? LocalizeKey { get; }

        public bool Disabled { get; set; } = false;

        public int Priority { get; } = 0;
        public int EditorSize { get; set; } = -1;

        public bool SameLine { get; set; } = false;

        public bool ConditionalDisplay { get; set; } = false;

        public int IntIncrements = 1;

        public float FloatIncrements = 0.1f;

        public string Format = $"%.1f";
        // Int 
        public int IntMin { get; set; } = int.MinValue;
        public int IntMax { get; set; } = int.MaxValue;
        public NumberEditType IntType { get; set; } = NumberEditType.Slider;

        // Float
        public float FloatMin { get; set; } = float.MinValue;
        public float FloatMax { get; set; } = int.MaxValue;
        public NumberEditType FloatType { get; set; } = NumberEditType.Slider;


        public bool EnforcedLimit { get; set; } = true;

        public delegate bool ConfigOptionEditor(string name, ref object configOption);

        public MethodInfo? Editor { get; set; }

        public enum NumberEditType
        {
            Slider,
            Drag,
        }

        //List
        public uint SelectedValue { get; set; } = 0;

        public FeatureConfigOptionAttribute(string name)
        {
            Name = name;
        }

        public FeatureConfigOptionAttribute(string name, string editorType, int priority = 0, string? localizeKey = null)
        {
            Name = name;
            Priority = priority;
            LocalizeKey = localizeKey ?? name;
            Editor = typeof(FeatureConfigEditor).GetMethod($"{editorType}Editor", BindingFlags.Public | BindingFlags.Static);
        }

        public FeatureConfigOptionAttribute(string name, uint selectedValue = 0)
        {
            Name = name;
            SelectedValue = selectedValue;
        }

    }
}
