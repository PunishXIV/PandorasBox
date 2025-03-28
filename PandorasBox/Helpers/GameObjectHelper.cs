using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using System;
using System.Numerics;

namespace PandorasBox.Helpers
{
    internal static class GameObjectHelper
    {
        public static float GetTargetDistance(IGameObject target)
        {
            if (target is null || Svc.ClientState.LocalPlayer is null)
                return 0;

            if (target.GameObjectId == Svc.ClientState.LocalPlayer.GameObjectId)
                return 0;

            Vector3 position = new(target.Position.X, target.Position.Z, target.Position.Y);
            Vector3 selfPosition = new(Player.Position.X, Player.Position.Z, Player.Position.Y);

            return Math.Max(0, Vector3.Distance(position, selfPosition) - target.HitboxRadius - Svc.ClientState.LocalPlayer.HitboxRadius);
        }

        public static float GetHeightDifference(IGameObject target)
        {
            var dist = Svc.ClientState.LocalPlayer!.Position.Y - target.Position.Y;
            if (dist < 0)
                dist *= -1;

            return dist;
        }
    }
}
