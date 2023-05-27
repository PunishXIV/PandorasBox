using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Character = Dalamud.Game.ClientState.Objects.Types.Character;

namespace PandorasBox.Features.Commands
{
    public unsafe class ResetEnmity : CommandFeature
    {
        public override string Name => "Reset Enmity";
        public override string Command { get; set; } = "/pan-resetenmity";
        public override string[] Alias => new string[] { "/pan-re" };

        public override List<string> Parameters => new() { "t", "a" };
        public override string Description => "Resets the enmity of all enemies targeting you. Useful for target dummies.";
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
                // if (args.Any(x => x == p))
                // {
                //     Svc.Chat.Print($"Test command executed with argument {p}.");
                // }
            }
        }

        private delegate long ExecuteCommandDele(int id, int a1, int a2, int a3, int a4);
        private ExecuteCommandDele ExecuteCommand;

        private void Reset(int objectId)
        {
            PluginLog.Information($"[Pandora's Box] Resetting enmity {objectId:X}");
            ExecuteCommand(0x140, objectId, 0, 0, 0);
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
                var numArray = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder.NumberArrays[19];

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