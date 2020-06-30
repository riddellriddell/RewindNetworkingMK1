#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System;
using System.Reflection;

public static class ReflectionUtility 
{

	//------------------------------------------------------- Value Getters -------------------------------------------------------

	public static object GetParent(SerializedProperty prop)
	{
		var path = prop.propertyPath.Replace(".Array.data[", "[");
		object obj = prop.serializedObject.targetObject;
		var elements = path.Split('.');
		foreach(var element in elements.Take(elements.Length-1))
		{
			if(element.Contains("["))
			{
				var elementName = element.Substring(0, element.IndexOf("["));
				var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[","").Replace("]",""));
				obj = GetValue(obj, elementName, index);
			}
			else
			{
				obj = GetValue(obj, element);
			}
		}
		return obj;
	}

	public static object GetParentChildValue(SerializedProperty prop,string strPropertyName)
	{
		//get parent 
		object objParent = GetParent(prop);

		return GetValue(objParent,strPropertyName);

	}

	public static T[] GetParentChildAttributes<T>(SerializedProperty prop,string strPropertyName ,bool bInherit = true)
	{
		//get parent 
		object objParent = GetParent(prop);
		
		return GetAttributes(objParent,strPropertyName,bInherit,typeof(T)) as T[];
		
	}

	public static object[] GetParentChildAttributes(SerializedProperty prop,string strPropertyName ,bool bInherit = true)
	{
		//get parent 
		object objParent = GetParent(prop);
		
		return GetAttributes(objParent,strPropertyName,bInherit);
		
	}
	
	public static object GetValue(object source, string name)
	{
		if(source == null)
			return null;
		var type = source.GetType();
		var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		if(f == null)
		{
			var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if(p == null)
				return null;
			return p.GetValue(source, null);
		}
		return f.GetValue(source);
	}

	public static object[] GetAttributes(object source, string name ,bool bInherit = true, Type typAttributeType = null)
	{
		if(source == null)
		{
			return null;
		}

		//get source type
		Type typSourceType = source.GetType();

		//get field 
		FieldInfo fdiFieldInfo = typSourceType.GetField(name);

		if(fdiFieldInfo == null)
		{
			return null;
		}

		if(typAttributeType != null)
		{
			return fdiFieldInfo.GetCustomAttributes(typAttributeType,bInherit);
		}

		return fdiFieldInfo.GetCustomAttributes(bInherit);
	}

	public static object GetValue(object source, string name, int index)
	{
		var enumerable = GetValue(source, name) as IEnumerable;
		var enm = enumerable.GetEnumerator();
		while(index-- >= 0)
			enm.MoveNext();
		return enm.Current;
	}

	//---------------------------------------------------------- Method Callers --------------------------------------------------

	public static void CallMethodOnTarget(object source,string strFuctionName)
	{
		//create list of types 
		Type[] typTypeList = new Type[0];
		
		//create list of parameters
		object[] objObjectList = new object[0];
		
		CallMethodOnTarget(source,strFuctionName,typTypeList,objObjectList);

		return ;
	}

