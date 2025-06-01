using Dalamud.Hooking;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

namespace PandorasBox.Features.Other
{
    internal class AfkDummyReset : Feature
    {
        public override string Name => "Inactivity Dummy Reset";

        public override string Description => "Automatically reset enmity on target dummies if you do not perform an action after a specified amount of time.";

        public override FeatureType FeatureType => FeatureType.Other;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Inactivity Timer (seconds)", EditorSize = 300, IntMax = 120, IntMin = 1, EnforcedLimit = true)]
            public int InactivityTimer = 1;
        }

        public Configs Config { get; private set; }

        internal unsafe delegate bool UseActionDelegate(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8);
        internal Hook<UseActionDelegate> UseActionHook;


        public unsafe override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            UseActionHook ??= Svc.Hook.HookFromAddress<UseActionDelegate>((nint)ActionManager.Addresses.UseAction.Value, UseActionDetour);
            UseActionHook?.Enable();
            base.Enable();
        }

        private unsafe bool UseActionDetour(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8)
        {
            if (type is ActionType.Action or ActionType.Ability)
            {
                try
                {
                    if (TaskManager.IsBusy)
                    {
                        TaskManager.Abort();
                    }

                    var delay = (Config.InactivityTimer * 1000) + (Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRow(am->GetAdjustedActionId(acId)).Cast100ms * 100);
                    TaskManager.EnqueueDelay(delay);
                    TaskManager.Enqueue(() => { Chat.Instance.SendMessage("/presetenmity"); });
                }
                catch(Exception ex)
                {
                    ex.Log();
                }
            }
            return UseActionHook.Original(am, type, acId, target, a5, a6, a7, a8);
        }

        public override void Disable()
        {
            SaveConfig(Config);
            UseActionHook?.Disable();
            base.Disable();
        }

        public override void Dispose()
        {
            UseActionHook?.Dispose();
            base.Dispose();
        }
    }
}
