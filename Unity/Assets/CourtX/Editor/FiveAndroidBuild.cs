using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FiveAndroidBuild
{
    // Invoke Unity with -batchmode -quit -buildTarget Android -executeMethod FiveAndroidBuild.Build.
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException("Select Android or pass -buildTarget Android to the editor.");

        string keystore = Path.GetFullPath(Required("COURTX_ANDROID_KEYSTORE"));
        if (!File.Exists(keystore))
            throw new BuildFailedException("CourtX Android release keystore is missing.");
        string alias = Required("COURTX_ANDROID_KEY_ALIAS");
        string storePassword = Required("COURTX_ANDROID_STORE_PASSWORD");
        string keyPassword = Required("COURTX_ANDROID_KEY_PASSWORD");
        string output = Environment.GetEnvironmentVariable("COURTX_ANDROID_APK") ??
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "Android", "CourtX-Android-arm64.apk");
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        var android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, "com.courtx.fiveplayer");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.bundleVersionCode = 700;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        PlayerSettings.Android.useAPKExpansionFiles = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.buildAppBundle = false;

        var orientation = PlayerSettings.defaultInterfaceOrientation;
        bool portrait = PlayerSettings.allowedAutorotateToPortrait;
        bool inverted = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool left = PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool right = PlayerSettings.allowedAutorotateToLandscapeRight;
        bool custom = PlayerSettings.Android.useCustomKeystore;
        string previousStore = PlayerSettings.Android.keystoreName;
        string previousAlias = PlayerSettings.Android.keyaliasName;
        string previousStorePassword = PlayerSettings.Android.keystorePass;
        string previousKeyPassword = PlayerSettings.Android.keyaliasPass;
        try
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPassword;

            var report = BuildPipeline.BuildPlayer(new[] { FivePlayerAuthoring.Scene }, output,
                BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("CourtX Android build failed: " + report.summary.result);
            Debug.Log("CourtX Android APK built at " + output);
        }
        finally
        {
            PlayerSettings.Android.keystoreName = previousStore;
            PlayerSettings.Android.keyaliasName = previousAlias;
            PlayerSettings.Android.keystorePass = previousStorePassword;
            PlayerSettings.Android.keyaliasPass = previousKeyPassword;
            PlayerSettings.Android.useCustomKeystore = custom;
            PlayerSettings.defaultInterfaceOrientation = orientation;
            PlayerSettings.allowedAutorotateToPortrait = portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = inverted;
            PlayerSettings.allowedAutorotateToLandscapeLeft = left;
            PlayerSettings.allowedAutorotateToLandscapeRight = right;
        }
    }

    static string Required(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            throw new BuildFailedException("Set " + name + " to sign the release APK.");
        return value;
    }
}

sealed class FiveAndroidNotices : BuildPlayerProcessor
{
    public override void PrepareForBuild(BuildPlayerContext context)
    {
        if (context.BuildPlayerOptions.target != BuildTarget.Android)
            return;
        string notices = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "ThirdPartyNotices"));
        context.AddAdditionalPathToStreamingAssets(notices, "ThirdPartyNotices");
    }
}
