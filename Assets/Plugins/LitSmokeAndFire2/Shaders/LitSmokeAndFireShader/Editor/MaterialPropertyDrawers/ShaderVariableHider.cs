using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderVariableHider : MaterialPropertyDrawer
{

    public string _strTarget;
    public string _strShowValue;


    public ShaderVariableHider(string strTarget,string strValue):base()
    {
        _strTarget = strTarget;
        _strShowValue = strValue;

        //Debug.Log("Constructor");
    }

    public ShaderVariableHider(string strTarget, float fValue) : base()
    {
        _strTarget = strTarget;
        _strShowValue = fValue.ToString();

        //Debug.Log("Constructor");
    }

    public bool ShowField(MaterialProperty prop)
    {

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
        else
        {
            if (prop.type == MaterialProperty.PropType.Texture)
            {

                return 70;
            }
            else
            {
                return base.GetPropertyHeight(prop, label, editor);
            }
        }

    }

    public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
    {
       
      
        if (ShowField(prop))
        {
            if (prop.type == MaterialProperty.PropType.Texture)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = prop.hasMixedValue;

                Texture texTexture = prop.textureValue;
                Vector4 vecScaleAndOffset = prop.textureScaleAndOffset;

                // Show the toggle control
                //draw textuer
                texTexture = editor.TextureProperty(position, prop, label);

                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    // Set the new value if it has changed
                    prop.textureValue = texTexture;
                }
            }
            else
            {
                // Debug.Log("Draw it");
                editor.DefaultShaderProperty(position, prop, label);
               
            }
        }

       // Debug.Log("Finished");

    }
}
