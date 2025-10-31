using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Gamepad;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using Action = Lumina.Excel.Sheets.Action;

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
            (39806, 920), // Custom Ice Crystal
            (39808, 930), // Custom Wind Crystal
            (38791, 924), // Splendorous Water Shard
            (38789, 926), // Splendorous Earth Shard
            (38795, 923), // Adaptive Lightning Crystal
            (38797, 925), // Adaptive Fire Crystal
            (39812, 929), // Brilliant Lightning Cluster
            (39814, 931), // Brilliant Earth Cluster
            (41287, 938), // Inspirational Wind Cluster
            (41289, 940), // Inspirational Fire Cluster
            (41291, 939), // Nightforged Ice Cluster
            (41293, 941), // Nightforged Water Cluster
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

        private delegate void QuickGatherToggleDelegate(AddonGathering* a1);
        private Hook<QuickGatherToggleDelegate> quickGatherToggle;

        internal Vector4 DarkTheme = new Vector4(0.26f, 0.26f, 0.26f, 1f);
        internal Vector4 LightTheme = new Vector4(0.97f, 0.87f, 0.75f, 1f);
        internal Vector4 ClassicFFTheme = new Vector4(0.21f, 0f, 0.68f, 1f);
        internal Vector4 LightBlueTheme = new Vector4(0.21f, 0.36f, 0.59f, 0.25f);

        public override string Name => "Pandora Quick Gather";

        public override string Description => "Replaces the Quick Gather checkbox with a new one that enables better quick gathering. Works on all nodes and can be interrupted at any point by disabling the checkbox. Also remembers your settings between sessions.";

        public bool InDiadem => Svc.ClientState.TerritoryType == 939;

        private string? LocationEffect;
        private string? LocationEffect2;

        private bool HiddenRevealed = false;

        public class Configs : FeatureConfig
        {
            public bool CollectibleStop = false;

            public bool ShiftStop = false;

            public bool Gathering = false;

            public bool RememberLastNode = false;

            public bool DontBuffIfItemNotPresent = false;

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

            public bool GatherChanceUp = false;

            public int GPGatherChanceUp = 100;
        }

        public Configs Config { get; private set; }


        public override FeatureType FeatureType => FeatureType.Other;

        private Overlays? overlay;

        public override bool UseAutoConfig => false;

        private int lastGatheredIndex = 10;
        private uint lastGatheredItem = 0;
        private uint CurrentIntegrity { get; set; } = 0;
        private uint MaxIntegrity { get; set; } = 0;

        public override bool DrawConditions()
        {
            return Svc.GameGui.GetAddonByName("Gathering") != nint.Zero;
        }

        public override void Enable()
        {
            overlay = new Overlays(this);
            Config = LoadConfig<Configs>() ?? new Configs();

            quickGatherToggle ??= Svc.Hook.HookFromSignature<QuickGatherToggleDelegate>("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 80 B9 ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 48 8B 84 24 ?? ?? ?? ??", QuickGatherToggle);

            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Gathering", OnEvent);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Gathering", AddonSetup);
            Svc.Condition.ConditionChange += ResetCounter;
            Svc.Chat.ChatMessage += CheckRevisit;
            Svc.Framework.Update += UpdateIntegrity;

            base.Enable();
        }

        private void UpdateIntegrity(IFramework framework)
        {
            var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering").Address;
            if (addon != null)
            {
                CurrentIntegrity = addon->AtkValues[109].UInt;
                MaxIntegrity = addon->AtkValues[110].UInt;
            }
        }

        private void CheckRevisit(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type is (XivChatType)2107 && CurrentIntegrity == 0)
            {
                TaskManager.Abort();
                TaskManager.EnqueueDelay(1000);
                AddonSetup(AddonEvent.PostSetup, null);
            }
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(overlay!);
            SaveConfig(Config);
            quickGatherToggle?.Disable();
            Svc.AddonLifecycle.UnregisterListener(OnEvent);
            Svc.AddonLifecycle.UnregisterListener(AddonSetup);
            Svc.Chat.ChatMessage -= CheckRevisit;
            Svc.Framework.Update -= UpdateIntegrity;

            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering").Address;
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

        public override void Dispose()
        {
            quickGatherToggle?.Dispose();
            base.Dispose();
        }

        public unsafe override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("Gathering") != nint.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering").Address;
                if (addon == null) return;
                if (!addon->IsVisible) return;

                if (addon->UldManager.NodeListCount < 5) return;
                if (addon->UldManager.NodeList[2] is null) return;
                if (addon->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10] is null) return;
                if (!addon->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->IsVisible()) return;

                var node = addon->UldManager.NodeList[10];

                if (node->IsVisible())
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
                    3 => LightBlueTheme,
                    _ => throw new NotImplementedException()
                };

                if (color == 3)
                {
                    addon->UldManager.NodeList[5]->ToggleVisibility(false);
                    addon->UldManager.NodeList[6]->ToggleVisibility(false);
                    addon->UldManager.NodeList[9]->ToggleVisibility(false);
                    addon->UldManager.NodeList[8]->ToggleVisibility(false);
                }

                LocationEffect = addon->UldManager.NodeList[8]->GetAsAtkTextNode()->NodeText.GetText();
                LocationEffect2 = addon->UldManager.NodeList[7]->GetAsAtkTextNode()->NodeText.GetText();
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
                ImGui.Begin($"###PandoraGathering{node->NodeId}", ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoResize);

                ImGui.Dummy(new Vector2(2f));

                ImGui.Columns(3, default, false);

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
                switch (Svc.ClientState.LocalPlayer!.ClassJob.RowId)
                {
                    case 17:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.ToString()}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.ToString()}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.ToString()}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.ToString()}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                    case 16:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.ToString()}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.ToString()}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.ToString()}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.ToString()}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.ToString()}", ref Config.UseGivingLand))
                {
                    Config.Use100GPYield = false;
                    Config.Use500GPYield = false;
                    Config.UseTidings = false;
                    SaveConfig(Config);
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.ToString().ToTitleCase()}", ref Config.UseTwelvesBounty))
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
                    ImGuiEx.LineCentered("###LocationEffect", () =>
                    {
                        ImGui.Text($"{LocationEffect}");
                    });
                }
                if (LocationEffect2.Length > 0)
                {
                    ImGuiEx.LineCentered("###LocationEffect2", () =>
                    {
                        ImGui.Text($"{LocationEffect2}");
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

        private void OnEvent(AddonEvent type, AddonArgs args)
        {
            if (args is AddonReceiveEventArgs a)
            {
                if ((AtkEventType)a.AtkEventType is AtkEventType.ButtonClick)
                {
                    var index = a.EventParam;
                    CheckNodeAndClick(index);
                }
            }
        }

        private void CheckNodeAndClick(int index)
        {
            try
            {
                var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1).Address;

                if (addon != null && Config.Gathering)
                {
                    var ids = new List<uint>();
                    for (int i = 6; i <= (11 * 8); i += 11)
                    {
                        ids.Add(addon->AtkValues[i].UInt);
                    }
                    Svc.Log.Debug($"Gathering IDs: {string.Join(", ", ids)}");
                    if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x && y.Quest.RowId > 0)))
                    {
                        Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                        Disable();
                        return;
                    }

                    var item = ids[index];

                    if (item != lastGatheredItem && item != 0)
                    {
                        TaskManager.Abort();
                        lastGatheredIndex = (byte)index;
                        lastGatheredItem = item;
                    }

                    if (item != 0)
                    {
                        if ((Svc.Data.GetExcelSheet<Item>()!.FindFirst(x => x.RowId == item, out var sitem) && !sitem.IsCollectable) || (Svc.Data.GetExcelSheet<EventItem>().FindFirst(x => x.RowId == item, out var eitem) && eitem.Quest.RowId == 0))
                        {
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                            TaskManager.Enqueue(() =>
                            {
                                var diffIntegrity = MaxIntegrity - CurrentIntegrity;

                                if (Config.GPSolidReason <= Svc.ClientState.LocalPlayer!.CurrentGp && Config.UseSolidReason && CanUseIntegrityAction() && diffIntegrity >= 2)
                                {
                                    TaskManager.BeginStack();
                                    TaskManager.Enqueue(() => UseIntegrityAction());
                                    TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                                    TaskManager.Enqueue(() => UseWisdom());
                                    TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                                    TaskManager.InsertStack();
                                }
                            });
                            TaskManager.Enqueue(() =>
                            {
                                if (Config.GP100Yield <= Svc.ClientState.LocalPlayer!.CurrentGp && Config.Use100GPYield)
                                {
                                    TaskManager.InsertMulti([new(() => Use100GPSkill()), new(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction])]);
                                }
                            });

                            ClickGather(lastGatheredIndex);

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private bool CanUseIntegrityAction()
        {
            switch (Svc.ClientState.LocalPlayer!.ClassJob.RowId)
            {
                case 17:
                    return ActionManager.Instance()->GetActionStatus(ActionType.Action, 215) == 0;
                case 16:
                    return ActionManager.Instance()->GetActionStatus(ActionType.Action, 232) == 0;
            }

            return true;
        }

        private void AddonSetup(AddonEvent type, AddonArgs args)
        {
            if (Config.Gathering && ((Config.ShiftStop && !ImGui.GetIO().KeyShift && !GamePad.IsButtonHeld(Dalamud.Game.ClientState.GamePad.GamepadButtons.L2)) || !Config.ShiftStop))
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                TaskManager.Enqueue(() =>
                {
                    var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1).Address;

                    if (addon == null) return;

                    var ids = new List<uint>();
                    for (int i = 6; i <= (11 * 8); i += 11)
                    {
                        ids.Add(addon->AtkValues[i].UInt);
                    }

                    if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x && y.Quest.RowId > 0)))
                    {
                        Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                        Disable();
                        return;
                    }

                    if (Config.RememberLastNode && Config.DontBuffIfItemNotPresent && !ids.Any(x => x is not 0 && x == lastGatheredItem))
                    {
                        Svc.Log.Debug("Last gathered item not found in current node.");
                        return;
                    }

                    var nodeHasCollectibles = ids.Any(x => Svc.Data.Excel.GetSheet<Item>().Any(y => y.RowId == x && y.IsCollectable));
                    if (nodeHasCollectibles && !Config.CollectibleStop || !nodeHasCollectibles)
                    {
                        Dictionary<int, int> boonChances = new();
                        Dictionary<int, int> gatherChances = new();

                        for (int i = 0; i <= 7; i++)
                        {
                            int.TryParse(addon->AtkUnitBase.UldManager.NodeList[25 - i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out var boonChance);
                            boonChances.Add(i, boonChance);
                        }

                        Svc.Log.Debug($"{string.Join(", ", boonChances)}");

                        if (Config.UseLuck && NodeHasHiddenItems(ids) && Svc.ClientState.LocalPlayer!.CurrentGp >= Config.GPLuck && !HiddenRevealed)
                        {
                            TaskManager.Enqueue(() => UseLuck(), "UseLuck");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                            TaskManager.Enqueue(() => AddonSetup(type, args));
                            HiddenRevealed = true;
                            return;
                        }

                        HiddenRevealed = false;

                        if (Config.GPTidings <= Svc.ClientState.LocalPlayer!.CurrentGp && Config.UseTidings && (boonChances.TryGetValue(lastGatheredIndex, out var val) && val >= Config.GatherersBoon || boonChances.Where(x => x.Value != 0).All(x => x.Value >= Config.GatherersBoon)))
                        {
                            TaskManager.Enqueue(() => UseTidings(), "UseTidings");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                        }

                        if (Config.GP500Yield <= Svc.ClientState.LocalPlayer.CurrentGp && Config.Use500GPYield)
                        {
                            TaskManager.Enqueue(() => Use500GPSkill(), "Use500GPSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                        }

                        if (Config.GP100Yield <= Svc.ClientState.LocalPlayer.CurrentGp && Config.Use100GPYield)
                        {
                            TaskManager.Enqueue(() => Use100GPSkill(), "Use100GPSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                        }

                        if (Config.GPGatherChanceUp <= Svc.ClientState.LocalPlayer.CurrentGp && Config.GatherChanceUp)
                        {

                        }

                        if (Config.GPGivingLand <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseGivingLand)
                        {
                            TaskManager.Enqueue(() => UseGivingLand(), "UseGivingSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                        }

                        if (Config.GPTwelvesBounty <= Svc.ClientState.LocalPlayer.CurrentGp && Config.UseTwelvesBounty)
                        {
                            TaskManager.Enqueue(() => UseTwelvesBounty(), "UseTwelvesSetup");
                            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
                        }

                    }
                    if (Config.RememberLastNode)
                    {
                        if (lastGatheredIndex > 7)
                            return;

                        if (ids.Any(x => x == lastGatheredItem))
                        {
                            lastGatheredIndex = ids.IndexOf(lastGatheredItem);
                        }

                        if (ids[lastGatheredIndex] == lastGatheredItem || InDiadem)
                        {
                            var quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
                            if (quickGathering)
                            {
                                QuickGatherToggle(addon);
                            }

                            var integrityLeft = CurrentIntegrity;
                            if (integrityLeft > 1)
                                ClickGather(lastGatheredIndex);
                        }
                    }
                });
            }
        }

        private void ResetCounter(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value)
            {
                TaskManager.Abort();
            }
        }


        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox($"Hold Shift / {GamePad.ControllerButtons[Dalamud.Game.ClientState.GamePad.GamepadButtons.L2]} to Temporarily Disable on Starting a Node", ref Config.ShiftStop))
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

            if (Config.RememberLastNode)
            {
                using var _ = ImRaii.PushIndent();
                if (ImGui.Checkbox("Don't Buff if Item Not Present", ref Config.DontBuffIfItemNotPresent))
                    SaveConfig(Config);
            }

            if (ImGui.IsItemHovered() && InDiadem)
            {
                ImGui.BeginTooltip();
                ImGui.Text("In the Diadem, this will remember the last slot selected and not the last item due to the varying nature of the nodes.");
                ImGui.EndTooltip();
            }
            var language = Svc.ClientState.ClientLanguage;

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.ToString()} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.ToString()}", ref Config.Use100GPYield))
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

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.ToString()} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.ToString()}", ref Config.Use500GPYield))
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

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.ToString()} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.ToString()}", ref Config.UseTidings))
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

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.ToString()} / {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.ToString()}", ref Config.UseSolidReason))
            {
                SaveConfig(Config);
            }

            if (Config.UseSolidReason)
            {
                ImGui.PushItemWidth(300);
                if (ImGui.SliderInt("Min. GP###MinGP4", ref Config.GPSolidReason, 300, 1000))
                    SaveConfig(Config);
            }

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.ToString()}", ref Config.UseGivingLand))
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

            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.ToString().ToTitleCase()}", ref Config.UseTwelvesBounty))
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

        private void ClickGather(int index)
        {
            TaskManager!.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction]);
            TaskManager.Enqueue(() =>
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering").Address;
                if (addon is null) return;

                if (addon is null) return;
                var checkBox = addon->GetNodeById(17 + (uint)index)->GetAsAtkComponentCheckBox();
                if (checkBox is null) return;
                checkBox->AtkComponentButton.IsChecked = true;
                ECommons.Automation.Callback.Fire(addon, true, index);
                CheckNodeAndClick(index);
            });
        }

        private void UseLuck()
        {
            switch (Svc.ClientState.LocalPlayer!.ClassJob.RowId)
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
                if (Svc.Data.GetExcelSheet<GatheringItem>().FindFirst(x => x.Item.RowId == id, out var item) && item.IsHidden) return false; //The node is exposed, don't need to expose it.
                if (Maps.Any(x => x.MapId == id)) return false;
                if (Items.Any(x => x.ItemId == id)) return false;

            }
            if (Seeds.Any(x => ids.Any(y => x.ItemId == y))) return true;
            var NodeId = Svc.ClientState.LocalPlayer?.TargetObject?.BaseId;
            var baseNode = Svc.Data.GetExcelSheet<GatheringPoint>()?.Where(x => x.RowId == NodeId).First().GatheringPointBase.Value;
            Svc.Log.Debug($"{baseNode?.RowId}");
            if (Items.Any(x => x.NodeId == baseNode?.RowId)) return true;
            if (Maps.Any(x => x.NodeIds.Any(y => y == baseNode?.RowId))) return true;


            return false;
        }

        private bool? UseGatherChanceUp()
        {
            switch (Svc.ClientState.LocalPlayer!.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 220) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 220);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 237) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 237);
                    }
                    break;
            }

            return true;
        }
        private bool? UseIntegrityAction()
        {
            switch (Svc.ClientState.LocalPlayer!.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 215) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 215);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 232) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 232);
                    }
                    break;
            }

            return true;
        }

        private bool? UseGivingLand()
        {
            switch (Svc.ClientState.LocalPlayer?.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4590) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4590);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4589) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4589);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
            }

            return true;
        }

        private bool? UseTwelvesBounty()
        {
            switch (Svc.ClientState.LocalPlayer?.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 282) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 282);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 280) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 280);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
            }

            return true;
        }

        private void Use100GPSkill()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286 || x.StatusId == 756))
                return;

            switch (Svc.ClientState.LocalPlayer.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 273) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 273);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4087) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4087);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 272) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 272);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 4073) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 4073);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
            }
        }

        private void Use500GPSkill()
        {
            if (Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 219) ?? false)
                return;

            switch (Svc.ClientState.LocalPlayer?.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 224) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 224);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 241) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 241);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
            }

        }

        private void UseTidings()
        {
            if (Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 2667) ?? false)
                return;

            switch (Svc.ClientState.LocalPlayer?.ClassJob.RowId)
            {
                case 17: //BTN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 21204) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 21204);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
                    }
                    break;
                case 16: //MIN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 21203) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 21203);
                        TaskManager.Insert(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
                    }
                    break;
            }

        }

        private void QuickGatherToggle(AddonGathering* a1)
        {
            if (a1 == null && Svc.GameGui.GetAddonByName("Gathering") != nint.Zero)
                a1 = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1).Address;

            a1->QuickGatheringComponentCheckBox->AtkComponentButton.Flags ^= 0x40000;
            quickGatherToggle?.Original(a1);
        }

        private bool? UseWisdom()
        {
            switch (Svc.ClientState.LocalPlayer?.ClassJob.RowId)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 26522) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 26522);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 26521) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 26521);
                    }
                    break;
            }

            return true;
        }


    }
}
