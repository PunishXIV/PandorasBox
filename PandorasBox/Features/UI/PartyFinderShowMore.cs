using Dalamud.Hooking;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.UI
{
    public unsafe class PartyFinderShowMore : Feature
    {
        public override string Name => "Party Finder Show More";

        public override string Description => "Raise the display limit from 50 to the 100 limit actually allowed by the game.";

        public override FeatureType FeatureType => FeatureType.UI;

        private delegate char? PartyFinderDelegate(long a1, int a2);
        private Hook<PartyFinderDelegate>? partyFinderHook;

        private char? PartyFinderDetour(long a1, int a2)
        {
            Marshal.WriteInt16(new nint(a1 + 1128), 100);
            return partyFinderHook?.Original(a1, a2);
        }

        public override void Enable()
        {
            partyFinderHook ??= Svc.Hook.HookFromSignature<PartyFinderDelegate>("48 89 5c 24 ?? 55 56 57 48 ?? ?? ?? ?? ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 48 ?? ?? 48 89 85 ?? ?? ?? ?? 48 ?? ?? 0f", new PartyFinderDelegate(PartyFinderDetour));
            partyFinderHook?.Enable();
            base.Enable();
        }

        public override void Disable()
        {
            partyFinderHook?.Disable();
            base.Disable();
        }

        public override void Dispose()
        {
            partyFinderHook?.Dispose();
            base.Dispose();
        }
    }
}
