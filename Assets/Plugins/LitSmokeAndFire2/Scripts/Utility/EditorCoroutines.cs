using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomEditorUtility
{
	public class EditorCoroutine
	{
		public static EditorCoroutine start( IEnumerator _routine )
		{

            EditorCoroutine coroutine = new EditorCoroutine(_routine);
			coroutine.start();
			return coroutine;

        }
		
		readonly IEnumerator routine;
		EditorCoroutine( IEnumerator _routine )
		{
			routine = _routine;
		}
		
		void start()
		{
#if UNITY_EDITOR
            //Debug.Log("start")
            EditorApplication.update += update;
#endif
        }
        public void stop()
		{
#if UNITY_EDITOR
            //Debug.Log("stop");
            EditorApplication.update -= update;
#endif
        }
		
		void update()
		{
#if UNITY_EDITOR
            /* NOTE: no need to try/catch MoveNext,
			 * if an IEnumerator throws its next iteration returns false.
			 * Also, Unity probably catches when calling EditorApplication.update.
			 */

            //Debug.Log("update");
            if (!routine.MoveNext())
			{
				stop();
			}

#endif
        }
	}
}