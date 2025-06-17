using Windows.Storage;

public static class SettingsHelper
{
    private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    // 保存设置
    public static void SaveSetting(string key, object value)
    {
        localSettings.Values[key] = value;
    }

    // 读取设置
    public static T GetSetting<T>(string key, T defaultValue = default)
    {
        if (localSettings.Values.TryGetValue(key, out object? value))
        {
            return (T)value;
        }
        return defaultValue;
    }

    // 删除设置
    public static void RemoveSetting(string key)
    {
        if (localSettings.Values.ContainsKey(key))
        {
            localSettings.Values.Remove(key);
        }
    }

    // 检查设置是否存在
    public static bool HasSetting(string key)
    {
        return localSettings.Values.ContainsKey(key);
    }
}