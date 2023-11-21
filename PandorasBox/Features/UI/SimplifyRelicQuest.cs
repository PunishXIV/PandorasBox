using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
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

        private bool NeedsToUpdate = true;
        public override void Enable()
        {
            Svc.AddonLifeCycle.RegisterListener(AddonEvent.PreUpdate, ["SelectIconString", "SelectString"], AddonSetup);
            base.Enable();
        }

        public static readonly (uint RelicQuestId, string RelicStep)[] SimpleRelics =
        {
           (69381, "Step 1"),
           (69506, "Step 2"),
           (69507, "Step 3"),
           (69574, "Step 4"),
           (69576, "Step 5"),
           (69637, "Step 6"),
           (70189, "Step 1"),
           (70262, "Step 2"),
           (70308, "Step 3")

        };

        private unsafe void AddonSetup(AddonEvent type, AddonArgs args)
        {
            if (args.AddonName == "SelectIconString")
            {
                var addon = (AddonSelectIconString*)args.Addon;

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

                    //if (NeedsToUpdate)
                    //{
                    //    var part3 = list->ItemRendererList[1];
                    //    var part2 = list->ItemRendererList[2];
                    //    list->ItemRendererList[1] = part2;
                    //    list->ItemRendererList[2] = part3;

                    //    part2.AtkComponentListItemRenderer->ListItemIndex = 1;
                    //    part3.AtkComponentListItemRenderer->ListItemIndex = 2;

                    //    var evt = part2.AtkComponentListItemRenderer->AtkComponentButton.AtkComponentBase.AtkResNode->AtkEventManager.Event;
                    //    while (evt->Type != AtkEventType.ButtonClick)
                    //        evt = evt->NextEvent;

                    //    evt->Param = 1;
                        

                    //    NeedsToUpdate = false;
                    //    return;
                    //}
                }
                catch (Exception ex)
                {

                }
            }

            if (args.AddonName == "SelectString")
            {
                var addon = (AddonSelectString*)args.Addon;

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
                var quest = Svc.Data.GetExcelSheet<Quest>().GetRow(simpleRelic.RelicQuestId);
                if (text.Contains(quest.Name.RawString))
                {
                    text = $"{simpleRelic.RelicStep}: {quest.Name}";
                    buttonTextNode->SetText(text);

                    questReplacedId = quest.RowId;

                    if (questReplacedId == 70262)
                        NeedsToUpdate = true;
                    else
                        NeedsToUpdate = false;

                    return;
                }
            }
        }

        public override void Disable()
        {
            Svc.AddonLifeCycle.UnregisterListener(AddonSetup);
            base.Disable();
        }
    }
}
