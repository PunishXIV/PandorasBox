using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using Lumina.Excel.Sheets;
using System;
using System.Runtime.InteropServices;
using static ECommons.GenericHelpers;

namespace PandorasBox.Helpers;

internal static class IslandSanctuaryHelper
{
    private unsafe delegate nint ReceiveEventDelegate(AtkEventListener* eventListener, AtkEventType eventType, uint eventParam, void* eventData, void* inputData);

    public enum ScheduleListEntryType : int
    {
        NormalEntry = 0,
        LastEntry = 1,
        Category = 2,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public unsafe struct ScheduleListEntry
    {
        [FieldOffset(0x0)] public ScheduleListEntryType Type;
        [FieldOffset(0x4)] public uint Value; // for Category - category id (time/etc), otherwise - MJICraftworksObject row index - 1
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MJICraftScheduleSettingData
    {
        [FieldOffset(0x1A8)] public StdVector<Pointer<Pointer<ScheduleListEntry>>> Entries;
        [FieldOffset(0x1E4)] public int NumEntries;

        public int FindEntryIndex(uint rowId)
        {
            for (var i = 0; i < NumEntries; ++i)
            {
                var p = Entries[i].Value->Value;
                if (p->Type != ScheduleListEntryType.Category && p->Value == rowId - 1)
                    return i;
            }
            return -1;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct AddonMJICraftScheduleSetting
    {
        [FieldOffset(0x000)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(0x220)] public MJICraftScheduleSettingData* Data;
    }

    public static unsafe bool IsWorkshopUnlocked(int w, out int maxWorkshops)
    {
        maxWorkshops = 0;
        try
        {
            var currentRank = MJIManager.Instance()->IslandState.CurrentRank;
            switch (w)
            {
                case 1 when currentRank < 3:
                    maxWorkshops = 0;
                    break;
                case 2 when currentRank < 6:
                    maxWorkshops = 1;
                    break;
                case 3 when currentRank < 8:
                    maxWorkshops = 2;
                    break;
                case 4 when currentRank < 14:
                    maxWorkshops = 3;
                    break;
                default:
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            ex.Log();
            return false;
        }
    }

    public static unsafe bool isCraftworkObjectCraftable(MJICraftworksObject item) => MJIManager.Instance()->IslandState.CurrentRank >= item.LevelReq;
    public static unsafe bool isWorkshopOpen() => TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon) && addon->IsVisible;
    public static unsafe bool isCraftSelectOpen() => TryGetAddonByName<AtkUnitBase>("MJICraftScheduleSetting", out var addon) && addon->IsVisible;
    public static unsafe int? GetOpenCycle() =>
        TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon) && addon->IsVisible && addon->AtkValues[0].Type != 0
        ? (int)addon->AtkValues[0].UInt : null;
}
