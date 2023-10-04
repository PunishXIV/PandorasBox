using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using PandorasBox.Helpers;
using System.Collections.Generic;

namespace PandorasBox.Features.Commands
{
    public class InteractCommand : CommandFeature
    {
        public override string Name => "Interact with Target";
        public override string Command { get; set; } = "/pinteract";

        public override string[] Alias => new string[] { "/pint" };

        public override string Description => "Interacts with your current target.";

        protected unsafe override void OnCommand(List<string> args)
        {
            var target = TargetSystem.Instance()->Target;
            if (target != null)
            TargetSystem.Instance()->InteractWithObject(target);
        }
    }
}
