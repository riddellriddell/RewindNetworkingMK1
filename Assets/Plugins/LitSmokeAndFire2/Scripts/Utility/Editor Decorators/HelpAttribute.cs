using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpAttribute : PropertyAttribute
{
    
    public string _strCompareValue = "";
    public string _strFoldOutTarget = "";
    public string _strHelpText = "";
   


    public HelpAttribute(string strHelpText, string strTarget = "", string strShowValue = "True")
    {
        _strCompareValue = strShowValue;
        _strHelpText = strHelpText;
        _strFoldOutTarget = strTarget;

    }

}
