using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;
using System.Numerics;

namespace PandorasBox.Features.Targets
{
    public unsafe class ActionTargeting : Feature
    {
        public override string Name => "Action Combat Targeting";

        public override string Description => "Automatically targets and switches your target to the nearest within your line of sight.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Unset Target if not in cone range (all times, use with caution)", "", 1)]
            public bool UnsetTargetRange = false;

            [FeatureConfigOption("Unset Target if not in cone range (only in combat)")]
            public bool UnsetTargetCombat = false;

            [FeatureConfigOption("Max Distance (yalms)", "", 2, FloatMin = 0.1f, FloatMax = 30f, FloatIncrements = 0.1f, EditorSize = 300)]
            public float MaxDistance = 3f;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            TargetEnemy();
        }

        public unsafe void TargetEnemy()
        {
            if (NearestConeTarget() != null)
                Svc.Targets.Target = NearestConeTarget();
            else if (Config.UnsetTargetRange && Svc.Targets.Target != null && Svc.Targets.Target is IBattleNpc)
                Svc.Targets.Target = null;
            else if (Config.UnsetTargetCombat && Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] && Svc.Targets.Target != null && Svc.Targets.Target is IBattleNpc)
                Svc.Targets.Target = null;
        }

        public bool CanConeAoe()
        {
            var playerPos = Svc.ClientState.LocalPlayer?.Position ?? new();
            return Svc.Objects.Any(o => o.ObjectKind == ObjectKind.BattleNpc &&
                                    (BattleNpcSubKind)o.SubKind == BattleNpcSubKind.Enemy &&
                                    GameObjectIsTargetable(o) &&
                                    PointInCone(o.Position - Svc.ClientState.LocalPlayer.Position, Svc.ClientState.LocalPlayer.Rotation, 0 + (o.HitboxRadius / 2)) &&
                                    PointInCircle(o.Position - playerPos, Config.MaxDistance + o.HitboxRadius));
        }

        public IGameObject NearestConeTarget()
        {
            if (CanConeAoe())
            {
                var playerPos = Svc.ClientState.LocalPlayer?.Position ?? new();
                var target = Svc.Objects.OrderBy(GameObjectHelper.GetTargetDistance).First(o => o.ObjectKind == ObjectKind.BattleNpc &&
                                                (BattleNpcSubKind)o.SubKind == BattleNpcSubKind.Enemy &&
                                                GameObjectIsTargetable(o) &&
                                                PointInCone(o.Position - Svc.ClientState.LocalPlayer.Position, Svc.ClientState.LocalPlayer.Rotation, 0 + (o.HitboxRadius / 2)) &&
                                                PointInCircle(o.Position - playerPos, Config.MaxDistance + o.HitboxRadius));

                return target;
            }

            return null;
        }

        public static unsafe FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* GameObjectInternal(IGameObject obj)
        {
            return (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj?.Address;
        }
        public static unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* BattleCharaInternal(IBattleChara chara)
        {
            return (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)chara?.Address;
        }

        public static unsafe bool GameObjectIsTargetable(IGameObject obj)
        {
            return GameObjectInternal(obj)->GetIsTargetable();
        }

        public static unsafe bool GameObjectIsDead(IGameObject obj)
        {
            return GameObjectInternal(obj)->IsDead();
        }

        public static bool PointInCone(Vector3 offsetFromOrigin, Vector3 direction, float halfAngle)
        {
            return Vector3.Dot(Vector3.Normalize(offsetFromOrigin), direction) >= MathF.Cos(halfAngle);
        }
        public static bool PointInCone(Vector3 offsetFromOrigin, float direction, float halfAngle)
        {
            return PointInCone(offsetFromOrigin, DirectionToVec3(direction), halfAngle);
        }

        public static Vector3 DirectionToVec3(float direction)
        {
            return new(MathF.Sin(direction), 0, MathF.Cos(direction));
        }

        public static bool PointInCircle(Vector3 offsetFromOrigin, float radius)
        {
            return offsetFromOrigin.LengthSquared() <= radius * radius;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            ImGui.PushItemWidth(300);
            if (ImGui.SliderFloat("Max Distance (yalms)", ref Config.MaxDistance, 0.1f, 30f, "%.1f")) hasChanged = true;

            if (ImGui.RadioButton("Don't Unset Target", !Config.UnsetTargetRange && !Config.UnsetTargetCombat))
            {
                Config.UnsetTargetRange = false;
                Config.UnsetTargetCombat = false;
                hasChanged = true;
            }
            if (ImGui.RadioButton("Unset Target if not in cone range (all times, use with caution)", Config.UnsetTargetRange))
            {
                Config.UnsetTargetRange = true;
                Config.UnsetTargetCombat = false;
                hasChanged = true;
            }
            if (ImGui.RadioButton("Unset Target if not in cone range (only in combat)", Config.UnsetTargetCombat))
            {
                Config.UnsetTargetRange = false;
                Config.UnsetTargetCombat = true;
                hasChanged = true;
            }
        };
    }
}
