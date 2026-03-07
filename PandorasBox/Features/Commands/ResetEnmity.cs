using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Character = Dalamud.Game.ClientState.Objects.Types.ICharacter;

namespace PandorasBox.Features.Commands
{
    public unsafe class ResetEnmity : CommandFeature
    {
        public override string Name => "Reset Enmity";
        public override string Command { get; set; } = "/presetenmity";
        public override string[] Alias => ["/pre"];

        public override List<string> Parameters => ["t", "a"];
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

        private static void ResetTarget()
        {
            if (Svc.Targets.Target is Character { NameId: 541, GameObjectId: var id })
                Reset(id);
        }

        private static void ResetAll()
        {
            foreach (var dummy in Svc.Objects.Where(x => x is IBattleChara ch && ch.NameId == 541))
                Reset(dummy.GameObjectId);
        }

        private static void Reset(ulong GameObjectId)
        {
            Svc.Log.Information($"Resetting enmity {GameObjectId}");
            Svc.Log.Debug($"Reset enmity of {GameObjectId} returned: {GameMain.ExecuteCommand(319, (int)GameObjectId)}");
        }
    }
}
