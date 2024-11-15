using ECommons.DalamudServices;

namespace PandorasBox.FeaturesSetup
{
    internal static class Events
    {
        private static uint? jobID;

        public static void Init()
        {
            Svc.Framework.Update += UpdateEvents;
        }

        public static void Disable()
        {
            Svc.Framework.Update -= UpdateEvents;
        }

        private static void UpdateEvents(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            JobID = Svc.ClientState.LocalPlayer.ClassJob.RowId;
        }

        public static uint? JobID
        {
            get => jobID;
            set
            {
                if (value != null && jobID != value)
                {
                    jobID = value;
                    Svc.Log.Debug($"Job changed to {value}");
                    OnJobChanged?.Invoke(value);
                }
            }
        }

        public delegate void OnJobChangeDelegate(uint? jobId);
        public static event OnJobChangeDelegate? OnJobChanged;

    }
}
