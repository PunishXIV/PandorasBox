using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;

namespace PandorasBox.Features.UI
{
    internal class DefaultVendorValue : Feature
    {
        public override string Name { get; } = "Default Vendor Buy Amount";
        public override string Description { get; } = "Sets the default amount for items to buy from vendors";

        public override FeatureType FeatureType => FeatureType.UI;

        public override bool FeatureDisabled => true;

        public override string DisabledReason => "Crashing.";
        public class Config : FeatureConfig
        {
            [FeatureConfigOption("Default Value", IntMin = 1, IntMax = 99, EditorSize = 300)]
            public int Value = 1;
        }

        public override bool UseAutoConfig => true;

        public Config Configs { get; private set; } = null!;

        public override void Enable()
        {
            Configs = LoadConfig<Config>() ?? new Config();
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, ["InclusionShop", "Shop", "ShopExchangeItem", "ShopExchangeCurrency", "GrandCompanyExchange"], CheckNumerics);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, ["InclusionShop", "Shop", "ShopExchangeItem", "ShopExchangeCurrency", "GrandCompanyExchange"], CheckThrottle);
            base.Enable();
        }

        private void CheckThrottle(AddonEvent type, AddonArgs args)
        {
            setNodes.Clear();
        }

        private HashSet<string> setNodes = [];

        private unsafe void CheckNumerics(AddonEvent type, AddonArgs args)
        {
            try
            {
                var addon = (AtkUnitBase*)args.Addon.Address;
                if (addon == null)
                    return;

                for (var i = 0; i < addon->UldManager.NodeListCount; i++)
                {
                    try
                    {
                        var node = addon->UldManager.NodeList[i];
                        if (node == null)
                            continue;

                        var compNode = node->GetAsAtkComponentNode();
                        if (compNode is null || compNode->Component is null)
                            continue;

                        var comp = compNode->Component;
                        if (comp is null)
                            continue;

                        var componentInfo = comp->UldManager;
                        if (componentInfo.Objects is null)
                            continue;

                        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
                        if (objectInfo == null)
                            continue;

                        try
                        {
                            ComponentType? t = objectInfo->ComponentType;

                            if (t is null)
                                continue;

                            if (t is ComponentType.TreeList or ComponentType.List)
                            {
                                for (int y = 0; y < compNode->Component->UldManager.NodeListCount; y++)
                                {
                                    var renderNode = (AtkComponentNode*)compNode->Component->UldManager.NodeList[y];
                                    if (renderNode is null || renderNode->Component is null)
                                        continue;

                                    for (int p = 0; p < renderNode->Component->UldManager.NodeListCount; p++)
                                    {
                                        var subNode = renderNode->Component->UldManager.NodeList[p];

                                        if (subNode is null || !subNode->IsVisible())
                                            continue;

                                        NodeType? t2 = subNode->Type;
                                        if (t2 is null)
                                            continue;

                                        if (t2 is (NodeType)1012 or (NodeType)1011)
                                        {
                                            uint NodeIdSearch = 5;
                                            if (args.AddonName == "ShopExchangeCurrency")
                                                NodeIdSearch = 3;
                                            if (args.AddonName == "ShopExchangeItem")
                                                NodeIdSearch = 7;

                                            AtkTextNode* textNode = renderNode->Component->UldManager.SearchNodeById(NodeIdSearch)->GetAsAtkTextNode();

                                            if (textNode is null || string.IsNullOrEmpty(textNode->NodeText.GetText()))
                                                continue;

                                            var uniqueVal = $"{textNode->NodeText.GetText()}{renderNode->AtkResNode.NodeId}";
                                            if (setNodes.Contains(uniqueVal))
                                            {
                                                continue;
                                            }

                                            setNodes.Add(uniqueVal);

                                            var component = (AtkComponentNode*)subNode;
                                            var numeric = (AtkComponentNumericInput*)component->Component;

                                            if (component is null || numeric is null)
                                                continue;

                                            Svc.Log.Debug($"Setting {uniqueVal}");
                                            if (Configs.Value > 1)
                                                numeric->SetValue(Configs.Value);
                                        }

                                        if (t2 is (NodeType)1007)
                                        {
                                            uint NodeIdSearch = 3;

                                            var textNode = renderNode->Component->UldManager.SearchNodeById(NodeIdSearch)->GetAsAtkTextNode();

                                            if (textNode is null || string.IsNullOrEmpty(textNode->NodeText.GetText()))
                                                continue;

                                            var uniqueVal = $"{textNode->NodeText.GetText()}{renderNode->AtkResNode.NodeId}";
                                            if (setNodes.Contains(uniqueVal))
                                            {
                                                continue;
                                            }

                                            setNodes.Add(uniqueVal);

                                            var component = (AtkComponentNode*)subNode;
                                            var numeric = (AtkComponentNumericInput*)component->Component;

                                            if (component is null || numeric is null)
                                                continue;

                                            Svc.Log.Debug($"Setting {uniqueVal}");
                                            if (Configs.Value > 1)
                                                numeric->SetValue(Configs.Value);
                                        }

                                    }

                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
            }
            catch(Exception ex)
            {
                ex.Log();
            }
        }

        public override void Disable()
        {
            SaveConfig(Configs);
            Svc.AddonLifecycle.UnregisterListener(CheckNumerics);
            Svc.AddonLifecycle.UnregisterListener(CheckThrottle);
            base.Disable();
        }

    }
}