	public static void CallMethodOnTarget<Paramiter1>(object source,string strFuctionName,Paramiter1 prmParamiter1)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Paramiter1)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParamiter1};
		
		CallMethodOnTarget(source,strFuctionName,typTypeList,objObjectList);
		
		return ;
	}

	public static void CallMethodOnTarget<Paramiter1,Paramiter2>(object source,string strFuctionName,Paramiter1 prmParamiter1 , Paramiter2 prmParamiter2)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Paramiter1) , typeof(Paramiter2)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParamiter1 , prmParamiter2};
		
		CallMethodOnTarget(source,strFuctionName,typTypeList,objObjectList);
		
		return ;
	}

	public static void CallMethodOnTarget<Paramiter1,Paramiter2,Paramiter3>(object source,string strFuctionName,Paramiter1 prmParamiter1 , Paramiter2 prmParamiter2 , Paramiter3 prmParamiter3)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Paramiter1) , typeof(Paramiter2) , typeof(Paramiter3)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParamiter1 , prmParamiter2 , prmParamiter3};
		
		CallMethodOnTarget(source,strFuctionName,typTypeList,objObjectList);
		
		return ;
	}

	public static void CallMethodOnTarget<Paramiter1,Paramiter2,Paramiter3,Paramiter4>(object source,string strFuctionName,Paramiter1 prmParamiter1 , Paramiter2 prmParamiter2 , Paramiter3 prmParamiter3 , Paramiter4 prmParamiter4)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Paramiter1) , typeof(Paramiter2) , typeof(Paramiter3) , typeof(Paramiter4)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParamiter1 , prmParamiter2 , prmParamiter3 , prmParamiter4};
		
		CallMethodOnTarget(source,strFuctionName,typTypeList,objObjectList);
		
		return ;
	}

	public static void CallMethodOnTarget(object source,string strFuctionName,Type[] typParameterTypeList, object[] objParameters)
	{
		//error check
		if(source == null)
		{
			
			return ;
		}
		
		if(typParameterTypeList == null)
		{
			return ;
		}
		
		if(objParameters == null)
		{
			return ;
		}
		
		if(objParameters.Length != typParameterTypeList.Length)
		{
			return ;
		}
		
		//get type of source
		Type typSourceType = source.GetType();
		
		
		//get method list 
		MethodInfo[] mtiMethodInfoList = typSourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		
		//error check 
		if(mtiMethodInfoList == null || mtiMethodInfoList.Length == 0)
		{

			return ;
		}
		
		//get the return type
		Type typReturnType = typeof(void);
		
		//loop through all the funcitons on the source
		foreach(MethodInfo mtiMethod in mtiMethodInfoList)
		{
			//does the method pass the filter
			bool bIsTargetMethod = true;

			//check to see if name matches
			if(bIsTargetMethod == true && mtiMethod.Name != strFuctionName)
			{
				bIsTargetMethod = false;
			}

			//check to see if return type matches
			if(bIsTargetMethod == true && (typReturnType == mtiMethod.ReturnType) == false)
			{
				Debug.Log("return type Fail");

				//does the method pass the return t
				bIsTargetMethod = false;
			}
			

			
			//check to see if signature matchs
			if(bIsTargetMethod == true)
			{
				
				//get paramiters off the object
				ParameterInfo[] pmiParamiterInfoList = mtiMethod.GetParameters();
				
				//check if the paramiter list matches
				if(pmiParamiterInfoList != null && pmiParamiterInfoList.Length == typParameterTypeList.Length)
				{
					//loop through the paramiters
					for(int i = 0 ; i < typParameterTypeList.Length; i++)
					{
						//check to see if paramiter is target type
						if(typParameterTypeList[i] != pmiParamiterInfoList[i].ParameterType)
						{
							bIsTargetMethod = false;
							break;
						}
					}
				}
				else
				{
					bIsTargetMethod = false;
				}
				
			}
			
			//check to see if method passed filtering 
			if(bIsTargetMethod == true)
			{
				//invoke method
				try
				{
					
					
					mtiMethod.Invoke(source,objParameters);
					
					return;
				}
				catch
				{
					
					
					return ;
				}
			}
		}
		
		
		
		return ;
	}

	public static void CallFunctionOnTarget(object source,string strFuctionName)
	{
		//error check
		if(source == null)
		{
			
			return ;
		}
		
		//get type of source
		Type typSourceType = source.GetType();
		
		
		//get method list 
		MethodInfo[] mtiMethodInfoList = typSourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		
		//error check 
		if(mtiMethodInfoList == null || mtiMethodInfoList.Length == 0)
		{
			
			return ;
		}
		
		//get the return type
		Type typReturnType = typeof(void);
		
		//loop through all the funcitons on the source
		foreach(MethodInfo mtiMethod in mtiMethodInfoList)
		{
			//does the method pass the filter
			bool bIsTargetMethod = true;
			
			
			//check to see if return type matches
			if((typReturnType ==  mtiMethod.ReturnType) == false)
			{
				//does the method pass the return t
				bIsTargetMethod = false;
			}
			
			//check to see if name matches
			if(bIsTargetMethod == true && mtiMethod.Name != strFuctionName)
			{
				bIsTargetMethod = false;
			}
			
			//check to see if signature matchs
			if(bIsTargetMethod == true && mtiMethod.GetParameters() != null && mtiMethod.GetParameters().Length != 0 )
			{
				bIsTargetMethod = false;
			}
			
			//check to see if method passed filtering 
			if(bIsTargetMethod == true)
			{
				//invoke method
				try
				{
					
					mtiMethod.Invoke(source,new object[0]);
					return ;
				}
				catch
				{
					
					
					return;
				}
			}
		}
		
		
		return ;
	}

	public static ReturnType CallFunctionOnTarget<ReturnType>(object source,string strFuctionName)
	{
		//error check
		if(source == null)
		{
		
			return default(ReturnType);
		}

		//get type of source
		Type typSourceType = source.GetType();


		//get method list 
		MethodInfo[] mtiMethodInfoList = typSourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

		//error check 
		if(mtiMethodInfoList == null || mtiMethodInfoList.Length == 0)
		{

			return default(ReturnType);
		}

		//get the return type
		Type typReturnType = typeof(ReturnType);

		//loop through all the funcitons on the source
		foreach(MethodInfo mtiMethod in mtiMethodInfoList)
		{
			//does the method pass the filter
			bool bIsTargetMethod = true;


			//check to see if return type matches
			if(typReturnType.IsAssignableFrom( mtiMethod.ReturnType) == false)
			{
				//does the method pass the return t
				bIsTargetMethod = false;
			}

			//check to see if name matches
			if(bIsTargetMethod == true && mtiMethod.Name != strFuctionName)
			{
				bIsTargetMethod = false;
			}

			//check to see if signature matchs
			if(bIsTargetMethod == true && mtiMethod.GetParameters() != null && mtiMethod.GetParameters().Length != 0 )
			{
				bIsTargetMethod = false;
			}

			//check to see if method passed filtering 
			if(bIsTargetMethod == true)
			{
				//invoke method
				try
				{


					return (ReturnType)mtiMethod.Invoke(source,new object[0]);
				}
				catch
				{

					
					return default(ReturnType);
				}
			}
		}


		return default(ReturnType);
	}

	public static ReturnType CallFunctionOnTarget<ReturnType,Parameter1>(object source,string strFuctionName,Parameter1 prmParameter1)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Parameter1)};

		//create list of parameters
		object[] objObjectList = new object[]{prmParameter1};

		return CallFunctionOnTarget<ReturnType>(source,strFuctionName,typTypeList,objObjectList);
	}

	public static ReturnType CallFunctionOnTarget<ReturnType,Parameter1, Parameter2>(object source,string strFuctionName,Parameter1 prmParameter1 , Parameter2 prmParameter2)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Parameter1), typeof(Parameter2)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParameter1 , prmParameter2};
		
		return CallFunctionOnTarget<ReturnType>(source,strFuctionName,typTypeList,objObjectList);
	}

	public static ReturnType CallFunctionOnTarget<ReturnType,Parameter1, Parameter2,Parameter3>(object source,string strFuctionName,Parameter1 prmParameter1 , Parameter2 prmParameter2 , Parameter3 prmParameter3)
	{
		//create list of types 
		Type[] typTypeList = new Type[]{typeof(Parameter1), typeof(Parameter2),typeof(Parameter3)};
		
		//create list of parameters
		object[] objObjectList = new object[]{prmParameter1 ,prmParameter2 , prmParameter3};
		
		return CallFunctionOnTarget<ReturnType>(source,strFuctionName,typTypeList,objObjectList);
	}

	public static ReturnType CallFunctionOnTarget<ReturnType>(object source,string strFuctionName,Type[] typParameterTypeList, object[] objParameters)
	{
		//error check
		if(source == null)
		{

			return default(ReturnType);
		}

		if(typParameterTypeList == null)
		{
			return default(ReturnType);
		}

		if(objParameters == null)
		{
			return default(ReturnType);
		}

		if(objParameters.Length != typParameterTypeList.Length)
		{
			return default(ReturnType);
		}

		//get type of source
		Type typSourceType = source.GetType();
		
		
		//get method list 
		MethodInfo[] mtiMethodInfoList = typSourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		
		//error check 
		if(mtiMethodInfoList == null || mtiMethodInfoList.Length == 0)
		{

			
			return default(ReturnType);
		}
		
		//get the return type
		Type typReturnType = typeof(ReturnType);
		
		//loop through all the funcitons on the source
		foreach(MethodInfo mtiMethod in mtiMethodInfoList)
		{
			//does the method pass the filter
			bool bIsTargetMethod = true;
			
			
			//check to see if return type matches
			if(typReturnType.IsAssignableFrom( mtiMethod.ReturnType) == false)
			{
				//does the method pass the return t
				bIsTargetMethod = false;
			}
			
			//check to see if name matches
			if(bIsTargetMethod == true && mtiMethod.Name != strFuctionName)
			{
				bIsTargetMethod = false;
			}
			
			//check to see if signature matchs
			if(bIsTargetMethod == true)
			{

				//get paramiters off the object
				ParameterInfo[] pmiParamiterInfoList = mtiMethod.GetParameters();

				//check if the paramiter list matches
				if(pmiParamiterInfoList != null && pmiParamiterInfoList.Length == typParameterTypeList.Length)
				{
					//loop through the paramiters
					for(int i = 0 ; i < typParameterTypeList.Length; i++)
					{
						//check to see if paramiter is target type
						if(typParameterTypeList[i] != pmiParamiterInfoList[i].ParameterType)
						{
							bIsTargetMethod = false;
							break;
						}
					}
				}
				else
				{
					bIsTargetMethod = false;
				}

			}
			
			//check to see if method passed filtering 
			if(bIsTargetMethod == true)
			{
				//invoke method
				try
				{

					
					return (ReturnType)mtiMethod.Invoke(source,new object[0]);
				}
				catch
				{

					
					return default(ReturnType);
				}
			}
		}
		

		
		return default(ReturnType);
	}

	//--------------------------------------------------------- Nested Variable Listers ------------------------------------------

	public static List<ReflectedObjectData> GetNestedReflectedObjectDataOnObject(GameObject objTargetObject)
	{
		//list to hold all the objects
		List<ReflectedObjectData> _rodVariableList = new List<ReflectedObjectData>();
		
		//null check 
		if(objTargetObject == null)
		{
			Debug.Log ("null object Passed");
			return _rodVariableList;
		}
		
		
		//list to hold all the components on the target object
		List<Component> comComponentsOnTargetObject = GetAllComponentsInPrefab(objTargetObject);
		
		//log the number of components found
		Debug.Log(comComponentsOnTargetObject.Count.ToString() + " Components Found on target");
		
		int iTextBreak = 0;
		
		//loop through all the components and add there values to teh variable list
		foreach(Component comComponent in comComponentsOnTargetObject)
		{
			iTextBreak++;
			
			//get all the fields in the object
			List<ReflectedObjectData> rdfFieldList = GetAllReflectedObjectDataOnObject(comComponent);
			
			
			
			//loop through all the field objects
			foreach(ReflectedObjectData rdfField in rdfFieldList)
			{
				
				
				Debug.Log(rdfField._fldFieldInfo.Name + " variable is being added to to variable list its value is" + rdfField.TargetToString());
				
				//add all the unique values to the variable list
				AddObjectIfUniqueReflectionData(ref _rodVariableList,rdfField);
				
				
				
			}
			
			
		}
		
		
		//loop through all fields found so far and fetch any nested values
		// For Testing Purposes only reflect 1 object
		for(int i = 0 ; i < _rodVariableList.Count ; i++)
		{
			
			//check if refference is to a component or game object to stop circular refferences 
			if((_rodVariableList[i].TargetObject is Component) == false && (_rodVariableList[i].TargetObject is GameObject) == false && _rodVariableList[i].TargetObject  != null)
			{
				//get object type
				Type typTargetType = _rodVariableList[i].TargetObject.GetType();
				
				if(typTargetType.IsPrimitive == false && typTargetType.IsEnum == false)
				{
					Debug.Log("Getting Nested Values on " + _rodVariableList[i].GetType().ToString() );
					
					//get object nested values
					List<ReflectedObjectData> rodNestedObjects = GetAllReflectedObjectDataOnObject(_rodVariableList[i].TargetObject);
					
					
					
					if(rodNestedObjects != null)
					{
						//loop through nested values 
						foreach(ReflectedObjectData objNestedField in rodNestedObjects)
						{
							//add unique values to object list 
							AddObjectIfUniqueReflectionData(ref _rodVariableList , objNestedField);
						}
					}
				}
				else
				{
					Debug.Log ("Variable " +_rodVariableList[i].GetType().ToString() + " has no nested values ");
				}
			}
			else
			{
				Debug.Log ("Variable " +_rodVariableList[i].GetType().ToString() + " is not a valid sub search object ");
			}
		}
		
		//return list of all the nested game objects 
		return _rodVariableList;
	}


	public static List<ReflectedObjectData> GetNestedReflectedObjectDataOnObject(GameObject objTargetObject,Type[] typTypesToIgnore)
	{
		//error check
		if(typTypesToIgnore == null)
		{
			typTypesToIgnore = new Type[0];
		}

		//list to hold all the objects
		List<ReflectedObjectData> _rodVariableList = new List<ReflectedObjectData>();
		
		//null check 
		if(objTargetObject == null)
		{
			Debug.Log ("null object Passed");
			return _rodVariableList;
		}
		
		
		//list to hold all the components on the target object
		List<Component> comComponentsOnTargetObject = GetAllComponentsInPrefab(objTargetObject);
		
		//log the number of components found
		Debug.Log(comComponentsOnTargetObject.Count.ToString() + " Components Found on target");
		
		int iTextBreak = 0;
		
		//loop through all the components and add there values to teh variable list
		foreach(Component comComponent in comComponentsOnTargetObject)
		{
			iTextBreak++;
			
			//get all the fields in the object
			List<ReflectedObjectData> rdfFieldList = GetAllReflectedObjectDataOnObject(comComponent);
			
			
			
			//loop through all the field objects
			foreach(ReflectedObjectData rdfField in rdfFieldList)
			{
				//check if this type should be ignored
				bool bIgnore = false;

				foreach(Type typIgnoreType in typTypesToIgnore)
				{
					if(typIgnoreType != null)
					{
						if(typIgnoreType.IsAssignableFrom( rdfField._fldFieldInfo.FieldType) == true)
						{
							bIgnore = true;

							break;
						}
					}

				}

				//check if passed filtering
				if(bIgnore == false)
				{
				
					//Debug.Log(rdfField._fldFieldInfo.Name + " variable is being added to to variable list its value is" + rdfField.TargetToString());
					
					//add all the unique values to the variable list
					AddObjectIfUniqueReflectionData(ref _rodVariableList,rdfField);
				}
				
				
				
			}
			
			
		}
		
		
		//loop through all fields found so far and fetch any nested values
		// For Testing Purposes only reflect 1 object
		for(int i = 0 ; i < _rodVariableList.Count ; i++)
		{
			
			//check if refference is to a component or game object to stop circular refferences 
			if((_rodVariableList[i].TargetObject is Component) == false && (_rodVariableList[i].TargetObject is GameObject) == false && _rodVariableList[i].TargetObject  != null)
			{
				//get object type
				Type typTargetType = _rodVariableList[i].TargetObject.GetType();



				if(typTargetType.IsPrimitive == false && typTargetType.IsEnum == false)
				{
					//Debug.Log("Getting Nested Values on " + _rodVariableList[i].GetType().ToString() );
					
					//get object nested values
					List<ReflectedObjectData> rodNestedObjects = GetAllReflectedObjectDataOnObject(_rodVariableList[i].TargetObject);
					
					
					
					if(rodNestedObjects != null)
					{
						//loop through nested values 
						foreach(ReflectedObjectData objNestedField in rodNestedObjects)
						{

							//check if this type should be ignored
							bool bIgnore = false;
							
							foreach(Type typIgnoreType in typTypesToIgnore)
							{
								if(typIgnoreType != null)
								{
									if(typIgnoreType.IsAssignableFrom(objNestedField._fldFieldInfo.FieldType) == false)
									{
										bIgnore = true;
										
										break;
									}
								}
								
							}
							
							//check if passed filtering
							if(bIgnore == false)
							{

								//add unique values to object list 
								AddObjectIfUniqueReflectionData(ref _rodVariableList , objNestedField);
							}
						}
					}
				}
				else
				{
					//Debug.Log ("Variable " +_rodVariableList[i].GetType().ToString() + " has no nested values ");
				}
			}
			else
			{
				//Debug.Log ("Variable " +_rodVariableList[i].GetType().ToString() + " is not a valid sub search object ");
			}
		}
		
		//return list of all the nested game objects 
		return _rodVariableList;
	}

	/// <summary>
	/// get all the variables on a target object 
	/// </summary>
	/// <returns>The nested variables in game object.</returns>
	/// <param name="objTargetObject">Object target object.</param>
	public static List<object> GetNestedVariablesInGameObject(GameObject objTargetObject)
	{
		//list to hold all the objects
		List<object> _lstVariableList = new List<object>();
		
		//null check 
		if(objTargetObject == null)
		{
			Debug.Log ("null object Passed");
			return _lstVariableList;
		}
		
		
		//list to hold all the components on the target object
		List<Component> comComponentsOnTargetObject = GetAllComponentsInPrefab(objTargetObject);
		
		//log the number of components found
		Debug.Log(comComponentsOnTargetObject.Count.ToString() + " Components Found on target");
		
		//loop through all the components and add there values to teh variable list
		foreach(Component comComponent in comComponentsOnTargetObject)
		{
			//get all the fields in the object
			List<object> objFieldList = GetAllVariablesOnObject(comComponent);
			
			//loop through all the field objects
			foreach(object objField in objFieldList)
			{
				//add all the unique values to the variable list
				AddObjectIfUnique(ref _lstVariableList,objField);
			}
		}
		
		
		
		//		List<object> objFieldListFirstLayer = new List<object>();
		
		//loop through all fields found so far and fetch any nested values
		// For Testing Purposes only reflect 1 object
		for(int i = 0 ; i < _lstVariableList.Count ; i++)
		{
			
			//check if refference is to a component or game object to stop circular refferences 
			if((_lstVariableList[i] is Component) == false && (_lstVariableList[i] is GameObject) == false && _lstVariableList[i]  != null)
			{
				//get object type
				Type typTargetType = _lstVariableList[i].GetType();
				
				if(typTargetType.IsPrimitive == false && typTargetType.IsEnum == false)
				{
					Debug.Log("Getting Nested Values on " + _lstVariableList[i].GetType().ToString() );
					
					//get object nested values
					List<object> objNestedObjects = GetAllVariablesOnObject(_lstVariableList[i]);
					
					
					
					if(objNestedObjects != null)
					{
						//loop through nested values 
						foreach(object objNestedField in objNestedObjects)
						{
							//add unique values to object list 
							AddObjectIfUnique(ref _lstVariableList , objNestedField);
						}
					}
				}
				else
				{
					Debug.Log ("Variable " +_lstVariableList[i].GetType().ToString() + " has no nested values ");
				}
			}
			else
			{
				Debug.Log ("Variable " +_lstVariableList[i].GetType().ToString() + " is not a valid sub search object ");
			}
		}
		
		//return list of all the nested game objects 
		return _lstVariableList;
	}

	
	/// <summary>
	/// this fucntion gets al list of all the object variabels on a target object
	/// </summary>
	/// <returns>The all variables on object.</returns>
	/// <param name="objObject">Object object.</param>
	public static List<object> GetAllVariablesOnObject(object objObject)
	{
		//cerate list to hold all the preopertys
		List<object> objFieldList = new List<object>();
		
		//check for nulls
		if(objObject == null)
		{
			return new List<object>();
		}
		
		//get the type of the object
		Type typTargetObjectType = objObject.GetType();
		
		//create a list to hold all the type fields
		List<FieldInfo> fldFieldInfoList = new List<FieldInfo>();
		
		//add the inital field info to the list 
		fldFieldInfoList.AddRange(typTargetObjectType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
		
		//Debug.Log ("Child Type " + typTargetObjectType.ToString());
		
		//loop up through class hirachey
		while(typTargetObjectType.BaseType != null)
		{
			
			
			typTargetObjectType = typTargetObjectType.BaseType;
			
			//	Debug.Log ("BaseType " +  typTargetObjectType.ToString());
			
			fldFieldInfoList.AddRange(typTargetObjectType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
		}
		
		//get peropertys and add them to
		foreach(FieldInfo fldInfo in fldFieldInfoList)
		{
			//check if field is an array
			if(fldInfo.FieldType.IsArray == false)
			{
				//Debug.Log(fldInfo.FieldType.ToString() + " Is not an array");
				//get preoperty value
				object objField = fldInfo.GetValue(objObject);
				
				if(objField != null)
				{
					
					//add to property list
					objFieldList.Add(objField);
				}
			}
			else
			{
				Debug.Log("Unpacking Array");
				
				//get preoperty value
				object objField = fldInfo.GetValue(objObject);
				if(objField != null)
				{
					//cast target object to enumerabe
					IEnumerable enmArray = objField as IEnumerable;
					
					if(enmArray == null)
					{
						Debug.Log ("Array unpack error");
					}
					
					
					foreach(object objArrayObject in enmArray)
					{
						Debug.Log("Storing array element");
						
						if(objArrayObject != null)
						{
							//add to property list
							objFieldList.Add(objArrayObject);
						}
					}
				}
				
			}
		}
		
		//return property list
		return objFieldList;
	}
	
	/// <summary>
	/// Get reflection data on a target object 
	/// </summary>
	/// <returns>The all reflected object data on object.</returns>
	/// <param name="objObject">Object object.</param>
	public static List<ReflectedObjectData> GetAllReflectedObjectDataOnObject(object objObject)
	{
		//cerate list to hold all the preopertys
		List<ReflectedObjectData> rodReflectedObjectData = new List<ReflectedObjectData>();
		
		//check for nulls
		if(objObject == null)
		{
			return new List<ReflectedObjectData>();
		}
		
		//get the type of the object
		Type typTargetObjectType = objObject.GetType();
		
		//create a list to hold all the type fields
		List<FieldInfo> fldFieldInfoList = new List<FieldInfo>();
		
		//add the inital field info to the list 
		fldFieldInfoList.AddRange(typTargetObjectType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
		
		//Debug.Log ("Child Type " + typTargetObjectType.ToString());
		
		//loop up through class hirachey
		while(typTargetObjectType.BaseType != null)
		{
			
			
			typTargetObjectType = typTargetObjectType.BaseType;
			
			//Debug.Log ("BaseType " +  typTargetObjectType.ToString());
			
			fldFieldInfoList.AddRange(typTargetObjectType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
			
			//Debug.Log("Fields Fetched");
		}
		
		//get peropertys and add them to
		foreach(FieldInfo fldInfo in fldFieldInfoList)
		{
			
			ReflectedObjectData rodData = new ReflectedObjectData();
			
			rodData._objObjectParent = objObject;
			rodData._fldFieldInfo = fldInfo;
			
			//add to property list
			rodReflectedObjectData.Add(rodData);
			
		}
		
		//Debug.Log("returning Reflected object list ");
		
		//return property list
		return rodReflectedObjectData;
	}

	
	//-------------------------------------------- Internal Utilitys 

	public class ReflectedObjectData
	{
		//target object parent
		public object _objObjectParent;
		
		//target object reflection data
		public FieldInfo _fldFieldInfo;
		
		/// <summary>
		/// the value of the target object
		/// </summary>
		/// <value>The target object.</value>
		public object TargetObject
		{
			get
			{
				if(_objObjectParent != null && _fldFieldInfo !=  null)
				{
					return _fldFieldInfo.GetValue(_objObjectParent);
				}
				
				return null;
			}
		}
		
		public string TargetToString()
		{
			//check for nulls
			object objTarget = TargetObject;
			
			if(objTarget != null)
			{
				return objTarget.ToString();
			}
			
			return "null";
		}
		
		
	}

	
	/// <summary>
	/// Gets all components in prefab.
	/// </summary>
	/// <returns>The all components in prefab.</returns>
	/// <param name="objPrefab">Object prefab.</param>
	private static List<Component> GetAllComponentsInPrefab(GameObject objPrefab)
	{
		if(objPrefab == null)
		{
			Debug.Log ("null object Passed");
			return new List<Component>();
		}
		
		//create list to hold all the components
		List<Component> comComponentList = new List<Component>();
		
		//store components 
		comComponentList.AddRange(objPrefab.GetComponentsInChildren<Component>(true));
		
		return comComponentList;
	}

	/// <summary>
	/// Adds the object to the passed list only if the list does not already hold a refference to the object
	/// </summary>
	/// <param name="objList">Object list.</param>
	/// <param name="objObjectToAdd">Object object to add.</param>
	private static void AddObjectIfUnique(ref List<object> objList,object objObjectToAdd)
	{
		//check for nulls 
		if(objList == null)
		{
			objList = new List<object>();
		}

		if(objObjectToAdd == null)
		{
			return;
		}
		
		//loop through all objects 
		foreach(object objListObject in objList)
		{
			//check for duplicates
			if(objListObject == objObjectToAdd)
			{
				return;
			}
		}
		
		//add object to list
		objList.Add(objObjectToAdd);
	}
	
	private static void AddObjectIfUnique <T>(ref List<T> objList,T objObjectToAdd) where T: class
	{
		//check for nulls 
		if(objList == null)
		{
			objList = new List<T>();
		}

		
		if(objObjectToAdd == null)
		{
			return;
		}
		
		//loop through all objects 
		foreach(T objListObject in objList)
		{
			//check for duplicates
			if(objListObject == objObjectToAdd)
			{
				return;
			}
		}
		
		//add object to list
		objList.Add(objObjectToAdd);
	}
	
	private static void AddObjectIfUniqueReflectionData(ref List<ReflectedObjectData> refReflectionList,ReflectedObjectData rodReflectionData)
	{
		//check for nulls 
		if(refReflectionList == null)
		{
			refReflectionList = new List<ReflectedObjectData>();
		}

		
		if(rodReflectionData == null)
		{
			return;
		}
		
		//loop through all objects 
		foreach(ReflectedObjectData objListObject in refReflectionList)
		{
			//check for duplicates
			if(objListObject._objObjectParent == rodReflectionData._objObjectParent && objListObject._fldFieldInfo.Name == rodReflectionData._fldFieldInfo.Name)
			{
				return;
			}
		}
		
		//add object to list
		refReflectionList.Add(rodReflectionData);
	}

}

#endif
