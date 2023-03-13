using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SwingingExample : MonoBehaviour
{
    private Rigidbody _rb;
    private SpringJoint _joint;
    [SerializeField]
    private Transform _cameraTransform;
    private float _currentPitch;
    private float _currentYaw;
    [SerializeField]
    private Transform _anchorTransform;
    private Vector3 inputVec;
    private bool _breakJoint = false;
    private Vector3 _jointEndPoint;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _currentPitch = transform.rotation.eulerAngles.x;
        _currentYaw = transform.rotation.eulerAngles.y;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }


    private void Update()
    {
       
        if(Input.GetMouseButtonDown(0))
        {
            Ray viewportCenterRay = Camera.main.ViewportPointToRay(new Vector3(.5f,.5f));
           
            if(Physics.SphereCast(viewportCenterRay.origin, .3f, viewportCenterRay.direction, out RaycastHit hit, 100f))
            {
                Debug.Log("Hit Something");
                Debug.DrawLine(viewportCenterRay.origin, hit.point, Color.blue, 10f);
                _jointEndPoint = hit.point;
                if (_joint == null)
                    _joint = gameObject.AddComponent<SpringJoint>();
                
                _joint.autoConfigureConnectedAnchor = false;
                _joint.spring = 2f;
                _joint.damper = 7f;
                _joint.anchor = transform.InverseTransformPoint(_anchorTransform.position);
                _joint.connectedAnchor = hit.point;
                _joint.maxDistance = hit.distance * .8f;
                _joint.minDistance = 0.5f;
                _joint.massScale = 4.5f;
                _rb.AddForce(Vector3.ProjectOnPlane(_rb.velocity, Vector3.up).normalized * 10f, ForceMode.Impulse);
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            if(_joint != null)
            {
                Destroy(_joint);
            }
        }

        if (_joint != null && _breakJoint)
        {
            Destroy(_joint);
            _breakJoint = false;
        }
            

    Vector3 mouseInput = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _currentPitch = Mathf.Clamp(_currentPitch + (-mouseInput.y * 100f * Time.deltaTime), -90f, 90f);
        _currentYaw += mouseInput.x * 100f * Time.deltaTime;


        inputVec = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;




        
    }

    private void FixedUpdate()
    {
        
        if(_joint == null)
        {
            _rb.MoveRotation(Quaternion.Euler(0f, _currentYaw, 0f));
            _rb.AddForce(transform.rotation * inputVec * 20f);
        }
        else
        {
            Vector3 crossProd = Vector3.Cross(_cameraTransform.right, (_jointEndPoint - _anchorTransform.position).normalized);
            Debug.DrawLine(_anchorTransform.position, (_anchorTransform.position + crossProd * 10f), Color.yellow);
            _rb.AddForce((crossProd * 20f * inputVec.z) + (_cameraTransform.right * 20f * inputVec.x));

            if (!_breakJoint && (Vector3.Dot(crossProd, Vector3.up) > .92f || Vector3.Dot(crossProd, Vector3.down) > .92f))
                _breakJoint = true;

        }
    }

    private void LateUpdate()
    {
        _cameraTransform.position = transform.TransformPoint(new Vector3(0f, 0.659999847f, 0f));

        _cameraTransform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
    }
}
