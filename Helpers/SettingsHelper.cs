using Serilog;
using Serilog.Context;
using Windows.Storage;

public static class SettingsHelper
{
    private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    // 保存设置
    public static void SaveSetting(string key, object value)
    {
        localSettings.Values[key] = value;
        using (LogContext.PushProperty("Module", nameof(SettingsHelper)))
        {
            Log.Debug("Setting saved: {Key} = {Value}", key, value);
        }
    }

    // 读取设置
    public static T GetSetting<T>(string key, T defaultValue = default)
    {
        using (LogContext.PushProperty("Module", nameof(SettingsHelper)))
        {
            if (localSettings.Values.TryGetValue(key, out object? value))
            {
                Log.Debug("Setting retrieved: {Key} = {Value}", key, value);
                return (T)value;
            }
            Log.Debug("Setting not found: {Key}, returning default value: {DefaultValue}", key, defaultValue);
            return defaultValue;
        }
    }

    // 删除设置
    public static void RemoveSetting(string key)
    {
        using (LogContext.PushProperty("Module", nameof(SettingsHelper)))
        {
            if (localSettings.Values.ContainsKey(key))
            {
                Log.Debug("Setting removed: {Key}", key);
                localSettings.Values.Remove(key);
            }
            else
            {
                Log.Debug("Setting not found for removal: {Key}", key);
            }
        }
    }

    // 检查设置是否存在
    public static bool HasSetting(string key)
    {
        return localSettings.Values.ContainsKey(key);
    }
}