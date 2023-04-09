using Dalamud.Game;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Linq;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoProspectTriangulate : Feature
    {
        public override string Name => "Auto-Prospect/Triangulate";

        public override string Description => "When switching to MIN or BTN, automatically activate the other jobs searching ability.";

        private uint? jobID;
        public uint? JobID
        {
            get => jobID;
            set
            {
                if (jobID != value)
                {
                    if (value is 16 or 17)
                        P.TaskManager.Enqueue(() => ActivateBuff(value));
                }
                jobID = value;
            }
        }

        private bool? ActivateBuff(uint? value)
        {
            ActionManager* am = ActionManager.Instance();   
            if (Svc.ClientState.LocalPlayer.StatusList.Where(x => x.StatusId == 217 || x.StatusId == 225).Count() == 2)
                return true;

            if (value == 16 && am->GetActionStatus(ActionType.Spell, 210) == 0)
            {
                am->UseAction(ActionType.Spell, 210);
                return true;
            }
            if (value == 17 && am->GetActionStatus(ActionType.Spell, 227) == 0)
            {
                am->UseAction(ActionType.Spell, 227);
                return true;
            }

            return false;

        }

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            JobID = Svc.ClientState.LocalPlayer.ClassJob.Id;
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
