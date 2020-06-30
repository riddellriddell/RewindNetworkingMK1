using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(MagicStringsAttribute))]
public class MagicStringsEditor : PropertyDrawer  
{
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{

		
		//get attribute
		MagicStringsAttribute msaMagicStringAttribute = attribute as MagicStringsAttribute;
		
		//check target category name
		string strCategoryName = msaMagicStringAttribute._strCategory;

		//check for bad  property
		if(string.IsNullOrEmpty(strCategoryName))
		{
			EditorGUI.PropertyField(position,property,label);

			return;
		}

		//get magic strings
		MagicStringDefinitions msdDeffinitions = MagicStringDefinitions.GetMagicStringsDefinition();

		if(msdDeffinitions == null)
		{
			EditorGUI.PropertyField(position,property,label);

			return;
		}

		//get options array for category
		string[] strOptions = new string[0];

		strOptions = msdDeffinitions.GetOptionsInCategory(strCategoryName);

		if(strOptions.Length == 0)
		{
			EditorGUI.PropertyField(position,property,label);

			return;
		}

		//get current selection
		string strCurrentSelection = property.stringValue;

		//curernt index value
		int iIndex = -1;



		if(string.IsNullOrEmpty(strCurrentSelection) == false)
		{
			bool bIndexFound = false;

			//get current selection index
			for(int i = 0 ; i < strOptions.Length; i++)
			{

				//check for match
				if(strOptions[i] == strCurrentSelection)
				{
					iIndex = i;
					bIndexFound = true;
					break;
				}
			}

			if(bIndexFound == false)
			{
				EditorGUI.PropertyField(position,property,label);
				
				return;
			}
		}
		else
		{
			iIndex = 0;
		}

		//draw the options dropdown
		iIndex = EditorGUI.Popup(position,iIndex,strOptions);

		property.stringValue = strOptions[iIndex];
	}


}
