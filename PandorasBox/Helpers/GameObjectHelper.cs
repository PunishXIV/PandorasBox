using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using System;
using System.Numerics;

namespace PandorasBox.Helpers
{
    internal static class GameObjectHelper
    {
        public static float GetTargetDistance(GameObject target)
        {
            if (target is null || Svc.ClientState.LocalPlayer is null)
                return 0;

            if (target.ObjectId == Svc.ClientState.LocalPlayer.ObjectId)
                return 0;

            Vector2 position = new(target.Position.X, target.Position.Z);
            Vector2 selfPosition = new(Svc.ClientState.LocalPlayer.Position.X, Svc.ClientState.LocalPlayer.Position.Z);

            return Math.Max(0, Vector2.Distance(position, selfPosition) - target.HitboxRadius - Svc.ClientState.LocalPlayer.HitboxRadius);
        }

        public static float GetHeightDifference(GameObject target)
        {
            var dist = Svc.ClientState.LocalPlayer.Position.Y - target.Position.Y;
            if (dist < 0)
                dist *= -1;

            return dist;
        }
    }
}
