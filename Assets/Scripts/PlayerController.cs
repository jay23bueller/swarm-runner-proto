using System;
using System.Collections;
using UnityEngine;


[Flags]
public enum DetectionFlags
{ 
    None = 0,
    LeftSide = 1,
    RightSide = 2,
    Bottom = 4,
    Front = 8,
}

public enum PlayerMovementState
{
    Normal,
    WallRunning
}


public class PlayerController : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField]
    private float _moveSpeed;
    [SerializeField]
    private float _jumpForce;
    [SerializeField]
    private float _currentMultiplier = 1.0f;
    [SerializeField]
    private float _defaultMultiplier = 1.0f;

    [Header("Aerial Movement")]
    [SerializeField]
    private float _aerialMultiplier = 2.0f;


    [Header("Wall Movement")]
    [SerializeField]
    private float _offWallForce;
    [SerializeField]
    private float _verticallWallForce;
    private bool _justJumpedOffWall;


    [Header("Mouse Settings")]
    [SerializeField]
    [Range(0f,200f)]
    private float _mouseXSensitivity;
    [SerializeField]
    [Range(0f, 200f)]
    private float _mouseYSensitivity;
    private Vector2 mouseInput = Vector2.zero;

    //Rotation
    private Camera _camera;
    private float _currentPitch;

    [SerializeField]
    private string _wallMask;
    [SerializeField]
    private string _groundMask;

    
    private Rigidbody _rigidbody;
    private Vector3 _moveDirection = Vector3.zero;
    private CapsuleCollider _capsuleCollider;
    private PhysicMaterial _physicMaterial;
    

    //Detection and movement state
    private DetectionFlags _detectionFlags = DetectionFlags.None;
    private PlayerMovementState _playerMovementState = PlayerMovementState.Normal;
    private RaycastHit _bottomHit;
    private RaycastHit _frontHit;
    private RaycastHit _rightHit;
    private RaycastHit _leftHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _camera = GetComponentInChildren<Camera>();
        _currentPitch = _camera.transform.rotation.eulerAngles.x;
        _bottomHit = new RaycastHit();
        _rightHit = new RaycastHit();
        _leftHit = new RaycastHit();
        _physicMaterial = _capsuleCollider.material;

        
    }
    private void Update()
    {
        UpdateDetection();
        UpdateMovementInput();
        UpdateRotation();
        HandleJump();
       
       
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (_playerMovementState)
            {
                case PlayerMovementState.Normal:
                    if (_detectionFlags.HasFlag(DetectionFlags.Bottom))
                        _rigidbody.AddRelativeForce(transform.up * _jumpForce, ForceMode.Impulse);
                    break;
                case PlayerMovementState.WallRunning:
                    if (!_justJumpedOffWall)
                    {
                        Vector3 jumpDirection = Vector3.zero;

                        if (_detectionFlags.HasFlag(DetectionFlags.LeftSide))
                        {
                            if (_leftHit.collider != null)
                            {
                                jumpDirection = _leftHit.normal * _offWallForce + Vector3.up * _verticallWallForce;
                            }
                        }
                        else if (_detectionFlags.HasFlag(DetectionFlags.RightSide))
                        {
                            if (_rightHit.collider != null)
                            {
                                jumpDirection = _rightHit.normal * _offWallForce + Vector3.up * _verticallWallForce;
                            }
                        }
                        else
                        {
                            break;
                        }
                        _justJumpedOffWall = true;
                        StartCoroutine(ResetWallJumpRoutine());
                        _rigidbody.AddForce(jumpDirection, ForceMode.Impulse);
                    }


                    break;
            }

        }
    }

    private void UpdateMovementInput()
    {
        _moveDirection.x = Input.GetAxis("Horizontal");
        _moveDirection.z = Input.GetAxis("Vertical");
        _moveDirection = _rigidbody.rotation * _moveDirection;
    }

    private void UpdateRotation()
    {
        mouseInput.x = Input.GetAxis("Mouse X") * _mouseXSensitivity * Time.deltaTime;
        mouseInput.y = Input.GetAxis("Mouse Y") * _mouseYSensitivity * Time.deltaTime;


        _currentPitch = Mathf.Clamp(_currentPitch - mouseInput.y, -90f, 90f);
        _camera.transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);

        _rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.AngleAxis(mouseInput.x, transform.up));

    }

    private IEnumerator ResetWallJumpRoutine()
    {
        yield return new WaitForSeconds(.8f);
        _justJumpedOffWall = false;

    }

    private void UpdateDetection()
    {
        DetectionFlags tempFlags = DetectionFlags.None;
        if(!_justJumpedOffWall)
        {
            switch (_playerMovementState)
            {
                case PlayerMovementState.Normal:
                    
                    //Bottom
                    Ray bottomRay = new Ray(transform.position - (transform.up * _capsuleCollider.height * .4f), -transform.up);

                    bool detectedBelow = Physics.Raycast(bottomRay, out _bottomHit, 1f, LayerMask.GetMask(new string[]{ _groundMask, _wallMask}));

                    //Left
                    Ray leftRay = new Ray(transform.position + (transform.right * _capsuleCollider.radius * .8f), -transform.right);

                    bool detectedLeft = Physics.Raycast(leftRay, out _leftHit, 1f, LayerMask.GetMask(_wallMask));

                    //Right 
                    Ray rightRay = new Ray(transform.position - (transform.right * _capsuleCollider.radius * .8f), transform.right);

                    bool detectedRight = Physics.Raycast(rightRay, out _rightHit, 1f, LayerMask.GetMask(_wallMask));


                    if (detectedBelow)
                    {
                        tempFlags |= DetectionFlags.Bottom;
                        _currentMultiplier = _defaultMultiplier;
                        _physicMaterial.dynamicFriction = 1f;
                    }
                    else
                    {
                        _physicMaterial.dynamicFriction = 0f;

                        _currentMultiplier = _aerialMultiplier;
                    }
                        
                    if (detectedLeft)
                        tempFlags |= DetectionFlags.LeftSide;
                    if (detectedRight)
                        tempFlags |= DetectionFlags.RightSide;
                    _detectionFlags = tempFlags;

                    break;

                case PlayerMovementState.WallRunning:
                    
                    Ray rayDirectionToCheck = new Ray();
                    float orientation = -1f;
                    if (Vector3.Dot(transform.forward, Vector3.forward) < 0)
                        orientation = 1f;

                    bool detectedFront = Physics.Raycast(new Ray(transform.position, _rigidbody.velocity.normalized), out _frontHit, 1f, LayerMask.GetMask(_wallMask));

                    Debug.DrawRay(transform.position, _rigidbody.velocity.normalized, Color.blue, 5f);
                    if (detectedFront)
                        _detectionFlags |= DetectionFlags.Front; 



                    if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                    {
                        if(detectedFront)
                        {
                            _leftHit = _frontHit;
                        }
                        if (_leftHit.collider != null)
                        {
                            rayDirectionToCheck = new Ray(transform.position, orientation * (_rigidbody.rotation * _leftHit.normal));
                        }
                    }
                    else if ((_detectionFlags & DetectionFlags.RightSide) != 0)
                    {
                        if (detectedFront)
                        {
                            _rightHit = _frontHit;
                        }
                        if (_rightHit.collider != null)
                        {
                            rayDirectionToCheck = new Ray(transform.position, orientation * (_rigidbody.rotation * _rightHit.normal));
                        }
                    }

                    if (!Physics.Raycast(rayDirectionToCheck, 5f, LayerMask.GetMask(_wallMask)))
                    {
                        Debug.Log("Normal?");
                        Debug.Log(rayDirectionToCheck);
                        Debug.DrawRay(transform.position, rayDirectionToCheck.direction, Color.yellow, 10f);
                        _playerMovementState = PlayerMovementState.Normal;

                    }

                    break;
            }
        }
        




    }

    private void FixedUpdate()
    {
        

        switch (_playerMovementState)
        {
            case PlayerMovementState.Normal:

                //_rigidbody.velocity = _moveDirection * _moveSpeed * _currentMultiplier;
                _rigidbody.AddForce(_moveDirection * _moveSpeed * _currentMultiplier, ForceMode.Force);
                
                if (_detectionFlags.HasFlag(DetectionFlags.Bottom))
                    _rigidbody.drag = _rigidbody.velocity.magnitude * 1.5f / _moveSpeed;
                else
                    _rigidbody.drag = 1f;
                if(!_detectionFlags.HasFlag(DetectionFlags.Bottom) && (_detectionFlags & (DetectionFlags.LeftSide | DetectionFlags.RightSide)) != 0)
                {
                    
                    Debug.Log("Transition to wall");
                    _playerMovementState = PlayerMovementState.WallRunning;
                }

                break;
            case PlayerMovementState.WallRunning:
                if(!_justJumpedOffWall)
                {
                    Vector3 velocityDirection = Vector3.zero;
                    if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                    {
                        if (_leftHit.collider != null)
                        {
                            velocityDirection = Vector3.Cross(_leftHit.normal, transform.up);
                            Debug.DrawLine(transform.position, transform.position + velocityDirection * 5f, Color.red, 7f);
                            _rigidbody.velocity = velocityDirection * _moveSpeed;
                        }
                    }
                    else if ((_detectionFlags & DetectionFlags.RightSide) != 0)
                    {
                        if (_rightHit.collider != null)
                        {
                            velocityDirection = Vector3.Cross(_rightHit.normal, -transform.up);
                        }
                    }
                    else
                    {
                        _playerMovementState = PlayerMovementState.Normal;
                        break;
                    }
                    Debug.Log(velocityDirection);

                    _rigidbody.velocity = velocityDirection * _moveSpeed;
                    
                }
                break;

        }
        //CheckIfAtApexOfJump();
        

    }

    //private void CheckIfAtApexOfJump()
    //{
    //    if (_jumped == true)
    //    {
    //        if(_rigidbody.velocity.y  < 0f)
    //        {
    //            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, (Physics.gravity * 2f), Time.deltaTime);

    //            if (_rigidbody.velocity.y <= Physics.gravity.y * 2f || Physics.Raycast(new Ray(transform.position, -transform.up), 1f))
    //                _jumped = false;
    //        }

         
    //    }
    //}
}
