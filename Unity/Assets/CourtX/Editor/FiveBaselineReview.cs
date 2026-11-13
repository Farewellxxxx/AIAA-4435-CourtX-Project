using System;
using UnityEngine;
public static class FiveBaselineReview
{
    public static void Run()
    {
        ReactiveReview.RunRules();
        RecapReview.RunRules();
        LearningReview.RunRules();
        ReadabilityReview.RunRules();
        Debug.Log("FIVE BASELINE: all four legacy suites passed");
    }
}
