using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Domain.Settings;

// The numbers behind sign-in protection, sessions, access reviews and device health (the Security page).
// Defaults match the design: 5 failures lock an account for 15 minutes, two-step is required for controllers and administrators.
public sealed record SecuritySettings(
    int LockAfterFailures = 5,
    int LockMinutes = 15,
    bool TwoStepRequired = true,
    int ConsoleIdleMinutes = 15,
    int DriverAppLockMinutes = 5,
    int AccessReviewDays = 90,
    bool FlagLeavers = true,
    int DeviceLateAfterSeconds = 60,
    int DeviceOfflineAfterSeconds = 90)
{
    public static SecuritySettings Default { get; } = new();

    public Result Validate()
    {
        if (LockAfterFailures is < 3 or > 20)
            return IdentityErrors.SettingInvalid("LockAfterFailures", "Between 3 and 20 attempts.");
        if (LockMinutes is < 1 or > 1440)
            return IdentityErrors.SettingInvalid("LockMinutes", "Between 1 minute and 24 hours.");
        if (ConsoleIdleMinutes is < 5 or > 120)
            return IdentityErrors.SettingInvalid("ConsoleIdleMinutes", "Between 5 and 120 minutes.");
        if (DriverAppLockMinutes is < 1 or > 60)
            return IdentityErrors.SettingInvalid("DriverAppLockMinutes", "Between 1 and 60 minutes.");
        if (AccessReviewDays is < 30 or > 365)
            return IdentityErrors.SettingInvalid("AccessReviewDays", "Between 30 and 365 days.");
        if (DeviceLateAfterSeconds is < 30 or > 600 || DeviceOfflineAfterSeconds <= DeviceLateAfterSeconds || DeviceOfflineAfterSeconds > 1800)
            return IdentityErrors.SettingInvalid("DeviceOfflineAfterSeconds", "Late after 30 to 600 seconds; offline must be later than late.");

        return Result.Success();
    }
}
