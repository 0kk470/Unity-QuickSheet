using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
///
/// !!! Machine generated code !!!
///
/// A class which deriveds ScritableObject class so all its data 
/// can be serialized onto an asset data file.
/// 
[System.Serializable]
public class ExcelExample : ScriptableObject 
{	
    [HideInInspector] [SerializeField] 
    public string SheetName = "";
    
    [HideInInspector] [SerializeField] 
    public string WorksheetName = "";
    
    // Note: initialize in OnEnable() not here.
    public ExcelExampleData[] dataArray;
    
	private Dictionary<uint, ExcelExampleData> m_DataDic = new Dictionary<uint, ExcelExampleData>();
	
    void OnEnable()
    {		
//#if UNITY_EDITOR
        //hideFlags = HideFlags.DontSave;
//#endif
        // Important:
        //    It should be checked an initialization of any collection data before it is initialized.
        //    Without this check, the array collection which already has its data get to be null 
        //    because OnEnable is called whenever Unity builds.
        // 		
        if (dataArray == null)
            dataArray = new ExcelExampleData[0];
		for(int i = 0;i < dataArray.Length; ++i)
		{
			var key = dataArray[i].Id;
            if (m_DataDic.ContainsKey(key))
                continue;
            m_DataDic.Add(key, dataArray[i]);
		}
    }
	
	public Dictionary<uint, ExcelExampleData> GetExcelExampleDataDic()
	{
		return m_DataDic;
	}
	
	public ExcelExampleData GetExcelExampleData(uint id)
	{
		ExcelExampleData data;
        m_DataDic.TryGetValue(id, out data);
        return data;
	}
    
    //
    // Highly recommand to use LINQ to query the data sources.
    //

}
