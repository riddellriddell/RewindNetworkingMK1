using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace StringExtensionMethods
{
	public static class StringExtensionMethods 
	{
		public enum SplitOptions
		{
			BEFORE_DELIMITER,
			AFTER_DELIMITER
		}

		public static string[] SplitAndKeep(this string strTargetString, char[] delims, SplitOptions splSplitOptions = SplitOptions.BEFORE_DELIMITER)
		{
			int start = 0;
			int index = 0;

			List<string> strSplitString = new List<string>();

			int iCrazyStop = 0;

			while ((index = strTargetString.IndexOfAny(delims, start)) != -1)
			{
				iCrazyStop++;

				if(iCrazyStop > 9000)
				{
					Debug.LogError("infinite loop detected");
					break;
				}

				


				if(splSplitOptions == SplitOptions.BEFORE_DELIMITER)
				{
					//add string 
					strSplitString.Add(strTargetString.Substring(Mathf.Clamp((start -1), 0 , int.MaxValue),index - Mathf.Clamp((start -1), 0 , int.MaxValue)));
					index++;
				}

				
				if(splSplitOptions == SplitOptions.AFTER_DELIMITER)
				{
					index++;
					//add string 
					strSplitString.Add(strTargetString.Substring(start,index - start));
				
				}

				int iHolder = start;
				start = index;
				
				index = iHolder;
				
				
				
				
			}


			//return the final string component
			if(splSplitOptions == SplitOptions.BEFORE_DELIMITER)
			{
				//add string 
				strSplitString.Add(strTargetString.Substring(Mathf.Clamp((start -1), 0 , int.MaxValue)));

			}
			if(splSplitOptions == SplitOptions.AFTER_DELIMITER)
			{

				strSplitString.Add(strTargetString.Substring(start));
				
			}
			
			return strSplitString.ToArray();

			//return new string[]{"Derp"};
		}
	}
}