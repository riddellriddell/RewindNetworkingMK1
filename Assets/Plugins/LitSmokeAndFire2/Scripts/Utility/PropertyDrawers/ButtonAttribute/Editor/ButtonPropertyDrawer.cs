using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonPropertyDrawer : PropertyDrawer
{
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
	
		//get the atribute 
		ButtonAttribute btnAtribute = attribute as ButtonAttribute;
		
		//draw button over the field 
		if(GUI.Button(position,btnAtribute._strButtonText) == false)
		{
			return;
		}
		
		//use reflection to get the Parent class
		object objParentClass = PropertyRefferenceGetter.GetParent(property);
		
		//check for errors
		if(objParentClass ==  null)
		{
			return;
		}
		
		//get obejct typr
		Type typTargetType = objParentClass.GetType();
		
		//loop over class functions to try and fins a match
		MethodInfo[] methodInfos = typTargetType.GetMethods();
		
		//loop over method looking for matching method name
		for(int i = 0 ; i < methodInfos.Length; i++)
		{
			if(methodInfos[i].Name == btnAtribute._strFunctionName)
			{
				if(methodInfos[i].GetParameters() == null || methodInfos[i].GetParameters().Length == 0)
				{
					methodInfos[i].Invoke(objParentClass,new object[0]);
					
					return;
				}
			}
		}
		
		Debug.LogError("funciton " + btnAtribute._strFunctionName + " not found on object " + objParentClass.ToString());
	}
}
