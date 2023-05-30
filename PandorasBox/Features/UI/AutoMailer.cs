using PandorasBox.FeaturesSetup;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using ECommons;
using ImGuiNET;
using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Memory;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Dalamud.Game.Text.SeStringHandling;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoMailer : Feature
    {
        public override string Name => "Auto Mailer";

        public override string Description => "Mails a specific item to a specific person.";

        public override FeatureType FeatureType => FeatureType.UI;

        // internal Overlays OverlayWindow;

        public class Configs : FeatureConfig
        {
            public string SelectedFriend = "Annie Starbinder";
            public uint SelectedItem = 17836;
            public int position_in_list = 1;
        }

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;

        [FFXIVClientStructs.Attributes.Agent(AgentId.SocialFriendList)]
        [StructLayout(LayoutKind.Explicit, Size = 0xC8)]
        public unsafe struct AgentFriendList
        {
            public static AgentFriendList* Instance() => (AgentFriendList*)AgentModule.Instance()->GetAgentByInternalId(AgentId.SocialFriendList);

            [FieldOffset(0x00)] public AgentInterface AgentInterface;
            [FieldOffset(0x28)] public InfoProxyFriendList* InfoProxy;

            public uint Count => InfoProxy->InfoProxyCommonList.InfoProxyPageInterface.InfoProxyInterface.GetEntryCount();
            public InfoProxyCommonList.CharacterData* GetFriend(uint index) => InfoProxy->InfoProxyCommonList.GetEntry(index);
            public InfoProxyCommonList.CharacterData* this[uint index] => GetFriend(index);
        }

        // private List<AtkValue> friends = Enumerable.Range(28, 201)
        //       .Select(i => ((AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterAddress"))->AtkValues[i])
        //       .Where(atkValue => atkValue.String != null)
        //       .ToList();

        private const int InfoOffset = 0x28;
        private const int LengthOffset = 0x10;
        private const int ListOffset = 0x98;
        public static unsafe IList<FriendListEntry> FriendList
        {
            get
            {
                var friendListAgent = (IntPtr)Framework.Instance()
                            ->GetUiModule()
                        ->GetAgentModule()
                    ->GetAgentByInternalId(AgentId.SocialFriendList);
                if (friendListAgent == IntPtr.Zero)
                {
                    return Array.Empty<FriendListEntry>();
                }

                var info = *(IntPtr*)(friendListAgent + InfoOffset);
                if (info == IntPtr.Zero)
                {
                    return Array.Empty<FriendListEntry>();
                }

                var length = *(ushort*)(info + LengthOffset);
                if (length == 0)
                {
                    return Array.Empty<FriendListEntry>();
                }

                var list = *(IntPtr*)(info + ListOffset);
                if (list == IntPtr.Zero)
                {
                    return Array.Empty<FriendListEntry>();
                }

                var entries = new List<FriendListEntry>(length);
                for (var i = 0; i < length; i++)
                {
                    var entry = *(FriendListEntry*)(list + i * FriendListEntry.Size);
                    entries.Add(entry);
                }

                return entries;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        public unsafe struct FriendListEntry
        {
            internal const int Size = 96;

            /// <summary>
            /// The content ID of the friend.
            /// </summary>
            [FieldOffset(0x00)]
            public readonly ulong ContentId;

            /// <summary>
            /// The current world of the friend.
            /// </summary>
            [FieldOffset(0x16)]
            public readonly ushort CurrentWorld;

            /// <summary>
            /// The home world of the friend.
            /// </summary>
            [FieldOffset(0x18)]
            public readonly ushort HomeWorld;

            /// <summary>
            /// The job the friend is currently on.
            /// </summary>
            [FieldOffset(0x21)]
            public readonly byte Job;

            /// <summary>
            /// The friend's raw SeString name. See <see cref="Name"/>.
            /// </summary>
            [FieldOffset(0x22)]
            public fixed byte RawName[32];

            /// <summary>
            /// The friend's raw SeString free company tag. See <see cref="FreeCompany"/>.
            /// </summary>
            [FieldOffset(0x42)]
            public fixed byte RawFreeCompany[5];

            /// <summary>
            /// The friend's name.
            /// </summary>
            public SeString Name
            {
                get
                {
                    fixed (byte* ptr = this.RawName)
                    {
                        return MemoryHelper.ReadSeStringNullTerminated((IntPtr)ptr);
                    }
                }
            }

            /// <summary>
            /// The friend's free company tag.
            /// </summary>
            public SeString FreeCompany
            {
                get
                {
                    fixed (byte* ptr = this.RawFreeCompany)
                    {
                        return MemoryHelper.ReadSeStringNullTerminated((IntPtr)ptr);
                    }
                }
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            // List<AtkValue> friends = Enumerable.Range(28, 201)
            //     .Select(i => addon->AtkValues[i])
            //     .Where(atkValue => atkValue.String != null)
            //     .ToList();
            var agent = AgentFriendList.Instance();
            if (agent == null) return;
            var friends = FriendList.ToList();
            if (ImGui.BeginCombo("Select Friend", ""))
            {
                PluginLog.Log("in button" + agent->Count);
                foreach (FriendListEntry friend in friends)
                {
                    if (ImGui.Selectable(friend.Name.ToString(), Config.SelectedFriend == friend.Name.ToString()))
                    {
                        Config.SelectedFriend = friend.Name.ToString();
                    }
                }
                // for (var i = 0U; i < agent->Count; i++)
                // {
                //     PluginLog.Log("in loop");
                //     var friend = agent->GetFriend(i);
                //     if (friend == null)
                //     {
                //         PluginLog.Log("friend is null");
                //         continue;
                //     }
                //     if (friend->HomeWorld != Svc.ClientState.LocalPlayer.HomeWorld.Id) continue;
                //     var name = MemoryHelper.ReadString(new nint(friend->Name), 32);
                //     PluginLog.Log("in loop" + name);
                //     if (ImGui.Selectable(name, Config.SelectedFriend == name))
                //     {
                //         Config.SelectedFriend = name;
                //     }
                // }
                ImGui.EndCombo();
            }
        };

        private void TryAutoMailer()
        {
            TaskManager.Enqueue(() => SelectNew(), "Selecting New");
            TaskManager.Enqueue(() => OpenRecipient(), "Opening Dropdown");
            TaskManager.Enqueue(() => SelectRecipient(Config.SelectedFriend, Config.position_in_list), "Selecting Friend");
            TaskManager.Enqueue(() => AttachItem(Config.SelectedItem), "Attaching Item");
            TaskManager.Enqueue(() => SendButton(Config.SelectedFriend), "Selecting Send");
            // select yes function or something
            TaskManager.DelayNext("WaitForDelay", 400);
        }

        private unsafe bool? SelectNew()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterList", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting New", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting New"));
            try
            {
                var letterslistPTR = Svc.GameGui.GetAddonByName("LetterList", 1);
                if (letterslistPTR == IntPtr.Zero)
                    return false;

                var letterslistWindow = (AtkUnitBase*)letterslistPTR;
                if (letterslistWindow == null)
                    return false;


                var NewButton = stackalloc AtkValue[4];
                NewButton[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };
                NewButton[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                NewButton[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                NewButton[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                letterslistWindow->FireCallback(1, NewButton);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private unsafe bool? OpenRecipient()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterEditor", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Dropdown", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Dropdown"));
            try
            {
                var letterEditorPTR = Svc.GameGui.GetAddonByName("LetterEditor", 1);
                if (letterEditorPTR == IntPtr.Zero)
                    return false;

                var letterEditorWindow = (AtkUnitBase*)letterEditorPTR;
                if (letterEditorWindow == null)
                    return false;


                var DropDownMenu = stackalloc AtkValue[1];
                DropDownMenu[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 7,
                };

                letterEditorWindow->FireCallback(1, DropDownMenu);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool? SelectRecipient(string friend, int position_in_list)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterAddress", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Friend", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Friend"));
            try
            {
                var letterAddressPTR = Svc.GameGui.GetAddonByName("LetterAddress", 1);
                if (letterAddressPTR == IntPtr.Zero)
                    return false;

                var letterAddressWindow = (AtkUnitBase*)letterAddressPTR;
                if (letterAddressWindow == null)
                    return false;

                Callback.Fire(addon, false, 0, position_in_list, friend);
                // var FriendList = stackalloc AtkValue[3];
                // FriendList[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 7,
                // };
                // FriendList[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = position_in_list,
                // };
                // FriendList[2] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                //     String = Marshal.FreeHGlobal(new IntPtr(friend)),
                // };

                // letterAddressWindow->FireCallback(1, FriendList);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool? AttachItem(uint itemId)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterAddress", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;

            var invId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID();

            if (InventoryManager.Instance()->GetInventoryItemCount(itemId) == 0)
            {
                return true;
            }
            if (!Common.GetAddonByID(invId)->IsVisible)
            {
                return null;
            }

            var inventories = new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var inv in inventories)
            {
                var container = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (int i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == itemId)
                    {
                        var ag = AgentInventoryContext.Instance();
                        ag->OpenForItemSlot(container->Type, i, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID());
                        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1);
                        if (contextMenu != null)
                        {
                            var values = stackalloc AtkValue[5];
                            values[0] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                Int = 0
                            };
                            values[1] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                UInt = 0
                            };
                            values[2] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                                UInt = 0
                            };
                            values[3] = new AtkValue()
                            {
                                // Unknown Type: 0
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                                UInt = 0
                            };
                            values[4] = new AtkValue()
                            {
                                // Unknown Type: 0
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                                UInt = 0
                            };
                            contextMenu->FireCallback(5, values, (void*)1);

                            // TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Item, itemId, Svc.ClientState.LocalPlayer.ObjectId) == 0);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private unsafe bool? SendButton(string friend)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterEditor", 1);
            if (!Svc.Condition[ConditionFlag.OccupiedInEvent] || !Svc.Condition[ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Send", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Send"));
            try
            {
                var letterEditorPTR = Svc.GameGui.GetAddonByName("LetterEditor", 1);
                if (letterEditorPTR == IntPtr.Zero)
                    return false;

                var letterEditorWindow = (AtkUnitBase*)letterEditorPTR;
                if (letterEditorWindow == null)
                    return false;

                Callback.Fire(addon, false, 0, 0, friend, "", 0u, 0u);


                // var SendBtn = stackalloc AtkValue[6];
                // SendBtn[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };
                // SendBtn[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 0,
                // };
                // SendBtn[2] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                //     String = friend,
                // };
                // SendBtn[3] = new()
                // {
                //     // it's empty???
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String,
                // };
                // SendBtn[4] = new()
                // {
                //     // Unknown Type: 0
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = 0
                // };
                // SendBtn[5] = new()
                // {
                //     // Unknown Type: 0
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = 0
                // };

                // letterEditorWindow->FireCallback(1, SendBtn);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Enable()
        {
            // OverlayWindow = new(this);
            // P.Ws.AddWindow(OverlayWindow);
            base.Enable();
        }

        public override void Disable()
        {
            // P.Ws.RemoveWindow(OverlayWindow);
            // OverlayWindow = null;
            base.Disable();
        }
    }
}
