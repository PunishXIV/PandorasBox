using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoInteractDungeons : Feature
    {
        public override string Name => "Auto-interact with Objects in Instances";

        public override string Description => "Automatically try to pick all the keys, levers, and other thingymabobs. Also works to try and open doors and stuff.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        private const float slowCheckInterval = 0.1f;
        private float slowCheckRemaining = 0.0f;

        public List<uint> Exits = new List<uint>() { 2000046, 2000066, 2000139, 2000187, 2000275, 2000370, 2000493, 2000596, 2000605, 2000683, 2000788, 
                                                     2001143, 2001144, 2001215, 2001216, 2001610, 2001695, 2001716, 2001717, 2001835, 2001871, 2002502, 
                                                     2002738, 2002740, 2002879, 2002880, 2002888, 2003454, 2004012, 2004013, 2004014, 2004015, 2004361, 
                                                     2004651, 2004781, 2004783, 2004805, 2004936, 2004966, 2005218, 2005313, 2005332, 2005333, 2005334, 
                                                     2005335, 2005337, 2005445, 2005809, 2006016, 2006235, 2006413, 2006421, 2006430, 2006431, 2006502, 
                                                     2006963, 2007002, 2007003, 2007076, 2007240, 2007380, 2007403, 2007444, 2007463, 2007465, 2007515, 
                                                     2007526, 2007528, 2007530, 2007766, 2007781, 2007786, 2008134, 2008372, 2008489, 2009037, 2009044, 
                                                     2009071, 2009215, 2009267, 2009289, 2009468, 2009523, 2009641, 2009658, 2009675, 2009751, 2009753, 
                                                     2009758, 2009986, 2009987, 2010599, 2010600, 2010601, 2010602, 2010635, 2010742, 2010759, 2010829, 
                                                     2010831, 2010954, 2011059, 2011084, 2011102, 2011105, 2011137, 2011155, 2011235, 2011236, 2011237, 
                                                     2011238, 2011239, 2011250, 2011268, 2011277, 2011282, 2011309, 2011393, 2011583, 2011673, 2011721, 
                                                     2011729, 2011739, 2011851, 2011937, 2012292, 2012294, 2012296, 2012341, 2012380, 2012385, 2012528, 
                                                     2012529, 2012531, 2012533, 2012613, 2012683, 2012718, 2012721, 2012842, 2012871, 2013077, 2013104, 
                                                     2013137, 2013167, 2013227, 2013290 };

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (ms)", IntMin = 100, IntMax = 10000, EditorSize = 350)]
            public int Throttle = 1500;

            [FeatureConfigOption("Exclude Exits", "", 1)]
            public bool ExcludeExit = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;

                if (Svc.ClientState.LocalPlayer == null) return;
                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty])
                {
                    var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj && GameObjectHelper.GetTargetDistance(x) < 2).ToList();
                    if (nearbyNodes.Count == 0)
                        return;

                    var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
                    var baseObj = (GameObject*)nearestNode.Address;

                    if (!baseObj->GetIsTargetable())
                        return;

                    var sheetItem = Svc.Data.GetExcelSheet<EObj>().First(x => x.RowId == baseObj->DataID);
                    if ((sheetItem.SgbPath.Value.SgbPath.RawString.Contains("bgcommon/world/lvd/shared/for_vfx/sgvf_w_lvd_b0005.sgb") || Exits.Contains(sheetItem.RowId)) && Config.ExcludeExit)
                        return;


                    Svc.Targets.Target = nearestNode;

                    if (!TaskManager.IsBusy)
                    {
                        TaskManager.DelayNext("InteractDung", Config.Throttle);
                        TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                    }
                }
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
