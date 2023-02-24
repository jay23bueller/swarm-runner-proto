using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorBehaviour : MonoBehaviour
{
    [SerializeField]
    private Joint _anchor;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            if (_anchor.connectedBody == null)
            {
                //_anchor.axis = (_anchor.transform.position - transform.position).normalized;
               
                _anchor.connectedBody = GetComponent<Rigidbody>();
            }
                
            else
                _anchor.connectedBody = null;
        }
    }



}
