using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif
[CustomPropertyDrawer(typeof(HelpAttribute))]
public class HelpDecorator : PropertyDrawer
{
#if UNITY_EDITOR

    public int _iButtonSize = 15;
    public int _iButtonFontSize = 6;
    public int _iTextBottomPadding = 30;
    public int _iScrollBarComp = 20;
    public bool _bDisplayHelpText = false;
    public int _iDefaultFontSize = 12;

    public bool IsFoldedOut(SerializedProperty property)
    {

        //get attribute
        HelpAttribute hatHelpAttribute = attribute as HelpAttribute;

        if (string.IsNullOrEmpty(hatHelpAttribute._strFoldOutTarget) == false)
        {

            return IsParentFoldedOut(property, hatHelpAttribute._strFoldOutTarget, hatHelpAttribute._strCompareValue);

        }

        return true;
    }

    public bool IsParentFoldedOut(SerializedProperty srpProperty, string strVariableName, string strCompareValue)
    {
        //get property
        object objPropert = ReflectionUtility.GetParentChildValue(srpProperty, strVariableName);

        //check for errors
        if (objPropert == null)
        {
            return false;
        }

        if (objPropert is Component)
        {
            Component compItem = objPropert as Component;

            if (compItem == null)
            {
                return false;
            }
        }

        if (objPropert is GameObject)
        {
            GameObject objItem = objPropert as GameObject;

            if (objItem == null)
            {
                return false;
            }
        }

        //check if currently folded
        if (string.IsNullOrEmpty(strCompareValue) != true)
        {
            if (objPropert.ToString() != strCompareValue)
            {
                //Debug.Log ("Target string " + objPropert.ToString() + " true string " +  strCompareValue);
                return false;
            }
        }
        else if (objPropert is bool)
        {
            bool bFoldState = (bool)objPropert;

            if (bFoldState == false)
            {
                return false;
            }
        }


        //check to see if dependent on parent folding 

        //get property attributes
        CleanInspectorNameAttribute[] cinAttributes = ReflectionUtility.GetParentChildAttributes<CleanInspectorNameAttribute>(srpProperty, strVariableName, true);


        //check for errors
        if (cinAttributes == null || cinAttributes.Length == 0)
        {
            return true;
        }


        if (string.IsNullOrEmpty(cinAttributes[0]._strFoldOutTarget) == true)
        {

            return true;
        }

        return IsParentFoldedOut(srpProperty, cinAttributes[0]._strFoldOutTarget, cinAttributes[0]._strCompareValue);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        //check if folded out
        if(IsFoldedOut(property) == false)
        {
            return 0;
        }


        if (_bDisplayHelpText)
        {
            //get attribute
            HelpAttribute hatHelpAttribute = attribute as HelpAttribute;

            float fCurrentScreenWidth = EditorGUIUtility.currentViewWidth - _iScrollBarComp;

            bool _bUseWordWarp = EditorStyles.label.wordWrap;
            EditorStyles.label.wordWrap = true;

            float fOutputheight = EditorStyles.label.CalcHeight(new GUIContent(hatHelpAttribute._strHelpText), fCurrentScreenWidth) + _iTextBottomPadding;

            EditorStyles.label.wordWrap = _bUseWordWarp;

            return fOutputheight;
         
        }
        else
        {
            return _iButtonSize;
        }

    }

    public int GetHeightOfTextField(string strText, int iWidthOfField, int iWidthOfLetter, int iHeightOfLetter,int iPadding)
    {
        int iTotalLengthOfText = strText.Length * iWidthOfLetter;
        int iLinesInText = Mathf.CeilToInt((float)iTotalLengthOfText / (float)iWidthOfField);
        int iRawHeight = iLinesInText * iHeightOfLetter;
        int iFinalHeight = iRawHeight + iPadding;

        return iFinalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        //check if folded out
        if (IsFoldedOut(property) == false)
        {
            return ;
        }

        //store default font size
        _iDefaultFontSize = EditorStyles.label.fontSize;

        if (_bDisplayHelpText)
        {
            HelpAttribute hatHelpAttribute = attribute as HelpAttribute;

            Rect recButton = new Rect(position);
            Rect recText = new Rect(position);


            //clamp to button size
            recButton.width = _iButtonSize;
            recButton.height = _iButtonSize;

            int iFontSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = _iButtonFontSize;
            if (GUI.Button(recButton, "?"))
            {
                _bDisplayHelpText = !_bDisplayHelpText;
            }
            GUI.skin.button.fontSize = iFontSize;

            //shift text over
            recText.x += _iButtonSize;
            recText.width -= _iButtonSize;
            bool bExistingWarp = EditorStyles.label.wordWrap;
            EditorStyles.label.wordWrap = true;
            EditorGUI.LabelField(recText, hatHelpAttribute._strHelpText);
            EditorStyles.label.wordWrap = bExistingWarp;

        }
        else
        {
            Rect recButton = new Rect(position);

            //clamp to button size
            recButton.width = _iButtonSize;
            recButton.height = _iButtonSize;

            int iFontSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = _iButtonFontSize;
            if (GUI.Button(recButton, "?"))
            {
                _bDisplayHelpText = !_bDisplayHelpText;
            }
            GUI.skin.button.fontSize = iFontSize;
        }

    }

#endif
}