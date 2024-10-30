using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UtilityClass
{
    public static bool ShouldLog = true;
    public static void LogMessages(params string[] messages)
    {
        if (!ShouldLog) return;

        foreach (var message in messages)
            Debug.Log(message);
    }

    public static void LogMessages(bool overridePermission = true, params string[] messages)
    {
        if (!overridePermission) return;

        foreach(var message in messages)
            Debug.Log(message);
    }
                   
}
