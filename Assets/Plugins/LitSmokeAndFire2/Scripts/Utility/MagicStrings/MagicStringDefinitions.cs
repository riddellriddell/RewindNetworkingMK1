using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MagicStringDefinitions : ScriptableObject 
{
	//------------------------- Nested Classes -----------------------
	[System.Serializable]
	public class Category
	{
		//the name of the catagory
		public string _strCategoryName;

		/// <summary>
		/// all the options in the catagory
		/// </summary>
		[SerializeField]
		public List<string> _strOptions;
	}
	//-------------------------- Static fucnitons --------------------

	public static MagicStringDefinitions GetMagicStringsDefinition()
	{
#if UNITY_EDITOR
		Object objMagicStrings  = EditorGUIUtility.Load("MagicStrings/MagicStringDefinitions.asset");

		if(objMagicStrings != null)
		{

			MagicStringDefinitions msdMagicStringsDef = objMagicStrings as MagicStringDefinitions;

			return msdMagicStringsDef;
		}

		return null;

#else
		return null;
#endif

	}

	//-------------------------- Static Variables --------------------

	//-------------------------- Instance Variables ------------------

	/// <summary>
	/// List of all the categorys
	/// </summary>
	public List<Category> _catOptionCategorys;

	//-------------------------- Instance Fucnitons ------------------

	public string[] GetOptionsInCategory(string strCategoryName)
	{
		//check for null inputs
		if(string.IsNullOrEmpty(strCategoryName) == true)
		{
			return new string[0];
		}

		//check that the category list exists 
		if(_catOptionCategorys == null)
		{
			return new string[0];
		}

		//loop through all catagorys
		foreach(Category catCategory in _catOptionCategorys)
		{
			if(catCategory != null)
			{
				if(catCategory._strCategoryName == strCategoryName)
				{
					if(catCategory._strOptions != null)
					{

						return catCategory._strOptions.ToArray();

					}
				}

			}
		}

		return new string[0];

	}

}
