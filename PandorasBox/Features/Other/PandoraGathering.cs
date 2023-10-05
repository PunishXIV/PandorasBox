using ClickLib.Bases;
using ClickLib.Structures;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace PandorasBox.Features.Other
{
    public unsafe class PandoraGathering : Feature
    {

        public static readonly (uint ItemId, uint SeedId)[] Seeds =
        {
            (4785, 7715), // Paprika          
            (4777, 7716), // Wild Onion       
            (4778, 7717), // Coerthan Carrot  
            (4782, 7718), // La Noscean Lettuce
            (4804, 7719), // Cinderfoot Olive 
            (4787, 7720), // Popoto           
            (4821, 7721), // Millioncorn      
            (4788, 7722), // Wizard Eggplant  
            (4789, 7723), // Midland Cabbage  
            (4809, 7725), // La Noscean Orange
            (4808, 7726), // Lowland Grapes   
            (4810, 7727), // Faerie Apple     
            (4811, 7728), // Sun Lemon        
            (4812, 7729), // Pixie Plums      
            (4814, 7730), // Blood Currants   
            (6146, 7731), // Mirror Apple     
            (4815, 7732), // Rolanberry       
            (4829, 7735), // Garlean Garlic   
            (5539, 7736), // Lavender         
            (4830, 7737), // Black Pepper     
            (4835, 7738), // Ala Mhigan Mustard
            (4836, 7739), // Pearl Ginger     
            (5542, 7740), // Chamomile        
            (5346, 7741), // Flax         
            (4837, 7742), // Midland Basil
            (5543, 7743), // Mandrake     
            (4842, 7744), // Almonds

            (29669, 29670), // Oddly Specific Latex                 
            (29671, 29672), // Oddly Specific Obsidian              
            (29674, 29675), // Oddly Specific Amber                 
            (29676, 29677), // Oddly Specific Dark Matter           
            (31125, 31126), // Oddly Specific Leafborne Aethersand  
            (31130, 31131), // Oddly Specific Primordial Resin      
            (31127, 31128), // Oddly Specific Landborne Aethersand  
            (31132, 31133), // Oddly Specific Primordial Asphaltum  

            (38788, 38789), // Splendorous Earth Shard
            (38790, 38791), // Splendorous Water Shard
            (38794, 38795), // Splendorous Lightning Shard
            (38796, 38797), // Splendorous Fire Shard

            (39805, 39806), // Custom Ice Crystal
            (39807, 39808), // Custom Wind Crystal
            (39811, 39812), // Brilliant Lightning Cluster
            (39813, 39814), // Brilliant Earth Cluster
        };

        public static readonly (uint ItemId, uint NodeId)[] Items =
        {
            (7758, 203),  // Grade 1 La Noscean Topsoil
            (7761, 200),  // Grade 1 Shroud Topsoil   
            (7764, 201),  // Grade 1 Thanalan Topsoil 
            (7759, 150),  // Grade 2 La Noscean Topsoil
            (7762, 209),  // Grade 2 Shroud Topsoil   
            (7765, 151),  // Grade 2 Thanalan Topsoil 
            (10092, 210), // Black Limestone          
            (10094, 177), // Little Worm              
            (10097, 133), // Yafaemi Wildgrass        
            (12893, 295), // Dark Chestnut            
            (15865, 30),  // Firelight Seeds          
            (15866, 39),  // Icelight Seeds           
            (15867, 21),  // Windlight Seeds          
            (15868, 31),  // Earthlight Seeds         
            (15869, 25),  // Levinlight Seeds         
            (15870, 14),  // Waterlight Seeds
            (12534, 285), // Mythrite Ore             
            (12535, 353), // Hardsilver Ore           
            (12537, 286), // Titanium Ore             
            (12579, 356), // Birch Log                
            (12878, 297), // Cyclops Onion            
            (12879, 298), // Emerald Beans            
        };

        public static readonly (uint MapId, uint[] NodeIds)[] Maps =
        {
            (6688,  new uint[]{20, 49, 137, 140, 141, 180}),                                 // Leather
            (6689,  new uint[]{46, 142, 143, 185, 186}),                                     // Goatskin
            (6690,  new uint[]{198, 294, 197, 147, 199, 149, 189, 284, 210, 209, 150, 151}), // Toadskin
            (6691,  new uint[]{198, 294, 197, 147, 199, 149, 189, 284, 210, 209, 150, 151}), // Boarskin
            (6692,  new uint[]{198, 294, 197, 147, 199, 149, 189, 284, 210, 209, 150, 151}), // Peisteskin
            (12241, new uint[]{295, 287, 297, 286, 298, 296, 288, 285}),                     // Archaeoskin
            (12242, new uint[]{391, 356, 354, 358, 352, 359, 361, 360, 300, 351, 353, 355}), // Wyvernskin
            (12243, new uint[]{391, 356, 354, 358, 352, 359, 361, 360, 300, 351, 353, 355}), // Dragonskin
            (17835, new uint[]{514, 513, 517, 516, 519, 529, 493, 491, 495}),                // Gaganaskin
            (17836, new uint[]{514, 513, 517, 516, 519, 529, 493, 491, 495}),                // Gazelleskin
            (26744, new uint[]{621, 620, 625, 623, 596, 648, 598, 600, 602}),                // Gliderskin
            (26745, new uint[]{621, 620, 625, 623, 596, 648, 598, 600, 602}),                // Zonureskin
            (36611, new uint[]{847, 848, 825, 826}),                                         // Saigaskin
            (36612, new uint[]{847, 848, 825, 826}),                                         // Kumbhiraskin
            (39591, new uint[]{846, 844, 824, 823}),                                         // Ophiotauroskin
         };

        private delegate void GatherEventDelegate(nint a1, ulong a2, nint a3, ulong a4);
        private Hook<GatherEventDelegate> gatherEventHook;

        private delegate void QuickGatherToggleDelegate(AddonGathering* a1);
        private Hook<QuickGatherToggleDelegate> quickGatherToggle;

        internal Vector4 DarkTheme = new Vector4(0.26f, 0.26f, 0.26f, 1f);
        internal Vector4 LightTheme = new Vector4(0.97f, 0.87f, 0.75f, 1f);
        internal Vector4 ClassicFFTheme = new Vector4(0.21f, 0f, 0.68f, 1f);
        internal Vector4 LightBlueTheme = new Vector4(0.21f, 0.36f, 0.59f, 0.25f);

        internal int SwingCount = 0;
        public override string Name => "Pandora Quick Gather";

        public override string Description => "Replaces the Quick Gather checkbox with a new one that enables better quick gathering. Works on all nodes and can be interrupted at any point by disabling the checkbox. Also remembers your settings between sessions.";

        public bool InDiadem => Svc.ClientState.TerritoryType == 939;

        private string LocationEffect;

        public class Configs : FeatureConfig
        {
            public bool CollectibleStop = false;

            public bool ShiftStop = false;

            public bool Gathering = false;

            public bool RememberLastNode = false;

            public bool Use500GPYield = false;

            public int GP500Yield = 500;

            public bool Use100GPYield = false;

            public int GP100Yield = 100;

            public bool UseTidings = false;

            public int GPTidings = 200;

            public int GatherersBoon = 100;

            public bool UseGivingLand = false;

            public int GPGivingLand = 200;

            public bool UseTwelvesBounty = false;

            public int GPTwelvesBounty = 150;

            public bool UseSolidReason = false;

            public int GPSolidReason = 300;

            public bool UseLuck = false;

            public int GPLuck = 200;
        }

        public Configs Config { get; private set; }


        public override FeatureType FeatureType => FeatureType.Other;

        private Overlays Overlay;

        public override bool UseAutoConfig => false;

        private ulong lastGatheredIndex = 10;
        private uint lastGatheredItem = 0;

        public unsafe override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("Gathering") != nint.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering");
                if (addon == null || !addon->IsVisible) return;

                if (addon->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10] is null) return;
                if (!addon->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->IsVisible) return;

                var node = addon->UldManager.NodeList[10];

                if (node->IsVisible)
                    node->ToggleVisibility(false);

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;

                Svc.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.ColorThemeType, out uint color);

                var theme = color switch
                {
                    0 => DarkTheme,
                    1 => LightTheme,
                    2 => ClassicFFTheme,
                    3 => LightBlueTheme
                };

                if (color == 3)
                {
                    addon->UldManager.NodeList[5]->ToggleVisibility(false);
                    addon->UldManager.NodeList[6]->ToggleVisibility(false);
                    addon->UldManager.NodeList[9]->ToggleVisibility(false);
                    addon->UldManager.NodeList[8]->ToggleVisibility(false);
                }

                LocationEffect = addon->UldManager.NodeList[8]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (color == 1)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 1f));
                }

                ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetColorU32(theme));
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudGrey3);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);

                ImGui.GetFont().Scale = scale.X;
                var oldScale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = 0.83f;
                ImGui.PushFont(ImGui.GetFont());
                size.Y *= 6.3f;
                size.X *= 1.065f;
                position.X -= 15f * scale.X;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);
                ImGui.SetNextWindowSize(size);
                ImGui.Begin($"###PandoraGathering{node->NodeID}", ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoResize);

                ImGui.Dummy(new Vector2(2f));

                ImGui.Columns(3, null, false);


                if (ImGui.Checkbox("Enable P. Gathering", ref Config.Gathering))
                {
                    if (Config.Gathering && node->GetAsAtkComponentCheckBox()->IsChecked)
                        QuickGatherToggle(null);

                    if (!Config.Gathering)
                        TaskManager.Abort();

                    SaveConfig(Config);
                }

                ImGui.NextColumn();

                if (ImGui.Checkbox("Remember Item", ref Config.RememberLastNode))
                    SaveConfig(Config);

                if (ImGui.IsItemHovered() && InDiadem)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("In the Diadem, this will remember the last slot selected and not the last item due to the varying nature of the nodes.");
                    ImGui.EndTooltip();
                }
                var language = Svc.ClientState.ClientLanguage;
                switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
                {
                    case 17:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.RawString}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.RawString}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.RawString}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.RawString}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                    case 16:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.RawString}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.RawString}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.RawString}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.RawString}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.RawString}", ref Config.UseGivingLand))
                {
                    Config.Use100GPYield = false;
                    Config.Use500GPYield = false;
                    Config.UseTidings = false;
                    SaveConfig(Config);
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.RawString.ToTitleCase()}", ref Config.UseTwelvesBounty))
                {
                    Config.Use100GPYield = false;
                    Config.Use500GPYield = false;
                    Config.UseTidings = false;
                    SaveConfig(Config);
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Reveal Hidden Items", ref Config.UseLuck))
                    SaveConfig(Config);

                ImGui.Columns(1);

                if (LocationEffect.Length > 0)
                {
                    ImGuiEx.ImGuiLineCentered("###LocationEffect", () =>
                    {
                        ImGui.Text($"{LocationEffect}");
                    });
                }
                ImGui.End();

                ImGui.GetFont().Scale = 1;
                ImGui.GetIO().FontGlobalScale = oldScale;
                ImGui.PopFont();

                ImGui.PopStyleVar(4);
                ImGui.PopStyleColor(color == 1 ? 3 : 2);

            }
        }

        public override void Enable()
        {
            Overlay = new Overlays(this);
            Config = LoadConfig<Configs>() ?? new Configs();
            gatherEventHook ??= Svc.Hook.HookFromSignature<GatherEventDelegate>("E8 ?? ?? ?? ?? 84 C0 74 ?? EB ?? 48 8B 89", GatherDetour);
            gatherEventHook.Enable();

            quickGatherToggle ??= Svc.Hook.HookFromSignature<QuickGatherToggleDelegate>("e8 ?? ?? ?? ?? eb 3f 4c 8b 4c 24 50", QuickGatherToggle);

            Common.OnAddonSetup += CheckLastItem;
            Svc.Condition.ConditionChange += ResetCounter;

            base.Enable();
        }

        private void ResetCounter(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value)
            {
                TaskManager.Abort();
                TaskManager.Enqueue(() => SwingCount = 0);
            }
        }


        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox($"Hold Shift to Temporarily Disable on Starting a Node", ref Config.ShiftStop))
                SaveConfig(Config);

            if (ImGui.Checkbox($"Disable Starting Buffs on Nodes with Collectibles", ref Config.CollectibleStop))
                SaveConfig(Config);

            ImGuiComponents.HelpMarker("This will stop Pandora from using any actions when you start a node with a collectible on it. This is intended to prevent wasting GP on buffs that don't apply to collectibles.");

            if (ImGui.Checkbox("Enable Pandora Gathering", ref Config.Gathering))
            {
                if (!Config.Gathering)
                    TaskManager.Abort();

                SaveConfig(Config);
            }

            if (ImGui.Checkbox("Remember Item Between Nodes", ref Config.RememberLastNode))
                SaveConfig(Config);

            if (ImGui.IsItemHovered() && InDiadem)
            {
                ImGui.BeginTooltip();
                ImGui.Text("In the Diadem, this will remember the last slot selected and not the last item due to the varying nature of the nodes.");
                ImGui.EndTooltip();
            }
            var language = Svc.ClientState.ClientLanguage;

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.RawString} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.RawString}", ref Config.Use100GPYield))
            {
                Config.UseGivingLand = false;
                Config.UseTwelvesBounty = false;
                SaveConfig(Config);
            }

            if (Config.Use100GPYield)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP1", ref Config.GP100Yield, 100, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.RawString} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.RawString}", ref Config.Use500GPYield))
            {
                Config.UseGivingLand = false;
                Config.UseTwelvesBounty = false;
                SaveConfig(Config);
            }

            if (Config.Use500GPYield)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP2", ref Config.GP500Yield, 500, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.RawString} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.RawString}", ref Config.UseTidings))
            {
                Config.UseGivingLand = false;
                Config.UseTwelvesBounty = false;
                SaveConfig(Config);
            }

            if (Config.UseTidings)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP3", ref Config.GPTidings, 200, 1000))
                    SaveConfig(Config);
            }

            if (Config.UseTidings)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. Gatherer's Boon% For Tidings", ref Config.GatherersBoon, 1, 100))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.RawString} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.RawString}", ref Config.UseSolidReason))
            {
                SaveConfig(Config);
            }

            if (Config.UseSolidReason)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP4", ref Config.GPSolidReason, 300, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.RawString}", ref Config.UseGivingLand))
            {
                Config.Use100GPYield = false;
                Config.Use500GPYield = false;
                Config.UseTidings = false;
                SaveConfig(Config);
            }

            if (Config.UseGivingLand)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP5", ref Config.GPGivingLand, 200, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.RawString.ToTitleCase()}", ref Config.UseTwelvesBounty))
            {
                Config.Use100GPYield = false;
                Config.Use500GPYield = false;
                Config.UseTidings = false;
                SaveConfig(Config);
            }

            if (Config.UseTwelvesBounty)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP6", ref Config.GPTwelvesBounty, 150, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Reveal Hidden Items", ref Config.UseLuck))
                SaveConfig(Config);

            if (Config.UseLuck)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP7", ref Config.GPLuck, 200, 1000))
                    SaveConfig(Config);
            }

        };

        private void CheckLastItem(SetupAddonArgs obj)
        {
            if (obj.AddonName == "Gathering" && Config.Gathering && (Config.ShiftStop && !ImGui.GetIO().KeyShift || !Config.ShiftStop))
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                TaskManager.Enqueue(() =>
                {
                    var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);

                    var ids = new List<uint>()
                    {
                    addon->GatheredItemId1,
                    addon->GatheredItemId2,
                    addon->GatheredItemId3,
                    addon->GatheredItemId4,
                    addon->GatheredItemId5,
                    addon->GatheredItemId6,
                    addon->GatheredItemId7,
                    addon->GatheredItemId8
                    };

                    PluginLog.Debug($"{string.Join(", ", ids)}");
                    if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x && y.Quest.Row > 0)))
                    {
                        Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                        Disable();
                        return;
                    }

                    var nodeHasCollectibles = ids.Any(x => Svc.Data.Excel.GetSheet<Item>().Any(y => y.RowId == x && y.IsCollectable));
                    PluginLog.Debug($"{nodeHasCollectibles}");
                    if (nodeHasCollectibles && !Config.CollectibleStop || !nodeHasCollectibles)
                    {
                        Dictionary<int, int> boonChances = new();

                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[25]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n1b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[24]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n2b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[23]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n3b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[22]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n4b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[21]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n5b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[20]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n6b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[19]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n7b);
                        int.TryParse(addon->AtkUnitBase.UldManager.NodeList[18]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var n8b);

                        boonChances.Add(0, n1b);
                        boonChances.Add(1, n2b);
                        boonChances.Add(2, n3b);
                        boonChances.Add(3, n4b);
                        boonChances.Add(4, n5b);
                        boonChances.Add(5, n6b);
                        boonChances.Add(6, n7b);
                        boonChances.Add(7, n8b);

                        if (Config.UseLuck && NodeHasHiddenItems(ids) && Svc.ClientState.LocalPlayer.CurrentGp >= Config.GPLuck)
                        {
                            TaskManager.Enqueue(() => UseLuck(), "UseLuck");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                            TaskManager.Enqueue(() => CheckLastItem(obj));
                            return;
                        }

                        if (Config.GPTidings <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseTidings && (boonChances.TryGetValue((int)lastGatheredIndex, out var val) && val >= Config.GatherersBoon || boonChances.Where(x => x.Value != 0).All(x => x.Value >= Config.GatherersBoon)))
                        {
                            TaskManager.Enqueue(() => UseTidings(), "UseTidings");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                        }

                        if (Config.GP500Yield <= Svc.ClientState.LocalPlayer.CurrentGp && Config.Use500GPYield)
                        {
                            TaskManager.Enqueue(() => Use500GPSkill(), "Use500GPSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                        }

                        if (Config.GP100Yield <= Svc.ClientState.LocalPlayer.CurrentGp && Config.Use100GPYield)
                        {
                            TaskManager.Enqueue(() => Use100GPSkill(), "Use100GPSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                        }

                        if (Config.GPGivingLand <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseGivingLand)
                        {
                            TaskManager.Enqueue(() => UseGivingLand(), "UseGivingSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                        }

                        if (Config.GPTwelvesBounty <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseTwelvesBounty)
                        {
                            TaskManager.Enqueue(() => UseTwelvesBounty(), "UseTwelvesSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                        }

                    }
                    if (Config.RememberLastNode)
                    {
                        if (lastGatheredIndex > 7)
                            return;

                        if (ids.Any(x => x == lastGatheredItem))
                        {
                            lastGatheredIndex = (ulong)ids.IndexOf(lastGatheredItem);
                        }

                        if (ids[(int)lastGatheredIndex] == lastGatheredItem || InDiadem)
                        {
                            var quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
                            if (quickGathering)
                            {
                                QuickGatherToggle(addon);
                            }

                            var receiveEventAddress = new nint(addon->AtkUnitBase.AtkEventListener.vfunc[2]);
                            var eventDelegate = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress)!;

                            var target = AtkStage.GetSingleton();
                            var eventData = EventData.ForNormalTarget(target, &addon->AtkUnitBase);
                            var inputData = InputData.Empty();

                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                            TaskManager.Enqueue(() => eventDelegate.Invoke(&addon->AtkUnitBase.AtkEventListener, ClickLib.Enums.EventType.CHANGE, (uint)lastGatheredIndex, eventData.Data, inputData.Data));
                        }
                    }
                });
            }
        }

        private void UseLuck()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17: //BTN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4095) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4095);
                    }
                    break;
                case 16: //MIN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4081) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4081);
                    }
                    break;
            }
        }

        private bool NodeHasHiddenItems(List<uint> ids)
        {
            foreach (var id in ids.Where(x => x != 0))
            {
                if (Svc.Data.GetExcelSheet<GatheringItem>().FindFirst(x => x.Item == id, out var item) && item.IsHidden) return false; //The node is exposed, don't need to expose it.
            }
            if (Seeds.Any(x => ids.Any(y => x.ItemId == y))) return true;
            var nodeId = Svc.ClientState.LocalPlayer.TargetObject?.DataId;
            if (Items.Any(x => x.NodeId == nodeId)) return true;
            if (Maps.Any(x => x.NodeIds.Any(y => y == nodeId))) return true;


            return false;
        }

        private bool? UseIntegrityAction()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 215) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Action, 215);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 232) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Action, 232);
                    }
                    break;
            }

            return true;
        }

        private bool? UseGivingLand()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4590) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4590);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4589) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4589);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
            }

            return true;
        }

        private bool? UseTwelvesBounty()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 282) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 282);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 280) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 280);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
            }

            return true;
        }

        private void Use100GPSkill()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286 || x.StatusId == 756))
                return;

            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 273) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 273);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4087) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4087);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 272) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 272);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4073) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4073);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
            }
        }

        private void Use500GPSkill()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219))
                return;

            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 224) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 224);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 241) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 241);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
            }

        }

        private void UseTidings()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667))
                return;

            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17: //BTN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 21204) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 21204);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
                    }
                    break;
                case 16: //MIN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 21203) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 21203);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
                    }
                    break;
            }

        }

        private void QuickGatherToggle(AddonGathering* a1)
        {
            if (a1 == null && Svc.GameGui.GetAddonByName("Gathering") != nint.Zero)
                a1 = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);

            a1->QuickGatheringComponentCheckBox->AtkComponentButton.Flags ^= 0x40000;
            quickGatherToggle.Original(a1);
        }

        private void GatherDetour(nint a1, ulong index, nint a3, ulong a4)
        {
            try
            {
                SwingCount++;
                PluginLog.Debug($"SWING {SwingCount}");
                var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);
                var quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
                if (quickGathering)
                {
                    QuickGatherToggle(addon);
                }

                if (addon != null && Config.Gathering)
                {
                    var ids = new List<uint>()
                {
                    addon->GatheredItemId1,
                    addon->GatheredItemId2,
                    addon->GatheredItemId3,
                    addon->GatheredItemId4,
                    addon->GatheredItemId5,
                    addon->GatheredItemId6,
                    addon->GatheredItemId7,
                    addon->GatheredItemId8
                };

                    if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x && y.Quest.Row > 0)))
                    {
                        Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                        Disable();
                        return;
                    }

                    var item = index switch
                    {
                        0 => addon->GatheredItemId1,
                        1 => addon->GatheredItemId2,
                        2 => addon->GatheredItemId3,
                        3 => addon->GatheredItemId4,
                        4 => addon->GatheredItemId5,
                        5 => addon->GatheredItemId6,
                        6 => addon->GatheredItemId7,
                        7 => addon->GatheredItemId8
                    };

                    if (item != lastGatheredItem && item != 0)
                    {
                        TaskManager.Abort();
                        lastGatheredIndex = index;
                        lastGatheredItem = item;
                    }

                    if (item != 0)
                    {
                        if ((Svc.Data.GetExcelSheet<Item>().FindFirst(x => x.RowId == item, out var sitem) && !sitem.IsCollectable) || (Svc.Data.GetExcelSheet<EventItem>().FindFirst(x => x.RowId == item, out var eitem) && eitem.Quest.Row == 0))
                        {
                            var receiveEventAddress = new nint(addon->AtkUnitBase.AtkEventListener.vfunc[2]);
                            var eventDelegate = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress)!;

                            var target = AtkStage.GetSingleton();
                            var eventData = EventData.ForNormalTarget(target, &addon->AtkUnitBase);
                            var inputData = InputData.Empty();


                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                            TaskManager.Enqueue(() =>
                            {
                                if (Config.GPSolidReason <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseSolidReason && SwingCount >= 2)
                                {
                                    TaskManager.EnqueueImmediate(() => UseIntegrityAction());
                                    TaskManager.EnqueueImmediate(() => !Svc.Condition[ConditionFlag.Gathering42]);
                                    TaskManager.EnqueueImmediate(() => UseWisdom());
                                }
                            });
                            TaskManager.Enqueue(() =>
                            {
                                if (Config.GP100Yield <= Svc.ClientState.LocalPlayer.CurrentGp && Config.Use100GPYield)
                                {
                                    TaskManager.EnqueueImmediate(() => Use100GPSkill());
                                }
                            });
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                            TaskManager.Enqueue(() => eventDelegate.Invoke(&addon->AtkUnitBase.AtkEventListener, ClickLib.Enums.EventType.CHANGE, (uint)index, eventData.Data, inputData.Data));
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                ex.Log();
            }

            gatherEventHook.Original(a1, index, a3, a4);

        }

        private bool? UseWisdom()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 26522) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Action, 26522);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 26521) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Action, 26521);
                    }
                    break;
            }

            return true;
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            SaveConfig(Config);
            gatherEventHook?.Dispose();
            quickGatherToggle?.Dispose();
            Common.OnAddonSetup -= CheckLastItem;

            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering");
            if (addon != null)
            {
                addon->UldManager.NodeList[5]->ToggleVisibility(true);
                addon->UldManager.NodeList[6]->ToggleVisibility(true);
                addon->UldManager.NodeList[8]->ToggleVisibility(true);
                addon->UldManager.NodeList[9]->ToggleVisibility(true);
                addon->UldManager.NodeList[10]->ToggleVisibility(true);
            }
            Svc.Condition.ConditionChange -= ResetCounter;

            base.Disable();
        }
    }
}
