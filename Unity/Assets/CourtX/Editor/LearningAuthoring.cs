using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class LearningAuthoring
{
    public static void Build()=>BuildTo("../Windows_Learning/CourtX Learning.exe",null);
    public static void BuildQA()=>BuildTo("../Preview/Playtests/Learning-20261111/QA/CourtX Learning QA.exe",new[]{"COURTX_LEARNING_REVIEW"});
    static void BuildTo(string path,string[] defines)
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TacticalAuthoring.Scene},locationPathName=path,
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=defines});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Learning build failed: "+report.summary.result);
        UnityEngine.Debug.Log("LEARNING BUILD SUCCEEDED: "+report.summary.totalSize+" bytes");
    }
}
