using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TriviaIQBuild
{
    [MenuItem("Tools/TriviaIQ/Build WebGL")]
    public static void BuildWebGL()
    {
        TriviaIQWebBuild.Build();
    }
}
