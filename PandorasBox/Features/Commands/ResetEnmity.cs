using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Character = Dalamud.Game.ClientState.Objects.Types.ICharacter;

namespace PandorasBox.Features.Commands
{
    public unsafe class ResetEnmity : CommandFeature
    {
        public override string Name => "Reset Enmity";
        public override string Command { get; set; } = "/presetenmity";
        public override string[] Alias => new string[] { "/pre" };

        public override List<string> Parameters => new() { "t", "a" };
        public override string Description => "Resets combat with target dummies. Accepts arguments for t(arget) or a(ll). Defaults to all.";
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

        private void Reset(int GameObjectId)
        {
            // Reset enmity at target sig. This doesn't change often, but it does sometimes.
            nint scanText = Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B E9 41 8B D9 48 8B 0D ?? ?? ?? ?? 41 8B F8 8B F2");
            ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(scanText);

            Svc.Log.Debug($"{nameof(ExecuteCommand)} +{scanText - Process.GetCurrentProcess().MainModule!.BaseAddress:X}");
            Svc.Log.Information($"Resetting enmity {GameObjectId}");

            long success = ExecuteCommand(0x13f, GameObjectId, 0, 0, 0);
            Svc.Log.Debug($"Reset enmity of {GameObjectId} returned: {success}");
        }

        private void ResetTarget()
        {
            var target = Svc.Targets.Target;
            if (target is Character { NameId: 541 }) Reset((int)target.GameObjectId);
        }

        private void ResetAll()
        {
            var addonByName = Svc.GameGui.GetAddonByName("_EnemyList", 1);
            if (addonByName != IntPtr.Zero)
            {
                var addon = (AddonEnemyList*)addonByName.Address;
                // the 21 works now, but if this doesn't in the future, check this. It used to be 19.
                var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[21];

                for (var i = 0; i < addon->EnemyCount; i++)
                {
                    var enemyObjectId = numArray->IntArray[8 + i * 6];
                    var enemyChara = CharacterManager.Instance()->LookupBattleCharaByEntityId((uint)enemyObjectId);
                    if (enemyChara is null) continue;
                    if (enemyChara->Character.NameId == 541) Reset(enemyObjectId);
                }
            }
        }
    }
}
