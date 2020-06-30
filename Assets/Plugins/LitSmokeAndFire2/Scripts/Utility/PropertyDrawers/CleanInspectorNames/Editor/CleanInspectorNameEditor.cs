using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using StringExtensionMethods;
using System.Reflection;

[CustomPropertyDrawer(typeof(CleanInspectorNameAttribute))]
public class CleanInspectorNameEditor : PropertyDrawer 
{
	//instance variables 
//	protected Texture2D _texLineTexture;
//	public Texture2D LineTexture
//	{
//		get
//		{
//			if(_texLineTexture == null)
//			{
//				_texLineTexture = new Texture2D(1,2);
//
//				_texLineTexture.SetPixel(0,0,new Color(0.3f,0.3f,0.3f));
//				_texLineTexture.SetPixel(0,1,new Color(1f,1f,1f));
//
//				_texLineTexture.Apply();
//			}
//
//			return _texLineTexture;
//		}
//	}

	//--------------------------------- Instance Fucntions ----------------------

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		//Debug.Log("drawing Inspector");

		//check if folded out
		if(IsFoldedOut(property) == false)
		{
			GUI.color = new Color(1f,1f,1f,0.3f);
			GUI.Box(new Rect(position.x,position.y,position.width,1), "","box");
			GUI.color = Color.white;
			return;
		}

		//get attribute
		CleanInspectorNameAttribute cinCleanInspectorName = attribute as CleanInspectorNameAttribute;

		//check naming 
		string strFieldName = cinCleanInspectorName._strPropertyName;

		if(strFieldName == "")
		{
			//remove the type text from the existng name
			string strExistngName = property.name;

			//create split array
			char[] chrSplitArray = new char[] {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'};

			//split string at capitals 
			List<string> strSplitPropertyName = new List<string>(strExistngName.SplitAndKeep(chrSplitArray)) ;

			//create final result
			string strFinalResult = "";

			if(strSplitPropertyName != null)
			{
				for(int i = 1; i < strSplitPropertyName.Count; i++)
				{
					strFinalResult  = strFinalResult + strSplitPropertyName[i] + " " ;
				}

				label.text = strFinalResult;
			}
			else
			{
				label.text = "Clean Inspector name error";
			}
		}
		else
		{
			label.text = strFieldName;
		}


		//set indent 

		//create gui item with tooltip
		if(string.IsNullOrEmpty(cinCleanInspectorName._strToolTip) == false)
		{
			label.tooltip = cinCleanInspectorName._strToolTip;
		}

		//set colout
		GUI.color = new Color(cinCleanInspectorName._fRed,cinCleanInspectorName._fGreen, cinCleanInspectorName._fBlue);

		//set indent
		int iOldIndentLevel = EditorGUI.indentLevel;

		EditorGUI.indentLevel = cinCleanInspectorName._iIndent;

		EditorGUI.PropertyField(position,property,label);

		EditorGUI.indentLevel = iOldIndentLevel;

		GUI.color = Color.white;

	}
	
	public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
	{
		//check if folded out
		if(IsFoldedOut(property) == false)
		{
			return 2;
		}

		return base.GetPropertyHeight (property, label);
	}

	public bool IsFoldedOut(SerializedProperty property)
	{

		//get attribute
		CleanInspectorNameAttribute cinCleanInspectorName = attribute as CleanInspectorNameAttribute;

		if(string.IsNullOrEmpty(cinCleanInspectorName._strFoldOutTarget) == false)
		{

			return IsParentFoldedOut(property,cinCleanInspectorName._strFoldOutTarget,cinCleanInspectorName._strCompareValue);

		}

		return true;
	}

	public bool IsParentFoldedOut(SerializedProperty srpProperty,string strVariableName,string strCompareValue )
	{
		//get property
		object objPropert = ReflectionUtility.GetParentChildValue(srpProperty,strVariableName);

		//check for errors
		if(objPropert == null)
		{
			return false;
		}

		if(objPropert is Component)
		{
			Component compItem = objPropert as Component;

			if(compItem == null)
			{
				return false;
			}
		}

		if(objPropert is GameObject)
		{
			GameObject objItem = objPropert as GameObject;
			
			if(objItem == null)
			{
				return false;
			}
		}

		//check if currently folded
		if(string.IsNullOrEmpty(strCompareValue )!= true)
		{
			if(objPropert.ToString() != strCompareValue)
			{
				//Debug.Log ("Target string " + objPropert.ToString() + " true string " +  strCompareValue);
				return false;
			}
		}
		else if(objPropert is bool)
		{
			bool bFoldState = (bool)objPropert;

			if(bFoldState == false )
			{
				return false;
			}
		}


		//check to see if dependent on parent folding 

		//get property attributes
		CleanInspectorNameAttribute[] cinAttributes = ReflectionUtility.GetParentChildAttributes<CleanInspectorNameAttribute>(srpProperty,strVariableName,true);


		//check for errors
		if(cinAttributes == null || cinAttributes.Length == 0)
		{
			return true;
		}


		if(string.IsNullOrEmpty(cinAttributes[0]._strFoldOutTarget) == true)
		{

			return true;
		}
 
		return IsParentFoldedOut(srpProperty,cinAttributes[0]._strFoldOutTarget,cinAttributes[0]._strCompareValue);
	}
}
