using ClickLib.Bases;
using ClickLib.Enums;
using ClickLib.Structures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Helpers
{
    internal unsafe static class ClickHelper
    {
        private static ReceiveEventDelegate GetReceiveEvent(AtkEventListener* listener)
        {
            var receiveEventAddress = new IntPtr(listener->vfunc[2]);
            return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress)!;
        }

        private static void InvokeReceiveEvent(AtkEventListener* eventListener, EventType type, uint which, EventData eventData, InputData inputData)
        {
            var receiveEvent = GetReceiveEvent(eventListener);
            receiveEvent(eventListener, type, which, eventData.Data, inputData.Data);
        }

        private static void ClickAddonComponent(AtkComponentBase* unitbase, AtkComponentNode* target, uint which, EventType type, EventData? eventData = null, InputData? inputData = null)
        {
            eventData ??= EventData.ForNormalTarget(target, unitbase);
            inputData ??= InputData.Empty();

            InvokeReceiveEvent(&unitbase->AtkEventListener, type, which, eventData, inputData);
        }

        public static void ClickAddonButton(this AtkComponentButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE)
        => ClickAddonComponent(addon, target.AtkComponentBase.OwnerNode, which, type);

        public static void ClickRadioButton(this AtkComponentRadioButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE)
            => ClickAddonComponent(addon, target.AtkComponentBase.OwnerNode, which, type);
    }
}
