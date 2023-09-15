using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Preference;
using System;

namespace com.companyname.navigationgraph8net8.Fragments
{
    public class SettingsFragment : PreferenceFragmentCompat
    {
        private ISharedPreferences? sharedPreferences;
        private ColorThemeListPreference? colorThemeListPreference;
        private SystemThemeListPreference? systemThemeListPreference;

        #region OnCreatePreferences
        public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Activity!);

            SetPreferencesFromResource(Resource.Xml.preferences, rootKey);
            
            if (PreferenceScreen!.FindPreference("colorThemeValue") is ColorThemeListPreference colorThemeListPreference)
            {
                colorThemeListPreference.Init();
                colorThemeListPreference.PreferenceChange += ColorThemeListPreference_PreferenceChange;
            }

            if (PreferenceScreen.FindPreference("systemThemeValue") is SystemThemeListPreference systemThemeListPreference)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(31)) /*if (Build.VERSION.SdkInt >= BuildVersionCodes.S)*/ //Either here is ok
                {
                    systemThemeListPreference.Init();
                    systemThemeListPreference.PreferenceChange += SystemThemeListPreference_PreferenceChange;
                }
                else
                    systemThemeListPreference.Enabled = false;
            }

            if (PreferenceScreen.FindPreference("darkTheme") is CheckBoxPreference checkboxDarkThemePreference)
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
                    checkboxDarkThemePreference.PreferenceChange += CheckboxDarkThemePreference_PreferenceChange;
                else
                    checkboxDarkThemePreference.Enabled = false;
            }

            if (PreferenceScreen.FindPreference("use_dynamic_colors") is CheckBoxPreference checkboxDynamicColors)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(31)) /*if (Build.VERSION.SdkInt >= BuildVersionCodes.S)*/ //Either here is ok
                    checkboxDynamicColors.PreferenceChange += CheckboxDynamicColors_PreferenceChange;
                else
                    checkboxDynamicColors.Enabled = false;
            }

            if (PreferenceScreen.FindPreference("use_transparent_statusbar") is CheckBoxPreference checkboxTransparentStausBar)
                checkboxTransparentStausBar.PreferenceChange += CheckboxTransparentStausBar_PreferenceChange;


            if (PreferenceScreen.FindPreference("devicesWithNotchesAllowFullScreen") is CheckBoxPreference checkboxDevicesWithNotchesAllFullScreen)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(29))  /*if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)*/ //Either here is ok
                    checkboxDevicesWithNotchesAllFullScreen.PreferenceChange += CheckboxDevicesWithNotchesAllFullScreen_PreferenceChange;
                else
                    checkboxDevicesWithNotchesAllFullScreen.Enabled = false;
            }

        }
        #endregion

        #region CheckboxDarkThemePreference_PreferenceChange
        private void CheckboxDarkThemePreference_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {
            //bool requestedNightMode = (bool)e.NewValue;
            //ISharedPreferencesEditor editor = sharedPreferences.Edit();
            //editor.PutBoolean("darkTheme", requestedNightMode).Apply();
            //editor.Commit();

            bool requestedNightMode = (bool)e.NewValue!;
            ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            editor!.PutBoolean("darkTheme", requestedNightMode!)!.Apply();
            editor.Commit();

            // This is only available to devices running less than Android 10.
            SetDefaultNightMode(requestedNightMode);
        }
        #endregion

        #region CheckboxDynamicColors_PreferenceChange
        private void CheckboxDynamicColors_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {
            //bool useDynamicColors = (bool)e.NewValue;
            //ISharedPreferencesEditor editor = sharedPreferences.Edit();
            //editor.PutBoolean("use_dynamic_colors", useDynamicColors).Apply();
            //editor.Commit();

            bool useDynamicColors = (bool)e.NewValue!;
            ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            editor!.PutBoolean("use_dynamic_colors", useDynamicColors)!.Apply();
            editor.Commit();

            Activity!.Recreate();
        }
        #endregion

        #region CheckboxDevicesWithNotchesAllFullScreen_PreferenceChange
        private void CheckboxDevicesWithNotchesAllFullScreen_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {  
            //bool requestedMode = (bool)e.NewValue;
            //ISharedPreferencesEditor editor = sharedPreferences.Edit();
            //editor.PutBoolean("devicesWithNotchesAllowFullScreen", requestedMode).Apply();
            //editor.Commit();

            bool requestedMode = (bool)e.NewValue!;
            ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            editor!.PutBoolean("devicesWithNotchesAllowFullScreen", requestedMode)!.Apply();
            editor.Commit();

            Activity!.Recreate();
        }
        #endregion

        #region CheckboxTransparentStausBar_PreferenceChange
        private void CheckboxTransparentStausBar_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {
            bool useTransparentStatusBar = (bool)e.NewValue!;
            ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            editor!.PutBoolean("use_transparent_statusbar", useTransparentStatusBar)!.Apply();
            editor.Commit();
            
            Activity!.Recreate();
        }
        #endregion

        #region ColorThemeListPreference_PreferenceChange
        private void ColorThemeListPreference_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {
            colorThemeListPreference = e.Preference as ColorThemeListPreference;

            // Working nullable code - the long way
            //ISharedPreferences? sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Activity!);
            //ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            //editor!.PutString("colorThemeValue", e.NewValue!.ToString());
            //editor.Apply();

            ISharedPreferencesEditor? editor = colorThemeListPreference!.SharedPreferences!.Edit();
            editor!.PutString("colorThemeValue", e.NewValue!.ToString())!.Apply();
            editor.Commit();

            int index = Convert.ToInt16(e.NewValue.ToString());
            string colorThemeValue = colorThemeListPreference!.GetEntries()![index - 1];
            colorThemeListPreference.Summary = (index != -1) ? colorThemeValue : colorThemeListPreference.DefaultThemeValue;

            // Must now force the theme to change - see BaseActivity. It's OnCreate checks the sharedPreferences, get the string currentTheme and passes that value to SetAppTheme(currentTheme)
            // which checks to see if it has changed and if so calls SetTheme which the correct Resource.Style.Theme_Name)
            Activity!.Recreate();
        }
        #endregion

        #region SystemThemeListPreference_PreferenceChange
        private void SystemThemeListPreference_PreferenceChange(object? sender, Preference.PreferenceChangeEventArgs e)
        {
            systemThemeListPreference = e.Preference as SystemThemeListPreference;

            ISharedPreferencesEditor? editor = systemThemeListPreference!.SharedPreferences!.Edit();
            editor!.PutString("systemThemeValue", e.NewValue!.ToString())!.Apply();
            editor.Commit();

            int index = Convert.ToInt16(e.NewValue.ToString());
            string systemThemeValue = systemThemeListPreference.GetEntries()![index - 1];
            systemThemeListPreference.Summary = (index != -1) ? systemThemeValue : systemThemeListPreference.DefaultSystemThemeValue;

            // Only available on devices running Android 12+

            // Note we subtract 1 from the index - See SystemThemeListPreference
            // Equivelent to UiNightMode.Auto, No and Yes, we manipulated it by subtracting 1 to match 0,1,2 instead of 1,2,3 as in SystemThemeListPreference

            UiNightMode uiNightMode = (UiNightMode)index-1;     
            SetDefaultNightMode12(uiNightMode);
        }
        #endregion

        #region SetDefaultNightMode12
        private void SetDefaultNightMode12(UiNightMode uiNightMode)
        {
            // Sets and persists the night mode setting for this app. This allows the system to know
            // if the app wants to be displayed in dark mode before it launches so that the splash
            // screen can be displayed accordingly.
            // You don't need to do this if your app doesn't provide in-app dark mode setting. e.g. System Default, Light, Dark.
            // UiModeService
            // You could call overriding the Quick Settings Day/Night Theme button. 
            // In other words the user can select to override whatever the theme button is set to when on an Android 12+ device.

            UiModeManager? uiModeManager = Activity!.GetSystemService(Context.UiModeService) as UiModeManager;
            if (OperatingSystem.IsAndroidVersionAtLeast(31))    /* Build.VERSION.SdkInt >= BuildVersionCodes.S */   //Must use OperatingSystem.IsAndroidVersionAtLeast(xx) otherwise a warning  
                uiModeManager!.SetApplicationNightMode((int)uiNightMode);  // Only avaialable on Android 12 -API31 -S and above.

            ISharedPreferencesEditor? editor = sharedPreferences!.Edit();
            editor!.PutInt("night_mode", (int)uiNightMode)!.Apply();
            editor.Commit();
        }
        #endregion

        #region SetDefaultNightMode
        private static void SetDefaultNightMode(bool requestedNightMode)
        {
            AppCompatDelegate.DefaultNightMode = requestedNightMode ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightNo;
        }
        #endregion
       
    }
}