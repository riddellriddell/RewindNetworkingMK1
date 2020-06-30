using UnityEngine;
using System.Collections;
using System;


//this class marks a string as refferencing a file in the file structure 
public class FileStructureStringAttribute : PropertyAttribute
{
	public enum FileAddressOptions
	{
		DEFAULT,
		FOLDER_REFFERENCE_ONLY,
		EXCLUDE_FILE_EXTENSION,
		EDITOR_RESOURCE_ADDRESS,
		COUNT
	}

	public FileAddressOptions _faoFieldAddressOption = FileAddressOptions.DEFAULT;

	public Type _typTypeOfObjectToFilterFor = null;

	public bool _bAllowInheritance = true;

	public FileStructureStringAttribute()
	{
	}

	public FileStructureStringAttribute(FileAddressOptions faoFieldAddressOption)
	{
		_faoFieldAddressOption = faoFieldAddressOption;
	}

	public FileStructureStringAttribute(FileAddressOptions faoFieldAddressOption , Type typRequiredType)
	{
		_faoFieldAddressOption = faoFieldAddressOption;

		_typTypeOfObjectToFilterFor = typRequiredType;
	}

	public FileStructureStringAttribute(FileAddressOptions faoFieldAddressOption , Type typRequiredType , bool bAllowInheritance)
	{
		_faoFieldAddressOption = faoFieldAddressOption;
		
		_typTypeOfObjectToFilterFor = typRequiredType;

		_bAllowInheritance = bAllowInheritance;
	}

}
