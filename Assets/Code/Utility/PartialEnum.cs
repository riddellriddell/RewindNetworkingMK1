using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Utility
{
    class PartialEnum
    {        
        public static void SetupPartialEnum<T>()
        {
            var bindingFlags = BindingFlags.Instance |
                   BindingFlags.Public | 
                   BindingFlags.Static;

            List<FieldInfo> finEnumTypes = typeof(T).GetFields(bindingFlags).ToList();

            //make sure they are organised in a deterministic way 
            finEnumTypes.Sort();

            int itterator = 0; 

            for(int i = 0; i < finEnumTypes.Count; i++)
            {
                finEnumTypes[i].SetValue(null, itterator);
                itterator++;
            }                                             
        }
    }
}
