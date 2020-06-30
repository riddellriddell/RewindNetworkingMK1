using JsonDotNet;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;

namespace SwordfishTools
{
	public static class MetaDataHelper 
	{
#if UNITY_EDITOR
        public static T GetAssetImporterMetaData<T>(AssetImporter asmAssetImporter)
		{
			if(asmAssetImporter == null)
			{
				return default(T);
			}

			// create desertialization settings 
			T tDeserializedObject =  default(T);
			
			//setup serialization settings
			JsonSerializerSettings jssJsonSettings = new JsonSerializerSettings();
			
			jssJsonSettings.TypeNameHandling = TypeNameHandling.All;
			
			try
			{
				//set serializer to look for type settinggs 
				tDeserializedObject = JsonConvert.DeserializeObject<T>(asmAssetImporter.userData,jssJsonSettings);
			}
			catch
			{
				Debug.Log("Error occured during serialization");
				return default(T);
			}
			
			return tDeserializedObject;
		}
#endif

        public static void SetObjectMetaData(UnityEngine.Object objObject, string strMetaDataString)
		{
#if UNITY_EDITOR
            //check for nullls
            if (objObject == null)
			{
				return;
			}
			
			//check if object exists in file structure
			string strAssetPath = AssetDatabase.GetAssetPath(objObject);
			
			if(strAssetPath == "" )
			{
				Debug.LogError("asset " + objObject.ToString() + " not in file structure");
				
				return;
			}
			
			//get asset importer
			AssetImporter aimAssetImporter = AssetImporter.GetAtPath(strAssetPath);
			
			if(aimAssetImporter == null)
			{
				return;
			}
			
			//set meta data
			aimAssetImporter.userData = strMetaDataString;

			//mark object as dirty
			EditorUtility.SetDirty(aimAssetImporter);
			EditorUtility.SetDirty(objObject);

			
			AssetDatabase.WriteImportSettingsIfDirty(strAssetPath);

#endif

        } 
	
		public static void SetSelectedObjectMetaData<T>(T tMetaDataObject,Type typSelectionFilterOption)
		{
#if UNITY_EDITOR
            if (tMetaDataObject == null)
			{
				Debug.LogError("Metadata object not passed");
				return;
			}

			//get the taraget asset
			UnityEngine.Object objSelection = Selection.GetFiltered(typSelectionFilterOption,SelectionMode.TopLevel)[0];
			
			//check fo null selection
			if(objSelection == null)
			{
				return;
			}

			//get meta data for texture
			string strUserMetaData = null;
			
			//setup serialization settings
			JsonSerializerSettings jssJsonSettings = new JsonSerializerSettings();
			
			//formatting
			Formatting fmtFormatting = Formatting.None;
			
			jssJsonSettings.TypeNameHandling = TypeNameHandling.All;
			
			try
			{
				//set serializer to look for type settinggs 
				strUserMetaData = JsonConvert.SerializeObject(tMetaDataObject,fmtFormatting,jssJsonSettings);
			}
			catch
			{
				Debug.Log("Error occured during serialization");
				return ;
			}
			
			//set the meta data on the object
			SetObjectMetaData(objSelection,strUserMetaData);
#endif
        }
		
		public static void SetAllSelectedObjectMetaData<T>(T tMetaDataObject, Type typSelectionFilterOption )
		{
#if UNITY_EDITOR
            if (tMetaDataObject == null)
			{
				Debug.LogError("Metadata object not passed");
				return;
			}

			
			//get the taraget asset
			UnityEngine.Object[] objTargetObject = Selection.GetFiltered(typSelectionFilterOption,SelectionMode.TopLevel);
			
			if(objTargetObject == null || objTargetObject.Length == 0)
			{
				return;
			}
			
			//get meta data for object
			string strUserMetaData = null;
			
			//setup serialization settings
			JsonSerializerSettings jssJsonSettings = new JsonSerializerSettings();
			
			//formatting
			Formatting fmtFormatting = Formatting.None;
			
			jssJsonSettings.TypeNameHandling = TypeNameHandling.All;
			
			try
			{
				//set serializer to look for type settinggs 
				strUserMetaData = JsonConvert.SerializeObject(tMetaDataObject,fmtFormatting,jssJsonSettings);
			}
			catch
			{
				Debug.Log("Error occured during serialization");
				return ;
			}
			
			//assign meta data
			foreach(UnityEngine.Object objObject in objTargetObject)
			{
				SetObjectMetaData(objObject, strUserMetaData);
			}
#endif
        }
		
		public static string GetSelectedObjectMetaDataString(Type typSelectionFilterOption)
		{
#if UNITY_EDITOR
            //get the taraget asset
            UnityEngine.Object[] objTargetObject = Selection.GetFiltered(typSelectionFilterOption,SelectionMode.TopLevel);
			
			if(objTargetObject == null || objTargetObject.Length == 0)
			{
				return "";
			}
			
			//get the asset path for the target 
			List<string> strAssetPaths = new List<string>(objTargetObject.Length) ;
			
			foreach(UnityEngine.Object objObject in objTargetObject)
			{
				strAssetPaths.Add(AssetDatabase.GetAssetPath(objObject));
			}
			
			
			//get the asset inporter for an asset 
			List<AssetImporter> aimAssetImporter = new List<AssetImporter>(strAssetPaths.Count);
			
			foreach(string strAddress in strAssetPaths )
			{
				aimAssetImporter.Add(AssetImporter.GetAtPath(strAddress));
			}
			
			if(aimAssetImporter != null && aimAssetImporter.Count > 0 && aimAssetImporter[0] != null)
			{
				return aimAssetImporter[0].userData;
			}
			
			return "";
#else
            return "";
#endif
        }

        public static T GetSelectedObjectMetaDataClass<T>(Type typSelectionFilterOption)
		{
#if UNITY_EDITOR
            string strMetaData = GetSelectedObjectMetaDataString(typSelectionFilterOption);
			
			if(strMetaData == null || strMetaData == "" )
			{
				return  default(T);
			}
			
			T tDeserializedObject =  default(T);
			
			//setup serialization settings
			JsonSerializerSettings jssJsonSettings = new JsonSerializerSettings();
			
			jssJsonSettings.TypeNameHandling = TypeNameHandling.All;
			
			try
			{
				//set serializer to look for type settinggs 
				tDeserializedObject = JsonConvert.DeserializeObject<T>(strMetaData,jssJsonSettings);
			}
			catch
			{
				Debug.Log("Error occured during serialization");
				return default(T);
			}
			
			return tDeserializedObject;
#else
            return  default(T);
#endif
        }

        public static void ClearMetaDataOnSelection()
		{
		
		}
	}
}