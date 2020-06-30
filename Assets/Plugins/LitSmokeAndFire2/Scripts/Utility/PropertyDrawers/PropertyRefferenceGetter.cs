using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using System;
using System.Reflection;

public static class PropertyRefferenceGetter 
{
#if UNITY_EDITOR
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

#endif
}
