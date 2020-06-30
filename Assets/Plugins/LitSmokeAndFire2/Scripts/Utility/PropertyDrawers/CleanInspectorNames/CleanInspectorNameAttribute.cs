using UnityEngine;
using System.Collections;

public class CleanInspectorNameAttribute : PropertyAttribute
{
	public string _strPropertyName = "";

	public string _strFoldOutTarget = "";

	public string _strCompareValue = null; 

	public string _strToolTip = "";

	public int _iIndent = 0;

	public float _fRed = 1;
	public float _fGreen = 1;
	public float _fBlue = 1;

	public CleanInspectorNameAttribute()
	{

	}


	public CleanInspectorNameAttribute(string strPropertyName)
	{
		_strPropertyName = strPropertyName;
	}

	public CleanInspectorNameAttribute(string strPropertyName,string  strFoldOutTarget)
	{
		_strPropertyName = strPropertyName;

		_strFoldOutTarget = strFoldOutTarget;
	}

	public CleanInspectorNameAttribute(string strPropertyName,string  strFoldOutTarget, string strCompareValue)
	{
		_strPropertyName = strPropertyName;
		
		_strFoldOutTarget = strFoldOutTarget;

		_strCompareValue = strCompareValue;
	}

	public CleanInspectorNameAttribute(string strPropertyName,string  strFoldOutTarget, string strCompareValue , string strToolTip)
	{
		_strPropertyName = strPropertyName;
		
		_strFoldOutTarget = strFoldOutTarget;
		
		_strCompareValue = strCompareValue;

		_strToolTip = strToolTip;
	}

	public CleanInspectorNameAttribute(string strPropertyName,string  strFoldOutTarget, string strCompareValue , string strToolTip , int iIndent)
	{
		_strPropertyName = strPropertyName;
		
		_strFoldOutTarget = strFoldOutTarget;
		
		_strCompareValue = strCompareValue;
		
		_strToolTip = strToolTip;

		_iIndent = iIndent;

	}

	public CleanInspectorNameAttribute(string strPropertyName,string  strFoldOutTarget, string strCompareValue , string strToolTip , int iIndent ,float fRed,float fGreen, float fBlue)
	{
		_strPropertyName = strPropertyName;
		
		_strFoldOutTarget = strFoldOutTarget;
		
		_strCompareValue = strCompareValue;
		
		_strToolTip = strToolTip;
		
		_iIndent = iIndent;

		_fRed = fRed;

		_fGreen = fGreen;

		_fBlue = fBlue;
	}

}
