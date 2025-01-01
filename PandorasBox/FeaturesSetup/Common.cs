using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
using Action = Lumina.Excel.Sheets.Action;

namespace PandorasBox;

public static unsafe class Common
{

    // Common Delegates
    public delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);
    public delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private delegate void* AddonSetupDelegate(AtkUnitBase* addon);
    private static Hook<AddonSetupDelegate>? AddonSetupHook;

    private delegate void FinalizeAddonDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);
    private static Hook<FinalizeAddonDelegate>? FinalizeAddonHook;

    private static IntPtr LastCommandAddress;

    public static Utf8String* LastCommand { get; private set; }
    public static void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);

    public static event Action<SetupAddonArgs>? OnAddonSetup;
    public static event Action<SetupAddonArgs>? OnAddonPreSetup;
    public static event Action<SetupAddonArgs>? OnAddonFinalize;

    public static void Setup()
    {
        //LastCommandAddress = Svc.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 41 B1 01 49 8B D4 E8 ?? ?? ?? ?? 83 EB 06");
        //LastCommand = (Utf8String*)(LastCommandAddress);

        //AddonSetupHook = Svc.Hook.HookFromSignature<AddonSetupDelegate>("E8 ?? ?? ?? ?? 41 B1 1E", AddonSetupDetour);
        //AddonSetupHook?.Enable();

        //FinalizeAddonHook = Svc.Hook.HookFromSignature<FinalizeAddonDelegate>("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6", FinalizeAddonDetour);
        //FinalizeAddonHook?.Enable();
    }

    private static void* AddonSetupDetour(AtkUnitBase* addon)
    {
        try
        {
            OnAddonPreSetup?.Invoke(new SetupAddonArgs()
            {
                Addon = addon
            });
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "AddonSetupError");
        }
        var retVal = AddonSetupHook.Original(addon);
        try
        {
            OnAddonSetup?.Invoke(new SetupAddonArgs()
            {
                Addon = addon
            });
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "AddonSetupError2");
        }

        return retVal;
    }

    private static void FinalizeAddonDetour(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            OnAddonFinalize?.Invoke(new SetupAddonArgs()
            {
                Addon = atkUnitBase[0]
            });
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "FinalizeAddonError");
        }
        FinalizeAddonHook?.Original(unitManager, atkUnitBase);
    }

    public static AtkUnitBase* GetUnitBase(string name, int index = 1)
    {
        return (AtkUnitBase*)Svc.GameGui.GetAddonByName(name, index);
    }

    
    public static AtkValue* CreateAtkValueArray(params object[] values)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null) return null;
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                switch (v)
                {
                    case uint uintValue:
                        atkValues[i].Type = ValueType.UInt;
                        atkValues[i].UInt = uintValue;
                        break;
                    case int intValue:
                        atkValues[i].Type = ValueType.Int;
                        atkValues[i].Int = intValue;
                        break;
                    case float floatValue:
                        atkValues[i].Type = ValueType.Float;
                        atkValues[i].Float = floatValue;
                        break;
                    case bool boolValue:
                        atkValues[i].Type = ValueType.Bool;
                        atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                        break;
                    case string stringValue:
                        {
                            atkValues[i].Type = ValueType.String;
                            var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                            var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                            Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                            Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                            atkValues[i].String = (byte*)stringAlloc;
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                }
            }
        }
        catch
        {
            return null;
        }

        return atkValues;
    }


    public static void Shutdown()
    {
        if (ThrowawayOut != null)
        {
            Marshal.FreeHGlobal(new IntPtr(ThrowawayOut));
            ThrowawayOut = null;
        }

        AddonSetupHook?.Disable();
        AddonSetupHook?.Dispose();

        FinalizeAddonHook?.Disable();
        FinalizeAddonHook?.Dispose();
    }

    public const int UnitListCount = 18;
    public static AtkUnitBase* GetAddonByID(uint id)
    {
        var unitManagers = &AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
        for (var i = 0; i < UnitListCount; i++)
        {
            var unitManager = &unitManagers[i];
            foreach (var j in Enumerable.Range(0, Math.Min(unitManager->Count, unitManager->Entries.Length)))
            {
                var unitBase = unitManager->Entries[j].Value;
                if (unitBase != null && unitBase->Id == id)
                {
                    return unitBase;
                }
            }
        }

        return null;
    }

    public static bool IsActionUnlocked(uint id)
    {
        var unlockLink = Svc.Data.GetExcelSheet<Action>().GetRow(id).UnlockLink.RowId;
        if (unlockLink == 0) return true;
        return UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockLink);
    }
}

public unsafe class SetupAddonArgs
{
    public AtkUnitBase* Addon { get; init; }
    private string addonName;
    public string AddonName => addonName ??=Addon->NameString.Split('\0')[0];
}
