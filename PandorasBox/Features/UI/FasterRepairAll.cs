using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;

namespace PandorasBox.Features;

public unsafe class FasterRepairAll : Feature
{
    public override string Name => "Faster Repair All";
    public override string Description => "Substitutes the repair all button with a function to repair all instantly as opposed to queueing them one by one. Is this company stupid or what?";
    public override FeatureType FeatureType => FeatureType.UI;

    private const uint EventParamId = 0x50420000;
    public override void Enable()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Repair", AddEvent);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Repair", HandleEvent);
    }

    public override void Disable()
    {
        Svc.AddonLifecycle.UnregisterListener(AddEvent);
        Svc.AddonLifecycle.UnregisterListener(HandleEvent);
    }

    private unsafe void AddEvent(AddonEvent type, AddonArgs args)
    {
        var node = ((AtkUnitBase*)args.Addon.Address)->UldManager.SearchNodeById<AtkResNode>(12);
        node->AddEvent(AtkEventType.ButtonClick, EventParamId, &((AtkUnitBase*)args.Addon.Address)->AtkEventListener, null, false);
        // you have to match the event type that you're trying to replace or else the custom event doesn't go through
    }

    private void HandleEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs rea) return;
        // the normal event. Set both to 0 to block
        if (rea is { AtkEventType: (byte)AtkEventType.ButtonClick, EventParam: 5 })
        {
            rea.AtkEventType = 0;
            rea.EventParam = 0;
        }
        // custom event. Still has to be set to 0 or else the normal triggers (why?!)
        if (rea is { AtkEventType: (byte)AtkEventType.ButtonClick, EventParam: (int)EventParamId })
        {
            rea.AtkEventType = 0;
            rea.EventParam = 0;
            RepairAll();
        }
    }

    private unsafe void RepairAll()
    {
        if (AgentRepair.Instance()->IsSelfRepairOpen)
        {
            GameMain.ExecuteCommand(435, (int)InventoryType.EquippedItems);
            Enum.GetValues<RepairCategory>().ToList().ForEach(inv => GameMain.ExecuteCommand(436, (int)inv));
        }
        else
        {
            GameMain.ExecuteCommand(1602, (int)InventoryType.EquippedItems);
            Enum.GetValues<RepairCategory>().ToList().ForEach(inv => GameMain.ExecuteCommand(1601, (int)inv));
        }
    }

    private enum RepairCategory : int
    {
        MainOffHand = 0,
        HeadBodyArms = 1,
        LegsFeet = 2,
        EarsNeck = 3,
        WristRing = 4,
        Inventory = 5,
    }
}
