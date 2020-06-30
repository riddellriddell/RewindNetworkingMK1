using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderKeyWordList : MaterialPropertyDrawer
{
    string[] _strOptions;
    string[] _strKeyWords;
    
    public ShaderKeyWordList(string strKeyWord , string strOption ):base()
    {
        
        _strKeyWords = new string[1];
        _strKeyWords[0] = strKeyWord;

        _strOptions = new string[1];
        _strOptions[0] = strOption;
    }

    public ShaderKeyWordList(string strKeyWord1, string strKeyWord2, string strOption1 , string strOption2) : base()
    {
        
        _strKeyWords = new string[2];
        _strKeyWords[0] = strKeyWord1;
        _strKeyWords[1] = strKeyWord2;

        _strOptions = new string[2];
        _strOptions[0] = strOption1;
        _strOptions[1] = strOption2;
    }

    public ShaderKeyWordList(string strKeyWord1 , string strKeyWord2 , string strKeyWord3 , string strOption1 , string strOption2 , string strOption3) : base()
    {
        
        _strKeyWords = new string[3];
        _strKeyWords[0] = strKeyWord1;
        _strKeyWords[1] = strKeyWord2;
        _strKeyWords[2] = strKeyWord3;

        _strOptions = new string[3];
        _strOptions[0] = strOption1;
        _strOptions[1] = strOption2;
        _strOptions[2] = strOption3;
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
    {
        // Setup
        int iSelection = (int)(prop.floatValue);

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;

        // Show the toggle control
        iSelection = EditorGUI.Popup(position, label, iSelection,_strOptions);

        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
           for (int i = 0; i < prop.targets.Length; i++)
           {
               Material matTarget = ((Material)prop.targets[i]);
           
               for(int j = 0; j < _strKeyWords.Length; j++)
               {
                   if (j == iSelection)
                   {
                       matTarget.EnableKeyword(_strKeyWords[j]);
                   }
                   else
                   {
                       matTarget.DisableKeyword(_strKeyWords[j]);
                   }
               }
           
           }

            // Set the new value if it has changed
            prop.floatValue = iSelection;
        }
    }
}
