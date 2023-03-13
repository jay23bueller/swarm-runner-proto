using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ChainGenerator : MonoBehaviour
{

    private Vector3 _endPoint;
    private LineRenderer _lineRenderer;
    private bool _canDraw;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        //StartCoroutine(DrawRoutine());
        _lineRenderer.positionCount = 2;
    }


    private void LateUpdate()
    {
        _lineRenderer.SetPosition(0, transform.position);
        if (_canDraw)
        {
            _lineRenderer.SetPosition(1, _endPoint);
        }
        else
        {
            _lineRenderer.SetPosition(1, transform.position);
        }
    }




    private IEnumerator DrawRoutine()
    {
        while(true)
        {
            if (_canDraw)
            {
                float distance = Vector3.Distance(transform.position, _endPoint);
                Vector3 direction = (_endPoint - transform.position).normalized;

                float spaceBetweenPoints = distance / 15;

                for (int i = 0; i < 15; i++)
                {
                    _lineRenderer.positionCount = i + 1;

                    if (i > 0)
                        _lineRenderer.SetPosition(i, (direction * spaceBetweenPoints) + _lineRenderer.GetPosition(i - 1));
                    else
                        _lineRenderer.SetPosition(i, transform.position);

                    yield return null;
                }
            }

            yield return null;
        }
    }

    public void EnableLine(Vector3 point)
    {
        _canDraw = true;
        _endPoint = point;
    }


    public void DisableLine()
    {
        _canDraw = false;
    }




    //[SerializeField]
    //private float _diameter;
    //[SerializeField]
    //private GameObject _anchorPrefab;
    //[SerializeField]
    //private GameObject _cablePrefab;

    //private void Update()
    //{
    //    if(Input.GetMouseButtonDown(0))
    //    {
    //        Ray rayFromCenterOfViewport = Camera.main.ViewportPointToRay(new Vector3(.5f,.5f));
    //        if (Physics.SphereCast(rayFromCenterOfViewport, .3f, out RaycastHit hit, 50f))
    //        {
    //            if(hit.collider.CompareTag("Grapplable"))
    //            {
    //                Debug.Log(Vector3.Distance(hit.point, transform.position));
    //                GenerateChain(transform.position, hit.point);
    //            }
    //        }
    //    }

    //}

    //private void GenerateChain(Vector3 startPoint, Vector3 endPoint)
    //{
    //    float numOfSpheres = Mathf.Ceil(Vector3.Distance(endPoint, startPoint) / (_cablePrefab.GetComponent<SphereCollider>().radius * 2f * transform.localScale.magnitude));
    //    Debug.Log(numOfSpheres);
    //    var direction = (endPoint - startPoint).normalized;
    //    var prevGameObject = GameObject.Instantiate(_anchorPrefab, transform.position, transform.rotation);


    //    for(int i = 1; i <= numOfSpheres; i++)
    //    {
    //        var go = GameObject.Instantiate(_cablePrefab, startPoint + (numOfSpheres* (direction * _cablePrefab.GetComponent<SphereCollider>().radius * 2f * transform.localScale.magnitude)), transform.rotation);
    //        go.GetComponent<CharacterJoint>().connectedBody = prevGameObject.GetComponent<Rigidbody>();
    //        prevGameObject = go;

    //        if(i == numOfSpheres - 1)
    //        {
    //            prevGameObject.GetComponent<Rigidbody>().isKinematic = true;
    //        }
    //    }
    //}
}
