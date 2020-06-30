using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(FileStructureStringAttribute))]
public class FileStructureStringDrawer : PropertyDrawer
{
	// Draw the property inside the given rect
	public override void OnGUI (Rect rctPos,SerializedProperty srpSerializedProperty, GUIContent gucLabel) 
	{

		//error check
		if(srpSerializedProperty.propertyType != SerializedPropertyType.String)
		{
			EditorGUI.LabelField (rctPos, gucLabel.text, "Use File Structure String with string classes only.");

			return;
		}


		//split the draw area in 2
		Rect recLeft = new Rect(rctPos.x,rctPos.y,rctPos.width * 0.5f, rctPos.height );
		Rect recRight = new Rect( recLeft.xMax ,rctPos.y, recLeft.width, rctPos.height );

		//draw the strign 
		srpSerializedProperty.stringValue = EditorGUI.TextField(recLeft,gucLabel.text, srpSerializedProperty.stringValue);

		//get attribute
		FileStructureStringAttribute fsaAttribute = attribute as FileStructureStringAttribute;

		//the target object this string is pointing at
		Object objTargetObject = null;

		//try and get object at target address
		if(fsaAttribute._faoFieldAddressOption != FileStructureStringAttribute.FileAddressOptions.EDITOR_RESOURCE_ADDRESS)
		{
			objTargetObject = AssetDatabase.LoadAssetAtPath(srpSerializedProperty.stringValue,typeof(Object));
		}
		else
		{
			objTargetObject = EditorGUIUtility.Load(srpSerializedProperty.stringValue);
		}

		//Let user select new object
		objTargetObject = EditorGUI.ObjectField(recRight,objTargetObject,typeof(Object),false);

		//check if object passes type field
		if(DoesObjectPassFiltering(objTargetObject ) == false)
		{
			return;
		}

		//filter address
		string strFinalAddress =  FilterAddress( AssetDatabase.GetAssetPath(objTargetObject));

		//try and get the address at the target location
		srpSerializedProperty.stringValue = strFinalAddress;


	}

	public bool DoesObjectPassFiltering(Object objObject)
	{
		//check for errors
		if(objObject == null)
		{
			return false;
		}

		//get attribute
		FileStructureStringAttribute fsaAttribute = attribute as FileStructureStringAttribute;

		if(fsaAttribute._typTypeOfObjectToFilterFor == null)
		{
			return true;
		}

		//check for inheritence optins 
		if(fsaAttribute._bAllowInheritance == true)
		{

			//check if object is type
			return  fsaAttribute._typTypeOfObjectToFilterFor.IsAssignableFrom( objObject.GetType()); 
		}
		else
		{
			return objObject.GetType() == fsaAttribute._typTypeOfObjectToFilterFor;
		}
	}

	public string FilterAddress(string strFileAddress)
	{
		//get attribute
		FileStructureStringAttribute fsaAttribute = attribute as FileStructureStringAttribute;

		switch(fsaAttribute._faoFieldAddressOption)
		{

		case FileStructureStringAttribute.FileAddressOptions.DEFAULT:

			return strFileAddress;

			break;

		case FileStructureStringAttribute.FileAddressOptions.EXCLUDE_FILE_EXTENSION:

			return RemoveFileType(strFileAddress);

			break;

		case FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY:

			return FolderOnlyAddress(strFileAddress);

			break;

		case FileStructureStringAttribute.FileAddressOptions.EDITOR_RESOURCE_ADDRESS:

			return EditorResourceAddress(strFileAddress);

			break;

		}


		return strFileAddress;
	}

	public string EditorResourceAddress(string strFileAdderss)
	{
		return strFileAdderss.Split(new string[1]{"Editor Default Resources/"},System.StringSplitOptions.None)[1];
	}

	public string FolderOnlyAddress(string strFileAddress)
	{
		//check if address has a . in it
		if(strFileAddress.Contains(".") == false)
		{
			return strFileAddress;
		}
		
		string[] strAddressSegments = strFileAddress.Split('/');
		
		string strCleanedFile = "";
		
		for(int i = 0 ; i < strAddressSegments.Length -2 ; i++)
		{
			strCleanedFile = strCleanedFile + strAddressSegments[i] + "/";
		}

		if(strAddressSegments.Length -2 >= 0)
		{
			strCleanedFile = strCleanedFile + strAddressSegments[strAddressSegments.Length -2];
		}
		
		return strCleanedFile;
	}

	public string RemoveFileType(string strFileAdderss)
	{
		return strFileAdderss.Split('.')[0];
	}

}
