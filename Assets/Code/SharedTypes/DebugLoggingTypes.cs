using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//what level to do logging at 
public enum DebugLoggingLevel
{ 
    None,
    ErrorOnly,
    Minimal,
    Verbose,
    Count
}


public static class LogHelp
{
    public static bool LogError(DebugLoggingLevel dllLogLevel)
    {
        return dllLogLevel > DebugLoggingLevel.None;
    }

    public static bool LogMinimal(DebugLoggingLevel dllLogLevel)
    {
        return dllLogLevel > DebugLoggingLevel.ErrorOnly;
    }

    public static bool LogVerbose(DebugLoggingLevel dllLogLevel)
    {
        return dllLogLevel > DebugLoggingLevel.Minimal;
    }


    public static void ErrorLog(DebugLoggingLevel dllLogLevel, string strMessage)
    {
        if(dllLogLevel < DebugLoggingLevel.None)
        {
            Debug.LogError(strMessage);
        }
    }
}