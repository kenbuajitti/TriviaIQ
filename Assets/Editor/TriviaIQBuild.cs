using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TriviaIQBuild
{
    [MenuItem("Tools/TriviaIQ/Build WebGL")]
    public static void BuildWebGL()
    {
        PlayerSettings.productName = "TriviaIQ";
        PlayerSettings.companyName = "IQ Games";
        PlayerSettings.WebGL.template = "PROJECT:TspResponsive";
        // Uncompressed files work on itch.io without custom response headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/TspMenuScene.unity", "Assets/Scenes/TspGameScene.unity" },
            locationPathName = "Builds/TriviaIQ-WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("TriviaIQ WebGL build failed: " + report.summary.result);
        Debug.Log("TriviaIQ ready in Builds/TriviaIQ-WebGL. Zip the CONTENTS of that folder for itch.io.");
    }
}
