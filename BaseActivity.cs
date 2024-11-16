using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AndroidX.Preference;
using Google.Android.Material.Color;
using System;

namespace com.companyname.navigationgraph8net8
{
    [Activity(Label = "BaseActivity")]
    public class BaseActivity : AppCompatActivity
    {
        protected ISharedPreferences? sharedPreferences;

        private bool useDynamicColors;
        private bool useTransparentStatusBar;
        private string? requestedColorTheme;

        #region OnCreate
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Sets whether the decor view should fit root-level content views for WindowInsetsCompat.
            // In other words - 
            // The single argument controls whether or not our layout will fit inside the system windows (if true), or be draw behind them (if false). 
            WindowCompat.SetDecorFitsSystemWindows(Window!, false);

            // This rather than android:windowTranslucentStatus in styles seems to have fixed the problem with the OK button on the BasicDialogFragment
            // It also fixes the AppBarlayout so it extends full screen, when devicesWithNotchesAllowFullScreen = true; 
            // Comment this out to see the result of the AppBarLayout. Now changed to SetDecorFitsSystemWindows
            
            // From the above testing, it is quite obvious WindowManagerFlags.TranslucentStatus does much more than make the status bar translucent.
            // It does as their new documentation states, it automatically sets the already deprecated(deprecated by android 11) system UI visibility flags.
            // View#SYSTEM_UI_FLAG_LAYOUT_STABLE and View#SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN.
            //Window.AddFlags(WindowManagerFlags.TranslucentStatus);

            sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);

            // colorThemeValue defaults to RedBmw
            requestedColorTheme = sharedPreferences!.GetString("colorThemeValue", "3");
            useDynamicColors = sharedPreferences.GetBoolean("use_dynamic_colors", false);
            useTransparentStatusBar = sharedPreferences.GetBoolean("use_transparent_statusbar", false);

            //if (Build.VERSION.SdkInt >= BuildVersionCodes.S && useDynamicColors)
            if (OperatingSystem.IsAndroidVersionAtLeast(31) & useDynamicColors) 
            {
                SetAppTheme(requestedColorTheme!);
                // Work around for not working with SplashScreen Api in the Application class.
                DynamicColors.ApplyToActivityIfAvailable(this);
                SetStatusBarColor();
            }
            else
                SetAppTheme(requestedColorTheme!);

        }
        #endregion

        #region SetAppTheme
        private void SetAppTheme(string requestedColorTheme)
        {

            if (requestedColorTheme == "1")
                SetTheme(Resource.Style.Theme_NavigationGraph_RedBmw);
            else if (requestedColorTheme == "2")
                SetTheme(Resource.Style.Theme_NavigationGraph_BlueAudi);
            else if (requestedColorTheme == "3")
                SetTheme(Resource.Style.Theme_NavigationGraph_GreenBmw);

            SetStatusBarColor();
        }
        #endregion

        #region SetStatusBarColor
        private void SetStatusBarColor()
        {
            if (!IsNightModeActive())
            {
                TypedValue typedValue = new();
                Theme!.ResolveAttribute(Resource.Attribute.colorSecondary, typedValue, true);
                int color = ContextCompat.GetColor(this, typedValue.ResourceId);
                //0 - 255 e.g. 204-> 80 % transparent - will work even if the alpha component of the colour is already set e.g.to opaque FF.See chart below
               Window!.SetStatusBarColor(new Color(useTransparentStatusBar ? ColorUtils.SetAlphaComponent(color, 204) : color));
            }
            else
                Window!.SetStatusBarColor(new Color(Color.Black));   // Don't like the desaturated colour for statusbar with a Dark Theme 
        }
        #endregion

        #region IsNightModeActive
        private bool IsNightModeActive()
        {
            UiMode currentNightMode = Resources!.Configuration!.UiMode & UiMode.NightMask;
            return currentNightMode == UiMode.NightYes;
        }
        #endregion

        #region Notes - Table for translucent colors
        //100% — FF - 255
        //95%  — F2 - 242
        //90%  — E6 - 230
        //85%  — D9 - 217
        //80%  — CC - 204
        //75%  — BF - 191
        //70%  — B3 - 179
        //65%  — A6 - 166
        //60%  — 99 - 153
        //55%  — 8C - 140
        //50%  — 80 - 128
        //45%  — 73 - 115
        //40%  — 66 - 102
        //35%  — 59 - 89
        //30%  — 4D - 77
        //25%  — 40 - 64
        //20%  — 33 - 51
        //15%  — 26 - 38
        //10%  — 1A - 26
        //5%   — 0D - 13
        //0%   — 00 - 0
        #endregion


    }
}
