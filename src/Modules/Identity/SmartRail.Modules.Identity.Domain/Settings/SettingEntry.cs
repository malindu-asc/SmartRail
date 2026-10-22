namespace SmartRail.Modules.Identity.Domain.Settings;

// One stored setting: a key and its text value (the Security page reads and writes these).
public sealed class SettingEntry
{
    private SettingEntry() { }

    public SettingEntry(string key, string value, string updatedBy, DateTimeOffset now)
    {
        Key = key;
        Value = value;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string UpdatedBy { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Change(string value, string updatedBy, DateTimeOffset now)
    {
        Value = value;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }
}
