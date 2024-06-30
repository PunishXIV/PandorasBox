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

        public class Config : FeatureConfig
        {
            [FeatureConfigOption("Default Value", IntMin = 1, IntMax = 99, EditorSize = 300)]
            public int Value = 1;
        }

        public override bool UseAutoConfig => true;

        public Config Configs { get; private set; }

        public override void Enable()
        {
            Configs = LoadConfig<Config>() ?? new Config();
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, ["InclusionShop", "Shop", "ShopExchangeItem", "ShopExchangeCurrency", "GrandCompanyExchange"], CheckNumerics);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ["InclusionShop", "Shop", "ShopExchangeItem", "ShopExchangeCurrency", "GrandCompanyExchange"], CheckThrottle);
            base.Enable();
        }

        private void CheckThrottle(AddonEvent type, AddonArgs args)
        {
            SetNodes.Clear();
        }

        private HashSet<string> SetNodes = new();

        private unsafe void CheckNumerics(AddonEvent type, AddonArgs args)
        {
            var addon = (AtkUnitBase*)args.Addon;
            for (var i = 0; i < addon->UldManager.NodeListCount; i++)
            {
                try
                {
                    var node = addon->UldManager.NodeList[i];
                    var compNode = (AtkComponentNode*)node;
                    var componentInfo = compNode->Component->UldManager;
                    var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;

                    if (objectInfo == null)
                        continue;

                    if (objectInfo->ComponentType is ComponentType.TreeList or ComponentType.List)
                    {
                        var tree = (AtkComponentNode*)node;

                        for (int y = 0; y < tree->Component->UldManager.NodeListCount; y++)
                        {
                            try
                            {
                                var renderNode = (AtkComponentNode*)tree->Component->UldManager.NodeList[y];

                                for (int p = 0; p < renderNode->Component->UldManager.NodeListCount; p++)
                                {
                                    var subNode = renderNode->Component->UldManager.NodeList[p];

                                    if (!subNode->IsVisible())
                                        continue;

                                    if (subNode->Type is (NodeType)1012 or (NodeType)1011)
                                    {
                                        uint NodeIdSearch = 5;
                                        if (args.AddonName == "ShopExchangeCurrency")
                                            NodeIdSearch = 3;
                                        if (args.AddonName == "ShopExchangeItem")
                                            NodeIdSearch = 7;

                                        AtkTextNode* textNode = renderNode->Component->UldManager.SearchNodeById(NodeIdSearch)->GetAsAtkTextNode();

                                        if (string.IsNullOrEmpty(textNode->NodeText.ExtractText()))
                                            continue;

                                        var uniqueVal = $"{textNode->NodeText.ExtractText()}{renderNode->AtkResNode.NodeId}";
                                        if (SetNodes.Contains(uniqueVal))
                                        {
                                            continue;
                                        }

                                        SetNodes.Add(uniqueVal);

                                        var component = (AtkComponentNode*)subNode;
                                        var numeric = (AtkComponentNumericInput*)component->Component;

                                        Svc.Log.Debug($"Setting {uniqueVal}");
                                        if (Configs.Value > 1)
                                        numeric->SetValue(Configs.Value);
                                    }

                                    if (subNode->Type is (NodeType)1007)
                                    {
                                        uint NodeIdSearch = 3;

                                        var textNode = renderNode->Component->UldManager.SearchNodeById(NodeIdSearch)->GetAsAtkTextNode();

                                        if (string.IsNullOrEmpty(textNode->NodeText.ExtractText()))
                                            continue;

                                        var uniqueVal = $"{textNode->NodeText.ExtractText()}{renderNode->AtkResNode.NodeId}";
                                        if (SetNodes.Contains(uniqueVal))
                                        {
                                            continue;
                                        }

                                        SetNodes.Add(uniqueVal);

                                        var component = (AtkComponentNode*)subNode;
                                        var numeric = (AtkComponentNumericInput*)component->Component;

                                        Svc.Log.Debug($"Setting {uniqueVal}");
                                        if (Configs.Value > 1)
                                            numeric->SetValue(Configs.Value);
                                    }

                                }

                            }
                            catch (Exception ex)
                            {
                                //ex.Log();
                            }


                        }

                    }
                }
                catch
                {

                }
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
