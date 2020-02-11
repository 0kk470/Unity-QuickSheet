using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var config = Resources.Load<ExcelExample>("ExcelExample");
        if(config != null)
        {
            var data = config.GetExcelExampleData(6);
            if (data != null)
                Debug.Log("get 6:" + data.Name);
            var dic = config.GetExcelExampleDataDic();
            if(dic != null)
            {
                foreach(var item in dic.Values)
                {
                    Debug.Log(item.Name);
                }
            }
        }
    }
}
