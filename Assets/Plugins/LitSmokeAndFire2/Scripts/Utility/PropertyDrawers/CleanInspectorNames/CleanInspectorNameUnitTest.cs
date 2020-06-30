using UnityEngine;
using System.Collections;

public class CleanInspectorNameUnitTest : MonoBehaviour 
{
	[CleanInspectorNameAttribute]
	public string _strCrazyTextName;

	[CleanInspectorNameAttribute("Genifer garner")]
	public float _fAliasedName;

	[CleanInspectorNameAttribute] 
	public bool _bFoldoutValue  = false;

	[CleanInspectorNameAttribute("","_bFoldoutValue")]
	public float _fValueToHide1;

	[CleanInspectorNameAttribute("","_bFoldoutValue",null,"Test Tool Tip")]
	public float _fValueToHide2;

	[CleanInspectorNameAttribute("","_bFoldoutValue",null,"Test Tool Tip",1)]
	public string _bNestedFoldoutMaster0 = "";
	
	[CleanInspectorNameAttribute("","_bNestedFoldoutMaster0","True")]
	public int _iNestedFoldOut0;

	[CleanInspectorNameAttribute]
	public int _iNonHideValue;

	[CleanInspectorNameAttribute("","_bFoldoutValue",null,"Test Tool Tip",2,1,0,0)]
	public string _strValueToHide3;

	[CleanInspectorNameAttribute("","_bFoldoutValue")]
	public bool _bNestedFoldoutMaster = false;

	[CleanInspectorNameAttribute("","_bNestedFoldoutMaster")]
	public int _iNestedFoldOut;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
}
