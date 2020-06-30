
using UnityEngine;
using System.Collections;

public class MagicStringsAttribute : PropertyAttribute 
{
	public string _strCategory;

	public MagicStringsAttribute(string strCategory)
	{
		_strCategory = strCategory;
	}
}
