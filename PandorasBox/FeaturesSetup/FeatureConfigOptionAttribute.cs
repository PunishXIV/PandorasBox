using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.FeaturesSetup
{
    public class FeatureConfigOptionAttribute : Attribute
    {

        public string Name { get; }

        public string LocalizeKey { get; }

        public int Priority { get; } = 0;
        public int EditorSize { get; set; } = -1;

        public bool SameLine { get; set; } = false;

        public bool ConditionalDisplay { get; set; } = false;

        // Int 
        public int IntMin { get; set; } = int.MinValue;
        public int IntMax { get; set; } = int.MaxValue;
        public IntEditType IntType { get; set; } = IntEditType.Slider;

        public bool EnforcedLimit { get; set; } = true;

        public delegate bool ConfigOptionEditor(string name, ref object configOption);

        public MethodInfo Editor { get; set; }

        public enum IntEditType
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

        public FeatureConfigOptionAttribute(string name, string editorType, int priority = 0, string localizeKey = null)
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
