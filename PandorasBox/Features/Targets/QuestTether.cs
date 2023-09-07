using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.SplatoonAPI;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    internal class QuestTether : Feature
    {
        public override string Name => "Quest Tether (Requires Splatoon)";

        public override string Description => "Draw tethers to your current quest objectives";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public override void Enable()
        {
            if (Splatoon.IsConnected())
            {
                Svc.Framework.Update += UpdateTethers;
                base.Enable();
            }
            else
            {
                DuoLog.Error("Unable to enable due to not being connected to Splatoon.");
            }
        }

        private unsafe void UpdateTethers(Framework framework)
        {
            foreach (var quest in QuestManager.Instance()->NormalQuestsSpan)
            {
                if (quest.QuestId == 0) continue;

                var sheetItem = Svc.Data.GetExcelSheet<Quest>().FirstOrDefault(x => x.Id.RawString.Length >= 5 && Convert.ToUInt16(x.Id.RawString.Substring(x.Id.RawString.Length - 5)) == quest.QuestId);


                if (sheetItem != null)
                {
                    for (int i = 0; i <= 49; i++)
                    {
                        if (quest.Sequence == i)
                        {
                            var instruction = sheetItem.ScriptInstruction[i];
                            var arg = sheetItem.ScriptArg[i];
                            var listener = sheetItem.Listener[i];

                            if (Svc.Objects.FindFirst(x => x.DataId == listener, out var eobj))
                            {
                                Splatoon.AddDynamicElement(eobj.Name.TextValue, new Element(ElementType.CircleRelativeToActorPosition)
                                {
                                    refActorDataID = eobj.DataId,
                                    refActorComparisonType = RefActorComparisonType.DataID,
                                    tether = true,
                                    onlyTargetable = true,
                                    thicc = 5f,
                                    color = 4279786209,
                                    Enabled = true,
                                }, 0.01f);
                            }
                        }
                    }
                }
            }
        }

        private unsafe void DrawQuestTethers()
        {

        }

        public override void Disable()
        {
            Svc.Framework.Update -= UpdateTethers;
            base.Disable();
        }
    }
}
