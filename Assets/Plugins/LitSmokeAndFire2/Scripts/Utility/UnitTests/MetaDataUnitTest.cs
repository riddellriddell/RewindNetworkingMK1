using UnityEngine;
using System.Collections;
using SwordfishTools;
public class MetaDataUnitTest : MonoBehaviour 
{
#if UNITY_EDITOR
    [ButtonAttribute("SetMetaData","Set Meta Data")]
	public Object objSelectedObject;
	
	[ButtonAttribute("GetMetaData","Get Meta Data")]
	public bool _bGetSelectionMetaData;
	
	[ButtonAttribute("GetMetaDataClass","Get Meta Data Class")]
	public bool _bGetMetaDataClass;
	
	
	public string strMetaDataToSave;
	
	public string strMetaDataOnSelection;
	
	public TestMetaData _tmdMetaDataClass;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void GetMetaData()
	{
		strMetaDataOnSelection = MetaDataHelper.GetSelectedObjectMetaDataString(typeof(Texture2D));
	}
	
	public void GetMetaDataClass()
	{
		_tmdMetaDataClass = MetaDataHelper.GetSelectedObjectMetaDataClass<TestMetaData>(typeof(Texture2D));
	}
	
	public void SetMetaData()
	{
		TestMetaData tmdTestData = new TestMetaData();
		
		tmdTestData._strTestMeta = strMetaDataToSave;
		
		MetaDataHelper.SetSelectedObjectMetaData<TestMetaData>(tmdTestData,typeof(Texture2D));
	}

#endif

}

[System.Serializable]
public class TestMetaData
{
	public string _strTestMeta = "Test Meta";
}
