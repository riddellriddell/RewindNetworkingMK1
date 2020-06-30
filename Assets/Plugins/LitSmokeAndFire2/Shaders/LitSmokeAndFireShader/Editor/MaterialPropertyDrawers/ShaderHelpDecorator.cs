using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderHelpDecorator : MaterialPropertyDrawer
{
    public string _strShowValue = "";
    public string _strTarget = "";

    public string _strHelpText;
    public int _iTextHeight = 500;
    public int _iButtonSize = 15;
    public int _iFontSize = 6;
    public bool _bDisplayHelpText = false;

    public ShaderHelpDecorator(string strHelpText, float fHeight )
    {
        _strHelpText = strHelpText;
        _iTextHeight = (int)fHeight;
        //_strShowValue = strShowValue;
        //_strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1, string strHelpText2, float fHeight)
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2;
        _iTextHeight = (int)fHeight;
       //_strShowValue = strShowValue;
       //_strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1, string strHelpText2, string strHelpText3, float fHeight )
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2 + "\n \n" + strHelpText3;
        _iTextHeight = (int)fHeight;
        //_strShowValue = strShowValue;
        //_strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1, string strHelpText2, string strHelpText3, string strHelpText4, float fHeight)
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2 + "\n \n" + strHelpText3 + "\n \n" + strHelpText4;
        _iTextHeight = (int)fHeight;
        //_strShowValue = strShowValue;
        //_strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText, float fHeight, string strTarget, float fShowValue )
    {
        _strHelpText = strHelpText;
        _iTextHeight = (int)fHeight;
        _strShowValue = fShowValue.ToString();
        _strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1 , string strHelpText2, float fHeight, string strTarget, float fShowValue)
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2;
        _iTextHeight = (int)fHeight;
        _strShowValue = fShowValue.ToString();
        _strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1, string strHelpText2, string strHelpText3, float fHeight, string strTarget, float fShowValue)
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2 + "\n \n" + strHelpText3;
        _iTextHeight = (int)fHeight;
        _strShowValue = fShowValue.ToString();
        _strTarget = strTarget;
    }

    public ShaderHelpDecorator(string strHelpText1, string strHelpText2, string strHelpText3, string strHelpText4, float fHeight, string strTarget, float fShowValue)
    {
        _strHelpText = strHelpText1 + "\n \n" + strHelpText2 + "\n \n" + strHelpText3 + "\n \n" + strHelpText4;
        _iTextHeight = (int)fHeight;
        _strShowValue = fShowValue.ToString();
        _strTarget = strTarget;
    }

    public bool ShowField(MaterialProperty prop)
    {
        if(_strShowValue == "" || _strTarget == "")
        {
            return true;
        }

        //Debug.Log("check if property should be drawn");

        for (int i = 0; i < prop.targets.Length; i++)
        {
            Material matTarget = (Material)prop.targets[i];

            if (matTarget.GetFloat(_strTarget).ToString() == _strShowValue)
            {
                // Debug.Log("Passes check");
                return true;
            }
        }

        return false;
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        if(ShowField(prop) == false)
        {
            return 0;
        }

        if (_bDisplayHelpText)
        {
            return  _iTextHeight;
        }
        else
        {
            return _iButtonSize;
        }

    }

    public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
    {
        if (ShowField(prop) == false)
        {
            return;

        }

        if (_bDisplayHelpText)
        {
            Rect recButton = new Rect(position);
            Rect recText = new Rect(position);


            //clamp to button size
            recButton.width = _iButtonSize;
            recButton.height = _iButtonSize;

            int iFontSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = _iFontSize;
            if (GUI.Button(recButton, "?"))
            {
                _bDisplayHelpText = !_bDisplayHelpText;
            }
            GUI.skin.button.fontSize = iFontSize;

            //shift text over
            recText.x += _iButtonSize;
            recText.width -= _iButtonSize;
            bool bExistingWarp =EditorStyles.label.wordWrap;
           
            EditorStyles.label.wordWrap = true;
            EditorGUI.LabelField(recText, _strHelpText);
            EditorStyles.label.wordWrap = bExistingWarp;
           
        }
        else
        {
            Rect recButton = new Rect(position);

            //clamp to button size
            recButton.width = _iButtonSize;
            recButton.height = _iButtonSize;

            int iFontSize = GUI.skin.button.fontSize;
            GUI.skin.button.fontSize = _iFontSize;
            if (GUI.Button(recButton, "?"))
            {
                _bDisplayHelpText = !_bDisplayHelpText;
            }
            GUI.skin.button.fontSize = iFontSize;
        }
       
    }
}
