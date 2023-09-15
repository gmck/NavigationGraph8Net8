﻿using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;
using AndroidX.Navigation;
using AndroidX.Navigation.Fragment;
using AndroidX.Navigation.UI;
using com.companyname.navigationgraph8net8.Dialogs;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.companyname.navigationgraph8net8
{

    //https://proandroiddev.com/handling-back-press-in-android-13-the-correct-way-be43e0ad877a

    [Activity(Label = "@string/app_name", MainLauncher = true)]  //Theme = "@style/Theme.NavigationGraph.RedBmw", not required here - handled by postSplashScreenTheme, see Styles.xml
    public class MainActivity : BaseActivity, IOnApplyWindowInsetsListener,
                                NavController.IOnDestinationChangedListener,
                                NavigationView.IOnNavigationItemSelectedListener
    {
        private const int BLUETOOTH_PERMISSIONS_REQUEST_CODE = 9999;

        private readonly string logTag = "Nav-MainActivity";

        private AppBarConfiguration? appBarConfiguration;
        private NavigationView? navigationView;
        private DrawerLayout? drawerLayout;
        private BottomNavigationView? bottomNavigationView;
        private NavController? navController;
        private MaterialToolbar? toolbar;

        // Just added for the Bluetooth permissions example.
        internal BluetoothAdapter? bluetoothAdapter = null;
        internal ActivityResultCallback? activityResultCallback;
        internal ActivityResultLauncher? bluetoothEnablerResultLauncher;

        // Preference variables - see OnDestinationChanged where they are checked
        private bool devicesWithNotchesAllowFullScreen;             // allow full screen for devices with notches
        private bool animateFragments;                              // animate fragments 
        private bool resetHelperExplanationDialogs;
        
        // The following fragment(s) are immersive fragments - see SetShortEdgesIfRequired
        private List<int>? immersiveFragmentsDestinationIds;

        
        // Just to demonstrate the use the SnackBarHelper class - make it true and you will see a SnackBar when the app starts.
        private readonly bool showSnackBar = true;   


        #region OnCreate
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            _ = AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
            
            base.OnCreate(savedInstanceState);

            // System.Threading.Thread.Sleep(500). Only use for demonstration purposes in that during the SplashScrren you can easily observe the background color of the launch icon. Remove for production build.

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Require a toolbar
            toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // navigationView, bottomNavigationView for NavigationUI and drawerLayout for the AppBarConfiguration and NavigationUI
            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            bottomNavigationView = FindViewById<BottomNavigationView>(Resource.Id.bottom_nav);

            // NavHostFragment so we can get a NavController 
            NavHostFragment? navHostFragment = SupportFragmentManager.FindFragmentById(Resource.Id.nav_host) as NavHostFragment;
            navController = navHostFragment!.NavController;

            // These are the fragments that you don't wont the back button of the toolbar to display on e.g. topLevel fragments. They correspond to the items of the NavigationView.
            int[] topLevelDestinationIds = new int[] { Resource.Id.home_fragment, Resource.Id.gallery_fragment, Resource.Id.slideshow_fragment, Resource.Id.widgets_fragment, Resource.Id.purchase_fragment };
            appBarConfiguration = new AppBarConfiguration.Builder(topLevelDestinationIds).SetOpenableLayout(drawerLayout).Build();  // SetDrawerLayout replaced with SetOpenableLayout

            // The following fragments are immersive fragments - see SetShortEdgesIfRequired
            immersiveFragmentsDestinationIds = new List<int> { Resource.Id.race_result_fragment };

            NavigationUI.SetupActionBarWithNavController(this, navController, appBarConfiguration);

            navigationView!.SetNavigationItemSelectedListener(this);
            bottomNavigationView!.ItemSelected += BottomNavigationView_ItemSelected!;

            ViewCompat.SetOnApplyWindowInsetsListener(toolbar!, this);
            ViewCompat.SetOnApplyWindowInsetsListener(drawerLayout!, this);
        
            // Add the DestinationChanged listener
            navController.AddOnDestinationChangedListener(this);


            // Added 13/06/2023 for the Bluetooth permissions example.
            BluetoothManager? manager = Application.Context.GetSystemService(Application.BluetoothService) as BluetoothManager;
            bluetoothAdapter = manager!.Adapter;

            activityResultCallback = new ActivityResultCallback();
            activityResultCallback!.OnActivityResultCalled += ActivityResultCallback_ActivityResultCalled;
            bluetoothEnablerResultLauncher = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), activityResultCallback);
        }
        #endregion

        #region OnApplyWindowInsets
        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            AndroidX.Core.Graphics.Insets statusBarsInsets = insets.GetInsets(WindowInsetsCompat.Type.StatusBars());
            AndroidX.Core.Graphics.Insets navigationBarsInsets = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());

            if (v is MaterialToolbar)
            {
                SetTopMargin(v, statusBarsInsets);

                // Appear never to need displayCutout because it is always null
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    if (insets.DisplayCutout != null)
                        Window!.Attributes!.LayoutInDisplayCutoutMode = devicesWithNotchesAllowFullScreen ? LayoutInDisplayCutoutMode.ShortEdges : LayoutInDisplayCutoutMode.Default;
                }
            }
            else if (v is DrawerLayout)
                SetLeftMargin(v, navigationBarsInsets);
                

            return insets;
        }
        #endregion

        #region OnResume
        protected override void OnResume()
        {
            base.OnResume();

            if (showSnackBar)
                ShowWelcomeMessage();
        }
        #endregion

        #region OnSaveInstanceState
        protected override void OnSaveInstanceState(Bundle outState)
        {
            // See earlier comment today in OnDestroy, therefore since OnSaveInstanceState is called before OnDestroy, we can call Finish() here.
            // This eliminates needing to use the Application.IActivityLifecycleCallbacks in NavigationGraph8ApplicationNet8 class, which causes an additional call to OnActivityDestroyed that isn't required and is picked up by Android in Logcat.
            // This is more straight forward. We will leave the Application.IActivityLifecycleCallbacks code in NavigationGraph8ApplicationNet8 for now but since it is not required, we have commented out RegisterActivityLifecycleCallbacks(this)
            // and UnregisterActivityLifecycleCallbacks(this). Result:- the Predictive BackGesture is working, plus the Service is correctly shut down on exit.

            base.OnSaveInstanceState(outState);

            if (!IsChangingConfigurations)
            {
                Finish();
                Log.Debug(logTag, logTag + " OnSaveInstanceState - Calling Finish()");
            }
        }
        #endregion

        #region OnDestroy
        protected override void OnDestroy()
        {
            // Never enters OnDestroy unless the device is rotated. Not even when exiting the app. Tested again 16/08/2023 and confirmed.
            // It will come in here if OnActivitySaveIntanceState calls Finish() so IsFinishing will be true in that situation.
            // OnActivityDestroyed will only be called if OnActivitySaveInstanceState calls Finish(). So effectively OnActivityDestroyed since we already called Finish() does not require any code in its body
            // See new comments 16/08/2023 above in OnSaveInstanceState.

            base.OnDestroy();

            if (IsFinishing)
            {
                Log.Debug(logTag, logTag + " OnDestroy IsFinishing is " + IsFinishing.ToString());
                StopService();
            }
        }
        #endregion

        #region SetMargins
        private static void SetTopMargin(View v, AndroidX.Core.Graphics.Insets insets)
        {
            ViewGroup.MarginLayoutParams marginLayoutParams = (ViewGroup.MarginLayoutParams)v!.LayoutParameters!;
            //Log.Debug(logTag, "MainActivity - marginLayoutParams.LeftMargin " + marginLayoutParams.LeftMargin.ToString());
            marginLayoutParams.LeftMargin = marginLayoutParams.LeftMargin;
            marginLayoutParams.TopMargin = insets.Top;          // top is all we are concerned with - this will position the toolbar insets.Top from the top of the screen
            marginLayoutParams.RightMargin = marginLayoutParams.RightMargin;
            marginLayoutParams.BottomMargin = marginLayoutParams.BottomMargin;
            v.LayoutParameters = marginLayoutParams;
            v.RequestLayout();
        }

        private static void SetLeftMargin(View v, AndroidX.Core.Graphics.Insets insets)
        {
            ViewGroup.MarginLayoutParams marginLayoutParams = (ViewGroup.MarginLayoutParams)v!.LayoutParameters!;
            marginLayoutParams.LeftMargin = insets.Left;
            marginLayoutParams.TopMargin = marginLayoutParams.TopMargin;
            marginLayoutParams.RightMargin = insets.Right;// marginLayoutParams.RightMargin;
            marginLayoutParams.BottomMargin = marginLayoutParams.BottomMargin;
            v.LayoutParameters = marginLayoutParams;
            v.RequestLayout();
        }
        // Make a single function for setting Margins
        #endregion

        #region OnNavigationItemSelected
        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            // Using Fader2 as the default as animateFragment is false by default - check AnimationResource.cs for different animations
            if (!animateFragments)
                AnimationResource.Fader3();
            else
                AnimationResource.Slider();

            NavOptions navOptions = new NavOptions.Builder()
                    .SetLaunchSingleTop(true)
                    .SetEnterAnim(AnimationResource.EnterAnimation)
                    .SetExitAnim(AnimationResource.ExitAnimation)
                    .SetPopEnterAnim(AnimationResource.PopEnterAnimation)
                    .SetPopExitAnim(AnimationResource.PopExitAnimation)
                    .SetPopUpTo(Resource.Id.home_fragment, false)     // Inclusive false, saveState true. saved state doesn't change anything.
                    .Build();

            bool proceed = false;

            switch (menuItem.ItemId)
            {
                // These are all topLevel fragments
                // Add fragment classes and fragment layouts as we add to the codebase as per the NavigationView items. 
                // If any classes and layouts are missing, then the NavigationView will not update the item selected.
                // The menuitem highlight will stay on the current item and the current fragment will remain displayed, nor will the app crash.
                case Resource.Id.home_fragment:
                case Resource.Id.gallery_fragment:
                case Resource.Id.slideshow_fragment:
                case Resource.Id.widgets_fragment:
                case Resource.Id.purchase_fragment:
                    proceed = true;
                    break;

                default:
                    break;
            }
            // We have the option here of animating our toplevel destinations. If we don't want animation comment out the NavOptions. 
            bool handled = false;
            if (proceed)
            {
                navController!.Navigate(menuItem.ItemId, null, navOptions);
                handled = true;
            }

            if (drawerLayout!.IsDrawerOpen(GravityCompat.Start))
                drawerLayout.CloseDrawer(GravityCompat.Start);

            return handled;

        }
        #endregion

        #region BottomNavigationViewItemSelected
        private void BottomNavigationView_ItemSelected(object sender, NavigationBarView.ItemSelectedEventArgs e)
        {
            // Note NavigationBarView - not BottomNavigationView probably not what is expected. Note that BottomNavigationView inherits from NavigationBarView. See notes in NavigationGraph.docx
            if (!animateFragments)
                AnimationResource.Fader3();
            else
                AnimationResource.Slider();

            NavOptions navOptions = new NavOptions.Builder()
                    .SetLaunchSingleTop(true)
                    .SetEnterAnim(AnimationResource.EnterAnimation)
                    .SetExitAnim(AnimationResource.ExitAnimation)
                    .SetPopEnterAnim(AnimationResource.PopEnterAnimation)
                    .SetPopExitAnim(AnimationResource.PopExitAnimation)          
                    .SetPopUpTo(Resource.Id.slideshow_fragment, false)  // No .SetPopUpTo(Resource.Id.home_fragment, false, true) here - because here we go back tothe slideShow fragment
                    .Build();
            
            Log.Debug(logTag, "Navigate to - Enter Animation " + navOptions.EnterAnim.ToString());
            Log.Debug(logTag, "Navigate to - Exit Animation " + navOptions.ExitAnim.ToString());
            Log.Debug(logTag, "Navigate to - Pop Enter Animation " + navOptions.PopEnterAnim.ToString());
            Log.Debug(logTag, "Navigate to - Pop Exit Animation " + navOptions.PopExitAnim.ToString());

            bool proceed = false;

            switch (e.Item.ItemId)
            {
                case Resource.Id.leaderboardpager_fragment:
                case Resource.Id.register_fragment:
                case Resource.Id.race_result_fragment:
                    proceed = true;
                    break;

                default:
                    break;
            }
            if (proceed)
                navController!.Navigate(e.Item.ItemId, null, navOptions);

        }
        #endregion

        #region OnSupportNavigationUp
        public override bool OnSupportNavigateUp()
        {
            return NavigationUI.NavigateUp(navController!, appBarConfiguration!) || base.OnSupportNavigateUp();
        }
        #endregion

        #region OnDestinationChanged
        public void OnDestinationChanged(NavController navController, NavDestination navDestination, Bundle? bundle)
        {
            CheckForPreferenceChanges();

            // The first menu item is not checked by default, so we need to check it to show it is selected on the startDestination fragment, i.e. the home_fragment
            navigationView!.Menu!.FindItem(Resource.Id.home_fragment)!.SetChecked(navDestination.Id == Resource.Id.home_fragment);

            // The slideshowFragment contains a BottomNavigationView. We only want to show the BottomNavigationView when the SlideshowFragment is displayed.
            bottomNavigationView!.Visibility = navDestination.Id == Resource.Id.slideshow_fragment ? ViewStates.Visible : ViewStates.Gone;

            // By default because the LeaderboardPagerFragment, RegisterFragment and MaintenanceFileSelectionFragment are not top level fragments,
            // they will default to showing an up button (left arrow) plus the title.
            // If you don't want the up button, remove it here.  

            // As of May 25, 2023 we now have the missing OnPrepareMenu (so we can complete the transition to using the methods of IMenuProvider
            // However without removing the the NavigationIcon (back arrow) the menu of the new fragment
            // MaintenaceFileSelectionFragment would fail to show. If we remove the NavigationIcon it then shows.

            // if you have many fragments that are not top level fragments and they have menus, then maybe we need List<int> of their destinationIds, we could then
            // use similar code to how we handle immersivefragments as in SetShortEdgesIfRequired to remove the icon from the fragment with a menu
            // See an alternative method of removing the NavigationIcon in the OnViewCreated of MaintenanceFileSelectionFragment
            if (navDestination.Id == Resource.Id.leaderboardpager_fragment || navDestination.Id == Resource.Id.register_fragment)  // || navDestination.Id == Resource.Id.maintenance_file_selection_fragment) 
            {
                toolbar!.Title = navDestination.Label;
                toolbar.NavigationIcon = null;
            }

            // Is it an immersive fragment or is the preference set to allow full screen on devices with notches
            SetShortEdgesIfRequired(navDestination);
        }
        #endregion
        
        #region CheckForPreferenceChanges
        private void CheckForPreferenceChanges()
        {
            // Check if anything has been changed in the Settings Fragment before re-reading and updating the preference variables
            resetHelperExplanationDialogs = sharedPreferences!.GetBoolean("helper_screens", false);
            
            if (resetHelperExplanationDialogs) 
            {
                ISharedPreferencesEditor? editor = sharedPreferences.Edit();
                editor!.PutBoolean("showSubscriptionExplanationDialog", true);
                editor.PutBoolean("helper_screens", false);
                editor.PutBoolean("showFileMaintenanceExplanationDialog", true);
                editor.Commit();
            }
            
            // Re read again.
            resetHelperExplanationDialogs = sharedPreferences.GetBoolean("helper_screens", false);
            devicesWithNotchesAllowFullScreen = sharedPreferences.GetBoolean("devicesWithNotchesAllowFullScreen", false);
            animateFragments = sharedPreferences.GetBoolean("use_animations", false);
        }
        #endregion

        #region SetShortEdgesIfRequired
        private void SetShortEdgesIfRequired(NavDestination navDestination)
        {
            // Note: LayoutInDisplayCutoutMode.ShortEdges could be set in HideSystemUi in the ImmersiveFragment if you didn't have this requirement. 

            // For when we have more than one immersive fragment. Are they all going to displayed shortEdges or are only some screens (non immersive) going to be displayed ShortEdges.
            // Still to be done - change wording of the deviceWithNotchesAllowFullScreen, which will mean all fragments other than the ImmersiveFragments - see immersiveFragmentsDestinationIds - only one in the project.
            // Effectively it will be all fragments - or none, except all immersiveFragments will always be full screen because they will be in the List<int> immersiveFragmentDestinationIds. 
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                Window!.Attributes!.LayoutInDisplayCutoutMode = immersiveFragmentsDestinationIds!.Contains<int>(navDestination.Id) | devicesWithNotchesAllowFullScreen ? LayoutInDisplayCutoutMode.ShortEdges : LayoutInDisplayCutoutMode.Default;
        }
        #endregion

        #region DisplayWelcomeMessage
        private void ShowWelcomeMessage()
        {
            PackageManager packageManager = PackageManager!;
            PackageInfo packageInfo;

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                packageInfo = packageManager.GetPackageInfo(PackageName!, PackageManager.PackageInfoFlags.Of(PackageInfoFlagsLong.None));
            else
#pragma warning disable CS0618 // Type or member is obsolete
                packageInfo = packageManager.GetPackageInfo(PackageName!, 0)!;
#pragma warning restore CS0618 // Type or member is obsolete

            string message = GetString(Resource.String.welcome_navigationgraph8net8) + " - Version: " + packageInfo.VersionName;
            if (showSnackBar)
                SnackBarHelper.ShowSnackbar(navigationView!, message, 1);
        }
        #endregion

        #region Methods only called by ImmersiveFragment - if using
        public void DisableDrawerLayout() => drawerLayout!.SetDrawerLockMode(DrawerLayout.LockModeLockedClosed);
        public void EnableDrawerLayout() => drawerLayout!.SetDrawerLockMode(DrawerLayout.LockModeUnlocked);
        #endregion

        #region StopService - Dummy service test
        public void StopService()
        {
            // Called from OnDestroy when IsFinishing is true
            Log.Debug(logTag, logTag+ " StopService - called from MainActivity.OnDestroy");
        }
        #endregion

        #region OnRequestPermissionsResult
        // This is also used by the SettingsFragment - see MenuItemSelected in the HomeFragment
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == BLUETOOTH_PERMISSIONS_REQUEST_CODE)
            {
                bool permissionsGranted = grantResults.All(result => result == Permission.Granted);
                if (permissionsGranted)
                {
                    // Permissions granted, perform necessary actions
                    EnsureBluetoothEnabled();
                    navController!.Navigate(Resource.Id.settingsFragment);
                }
                else
                    ShowMissingPermissionDialog(this, GetString(Resource.String.missing_permission_title), GetString(Resource.String.missing_permission_explanation));

                return;
            }
        }
        #endregion

        #region ShowMissingPermissionDialog
        public static void ShowMissingPermissionDialog(FragmentActivity activity, string title, string explanation)
        {
            string tag = "ShowMissingPermissionDialog";
            AndroidX.Fragment.App.FragmentManager fm = activity.SupportFragmentManager;
            if (fm != null && !fm.IsDestroyed)
            {
                AndroidX.Fragment.App.Fragment? fragment = fm.FindFragmentByTag(tag);
                if (fragment == null)
                    BasicDialogFragment.NewInstance(title, explanation).Show(fm, tag);
            }
        }
        #endregion

        #region ShowBluetoothConfirmationDialog
        private void ShowBluetoothConfirmationDialog(string title, string explanation)
        {
            string tag = "BluetoothConfirmationDialogFragment";
            AndroidX.Fragment.App.FragmentManager fm = SupportFragmentManager;
            if (fm != null && !fm.IsDestroyed)
            {
                AndroidX.Fragment.App.Fragment? fragment = fm.FindFragmentByTag(tag);
                if (fragment == null)
                    BasicDialogFragment.NewInstance(title, explanation).Show(fm, tag);
            }
        }
        #endregion

        #region EnsureBluetoothEnabled
        private void EnsureBluetoothEnabled()
        {
            if ((bluetoothAdapter != null) && (!bluetoothAdapter.IsEnabled))
                bluetoothEnablerResultLauncher!.Launch(new Intent(BluetoothAdapter.ActionRequestEnable));
        }
        #endregion

        #region ActivityResultCallback_ActivityResultCalled
        private void ActivityResultCallback_ActivityResultCalled(object? sender, ActivityResult e)
        {
            string message;
            
            if (e.ResultCode == (int)Result.Ok)
            {
                if (bluetoothAdapter!.State == State.On)
                {
                    message = GetString(Resource.String.bluetooth_is_enabled);
                    ShowBluetoothConfirmationDialog(GetString(Resource.String.bluetooth_title), message);
                }
            }
            else if (e.ResultCode == (int)Result.Canceled)
            {
                if (bluetoothAdapter!.State == State.Off)
                {
                    message = GetString(Resource.String.bluetooth_not_enabled);
                    ShowBluetoothConfirmationDialog(GetString(Resource.String.bluetooth_title), message);
                }
                
            }
        }
        #endregion
    }
}




