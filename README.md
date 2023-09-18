# NavigationGraph8Net8 net8.0-android34

Changed showSnackBar back to false;

Readme for NavigationGraph8Net8 - 15 Sept, 2023.

Net8.0 RC1 was released Sept 12, 2023.

In this example I've converted the project NavigationGraph8Net7 to Net8.0, and therefore named it NavigationGraph8Net8. This readme explains the steps that were taken for the conversion.

The following link https://github.com/xamarin/xamarin-android/releases/ shows the release notes for Net8.0 RC1. I'm using VS2022 Community 17.8.0 Preview 2.0. It did not get the .Net Maui (Net8 Preview) option you can see in the image in the link. But when you examine the image you'll see that it is Visual Studio Enterprise 2022, which probably explains why it was missing in VS 2022 Community 17.8.0 Preview 2.0.

Therefore the way to apply the Net8.0 RC1 for an Android project is to follow the instructions listed below the image.
```
> dotnet workload install android
```
and then check the installation with 
```
> dotnet workload list
```
The following image shows the result of the workload list command.
```
Installed Workload Id      Manifest Version                            Installation Source
--------------------------------------------------------------------------------------------
android                    34.0.0-rc.1.432/8.0.100-rc.1                SDK 8.0.100-rc.1
```
Now you are ready to make the changes to the project. I didn't want to change the NavigationGraph8Net7 project, so I created a new project NavigationGraph8Net8. Basically I deleted all the files within the new project except for  mipmap folders.

I then copied the folder structure and their contents from NavigationGraph8Net7 to NavigationGraph8Net8. I then opened the NavigationGraph8Net8.csproj file and made the following changes. Then changed the TargetFramework from net7.0-android to net8.0-android. I changed ```<SupportedOSPlatformVersion>24</SupportedOSPlatformVersion>``` to 25, as that is the lowest API version that is supported by Net8.0. The final change was updating the to ```<RootNamespace>com.companyname.navigationgraph8net8</RootNamespace>``` 

I also changed the AndroidManifest.xml file to reflect ```<uses-sdk android:minSdkVersion="24" android:targetSdkVersion="33" />``` to 25 and 34 respectively. While I was in the manifest I also changed the package name to package="com.companyname.navigationgraph8net8", matching the RootNamespace.

The final change was to change the namespace of each file from com.companyname.navigationgraph8net7 to com.companyname.navigationgraph8net8, using Find and Replace/Replace in Files. However I do suggest first checking with Find and Replace/Find in Files just to check your changes before you commit to them.

There were a couple of Build errors which were easily fixed. The first was in the the HomeFragment in the method BluetoothPermissionsGranted(). It just needed to be wrapped in a ```if (OperatingSystem.IsAndroidVersionAtLeast(31)).``` The second was in the MainActivity in the DisplayWelcomeMessage method.

It was changed from
```
packageInfo = packageManager.GetPackageInfo(PackageName!, PackageManager.PackageInfoFlags.Of(0));
```
to
```
packageInfo = packageManager.GetPackageInfo(PackageName!, PackageManager.PackageInfoFlags.Of(PackageInfoFlagsLong.None));
```

PackageInfoFlagsLong.None is 0 anyway, so a very trival change.

Please note that Microsoft.NET.ILLink.Tasks library is added automatically to the build.

The project was successfully deployed to a Pixel 6 running Android 14 Beta.

I have run into a couple of problems with my main app. As I overcome those problems, I'll add sample code to this app, to demonstrate the changes.
