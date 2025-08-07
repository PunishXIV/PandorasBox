using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;

namespace PandorasBox.Features.UI
{
    internal class SimplifyRelicQuest : Feature
    {
        public override string Name { get; } = "Simplify Relic Quest Pickup";
        public override string Description { get; } = "Adds a description of what stage a quest is for when talking to the NPC.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PreUpdate, ["SelectIconString", "SelectString"], AddonSetup);
            base.Enable();
        }

        public static readonly (uint[] RelicQuestId, string RelicStep)[] SimpleRelics =
        {
           ([69381, 70189, 70267, 69429, 67748], "Step 1"),
           ([69506, 70262, 70268, 69430, 67749], "Step 2"),
           ([69507, 70308, 70304, 69519, 67750], "Step 3"),
           ([69574, 70305, 67820, 70343], "Step 4"),
           ([69576, 70339, 67864], "Step 5"),
           ([69637, 70340, 67915], "Step 6"),
           ([67932], "Step 7"),
           ([67940], "Step 8"),
           ([66655], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(22)!.Name.ToString()),
           ([66656], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(20)!.Name.ToString()),
           ([66657], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(21)!.Name.ToString()),
           ([66658], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(23)!.Name.ToString()),
           ([66659], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(26)!.Name.ToString()),
           ([66660], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(25)!.Name.ToString()),
           ([66661], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(24)!.Name.ToString()),
           ([66662], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(28)!.Name.ToString()),
           ([66663], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(29)!.Name.ToString()),
           ([67115], Svc.Data.GetExcelSheet<ClassJobCategory>()!.GetRow(92)!.Name.ToString()),
        };

        private unsafe void AddonSetup(AddonEvent type, AddonArgs args)
        {
            if (args.AddonName == "SelectIconString")
            {
                var addon = (AddonSelectIconString*)args.Addon.Address;

                var list = addon->PopupMenu.PopupMenu.List;

                try
                {
                    foreach (var index in Enumerable.Range(0, list->ListLength))
                    {
                        var listItemRenderer = list->ItemRendererList[index].AtkComponentListItemRenderer;
                        if (listItemRenderer is null) continue;

                        var buttonTextNode = listItemRenderer->AtkComponentButton.ButtonTextNode;
                        if (buttonTextNode is null) continue;

                        UpdateAddonText(buttonTextNode, out var questReplacedId);
                    }

                }
                catch (Exception ex)
                {

                }
            }

            if (args.AddonName == "SelectString")
            {
                var addon = (AddonSelectString*)args.Addon.Address;

                var list = addon->PopupMenu.PopupMenu.List;

                try
                {
                    foreach (var index in Enumerable.Range(0, list->ListLength))
                    {
                        var listItemRenderer = list->ItemRendererList[index].AtkComponentListItemRenderer;
                        if (listItemRenderer is null) continue;

                        var buttonTextNode = listItemRenderer->AtkComponentButton.ButtonTextNode;
                        if (buttonTextNode is null) continue;

                        UpdateAddonText(buttonTextNode, out var questReplacedId);
                    }
                }
                catch (Exception ex)
                {

                }
            }

        }

        private unsafe void UpdateAddonText(AtkTextNode* buttonTextNode, out uint questReplacedId)
        {
            var text = buttonTextNode->NodeText.ToString();
            questReplacedId = 0;

            if (text.Contains("Step"))
            {
                return;
            }

            foreach (var simpleRelic in SimpleRelics)
            {
                foreach (var questID in simpleRelic.RelicQuestId)
                {
                    var quest = Svc.Data.GetExcelSheet<Quest>().GetRow(questID);
                    if (text.Contains(quest.Name.ToString()))
                    {
                        text = $"{simpleRelic.RelicStep}: {quest.Name}";
                        buttonTextNode->SetText(text);

                        questReplacedId = quest.RowId;
                        return;
                    }
                }
            }
        }

        public override void Disable()
        {
            Svc.AddonLifecycle.UnregisterListener(AddonSetup);
            base.Disable();
        }
    }
}
