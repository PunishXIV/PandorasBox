using Dalamud.Interface;
using ECommons.ImGuiMethods;
using ECommons.ImGuiMethods.TerritorySelection;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;

namespace PandorasBox.FeaturesSetup
{
    public static class FeatureConfigEditor
    {
        public static bool ColorEditor(string name, ref object configOption)
        {
            switch (configOption)
            {
                case Vector4 v4 when ImGui.ColorEdit4(name, ref v4):
                    configOption = v4;
                    return true;
                case Vector3 v3 when ImGui.ColorEdit3(name, ref v3):
                    configOption = v3;
                    return true;
                default:
                    return false;
            }
        }

        public static bool SimpleColorEditor(string name, ref object configOption)
        {
            switch (configOption)
            {
                case Vector4 v4 when ImGui.ColorEdit4(name, ref v4, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar):
                    configOption = v4;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TerritorySelectionEditor(string name, ref object configOption)
        {
            if (configOption is List<uint> territories)
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.List))
                {
                    var x = new TerritorySelector(territories, (terr, selectedTerritories) =>
                    {
                        territories.Clear();
                        territories.AddRange(selectedTerritories);
                    })
                    {
                        SelectedCategory = TerritorySelector.Category.All,
                        ExtraColumns = [TerritorySelector.Column.ID, TerritorySelector.Column.IntendedUse],
                    };
                    return true;
                }
                ImGui.SameLine();
                ImGui.TextUnformatted($"Zone Whitelist ({territories.Count} territories selected)");
            }
            return false;
        }
    }
}
