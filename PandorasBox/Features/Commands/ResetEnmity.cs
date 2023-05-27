using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Character = Dalamud.Game.ClientState.Objects.Types.Character;

namespace PandorasBox.Features.Commands
{
    public unsafe class ResetEnmity : CommandFeature
    {
        public override string Name => "Reset Enmity";
        public override string Command { get; set; } = "/presetenmity";
        public override string[] Alias => new string[] { "/pre" };

        public override List<string> Parameters => new() { "t", "a" };
        public override string Description => "Resets the enmity of all enemies targeting you. Useful for target dummies. Accepts arguments for t(arget) or a(ll). Defaults to all.";
        protected override void OnCommand(List<string> args)
        {
            foreach (var p in Parameters)
            {
                switch (p)
                {
                    case "t":
                        ResetTarget();
                        break;
                    default:
                        ResetAll();
                        break;
                }
            }
        }

        private delegate long ExecuteCommandDelegate(uint id, int a1, int a2, int a3, int a4);
        private ExecuteCommandDelegate ExecuteCommand;

        private void Reset(int objectId)
        {
            // Reset enmity at target sig. This doesn't change often, but it does sometimes.
            nint scanText = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
            ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(scanText);

            PluginLog.Debug($"{nameof(ExecuteCommand)} +{scanText - Process.GetCurrentProcess().MainModule!.BaseAddress:X}");
            PluginLog.Information($"Resetting enmity {objectId:X}");

            long success = ExecuteCommand(0x13f, objectId, 0, 0, 0);
            PluginLog.Debug($"Reset enmity of {objectId:X} returned: {success}");
        }

        private void ResetTarget()
        {
            var target = Svc.Targets.Target;
            if (target is Character { NameId: 541 }) Reset((int)target.ObjectId);
        }

        private void ResetAll()
        {
            var addonByName = Svc.GameGui.GetAddonByName("_EnemyList", 1);
            if (addonByName != IntPtr.Zero)
            {
                var addon = (AddonEnemyList*)addonByName;
                // the 21 works now, but if this doesn't in the future, check this. It used to be 19.
                var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[21];

                for (var i = 0; i < addon->EnemyCount; i++)
                {
                    var enemyObjectId = numArray->IntArray[8 + i * 6];
                    var enemyChara = CharacterManager.Instance()->LookupBattleCharaByObjectId(enemyObjectId);
                    if (enemyChara is null) continue;
                    if (enemyChara->Character.NameID == 541) Reset(enemyObjectId);
                }
            }
        }
    }
}