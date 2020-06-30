using UnityEngine;
#if UNITY_EDITOR
//using UnityEditor;
#endif
using System.Collections;

public class ButtonAttribute : PropertyAttribute   
{

	public string _strFunctionName;
	
	public string _strButtonText;
	
	public ButtonAttribute(string strFunctionName , string strButtonText = "")
	{
		_strButtonText = strButtonText;
		_strFunctionName = strFunctionName;
		
		if(_strButtonText == "")
		{
			_strButtonText = _strFunctionName;
		}
	}
	
}
