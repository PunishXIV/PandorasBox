using ECommons.DalamudServices;

namespace PandorasBox.FeaturesSetup;

internal static class Events {
    public static void Init() {
        Svc.Framework.Update += UpdateEvents;
    }

    public static void Disable() {
        Svc.Framework.Update -= UpdateEvents;
    }

    private static void UpdateEvents(IFramework framework) {
        if (Svc.Objects.LocalPlayer is null) return;
        JobID = Svc.Objects.LocalPlayer.ClassJob.RowId;
    }

    public static uint? JobID {
        get;
        set {
            if (value != null && field != value) {
                field = value;
                Svc.Log.Debug($"Job changed to {value}");
                OnJobChanged?.Invoke(value);
            }
        }
    }

    public delegate void OnJobChangeDelegate(uint? jobId);
    public static event OnJobChangeDelegate? OnJobChanged;

}
