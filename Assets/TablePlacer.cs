using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TablePlacer : MonoBehaviour
{

    public GameObject table;

    private GameObject _tableAnchor;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!table) return;
        if (!_tableAnchor)
        {
            _tableAnchor = GameObject.FindGameObjectWithTag("TableAnchor");
        }
        if (!_tableAnchor) return;
        table.transform.position = _tableAnchor.transform.position;
        table.transform.rotation = _tableAnchor.transform.rotation;
        // TODO: scale such that virtual table is contained by physical table bounds
    }
}
