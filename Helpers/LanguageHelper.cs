namespace SpotlightGallery.Helpers
{
    public enum DisplayLanguageOption
    {
        Chinese,
        English
    }
    public static class LanguageHelper
    {
        public static void ApplyDisplayLanguage()
        {
            int langIndex = SettingsHelper.GetSetting("DisplayLanguage", 0);
            var langOption = (DisplayLanguageOption)langIndex;
            switch (langOption)
            {
                case DisplayLanguageOption.Chinese:
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
                    break;
                case DisplayLanguageOption.English:
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                    break;
                default:
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                    break;
            }
        }
    }
}