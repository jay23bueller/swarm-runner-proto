using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


[Flags]
public enum DetectionFlags
{
    None = 0,
    LeftSide = 1,
    RightSide = 2,
    Bottom = 4,
    Front = 8,
    Slope = 16
}

public enum PlayerMovementState
{
    Ground,
    Slope,
    Air,
    WallRunning
}

public enum JumpState
{
    Ready,
    NotifyFixed,
    InProgress,
    Resetting
}


public class PlayerController : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField]
    private float _moveSpeed;
    [SerializeField]
    private float _moveForce;
    [SerializeField]
    private float _currentMultiplier = 1.0f;
    [SerializeField]
    private float _defaultMultiplier = 1.0f;

    [Header("Wall")]
    [Range(0f, 50f)]
    [SerializeField]
    private float _wallRunSpeed;
    [SerializeField]
    private float _wallJumpUpForce = 25f;
    [SerializeField]
    private float _wallJumpNormalForce = 30f;

    [Header("Jump")]
    [SerializeField]
    private float _jumpForce = 50f;
    private JumpState _jumpState = JumpState.Ready;
    private Coroutine _jumpResetRoutine;

    [Range(20f, 200f)]
    [SerializeField]
    private float _minAirForce;
    [Range(20f, 200f)]
    [SerializeField]
    private float _maxAirForce;
    private float _currentAirForce;
    [Range(1f, 50f)]
    [SerializeField]
    private float _airForceRate;


    [Header("Mouse Settings")]
    [SerializeField]
    [Range(0f, 200f)]
    private float _mouseXSensitivity;
    [SerializeField]
    [Range(0f, 200f)]
    private float _mouseYSensitivity;
    private Vector2 _mouseInput = Vector2.zero;

    //Rotation
    [SerializeField]
    private Camera _camera;
    private float _currentPitch;

    [SerializeField]
    private string _wallMask;
    [SerializeField]
    private string _groundMask;
    private string[] _groundLayerMasks = null;


    private Rigidbody _rigidbody;
    private Vector3 _moveDirection = Vector3.zero;
    private CapsuleCollider _capsuleCollider;
    private PhysicMaterial _physicMaterial;


    //Detection and movement state
    private DetectionFlags _detectionFlags = DetectionFlags.None;
    private PlayerMovementState _playerMovementState = PlayerMovementState.Ground;


    private RaycastHit _bottomHit;

    private RaycastHit _rightHit;
    private RaycastHit _rightFrontHit;
    private RaycastHit _rightBackHit;

    private RaycastHit _leftFrontHit;
    private RaycastHit _leftBackHit;
    private RaycastHit _leftHit;

    private bool _justTurnedACorner = false;


    private float _currentYaw;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _physicMaterial = _capsuleCollider.material;
        _groundLayerMasks = new string[] { _groundMask, _wallMask };
        _currentAirForce = _minAirForce;
        
    }

    private void Start()
    {
        _currentPitch = _camera.transform.rotation.eulerAngles.x;
        _currentYaw = _camera.transform.rotation.eulerAngles.y;
    }

    private void LateUpdate()
    {

        _camera.transform.position = transform.TransformPoint(new Vector3(0f, 0.659999847f, 0f));

        _currentPitch = Mathf.Clamp(_currentPitch + _mouseInput.y, -90f, 90f);
        _currentYaw += _mouseInput.x;
        _camera.transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        
        
    }

    #region Update
    private void Update()
    {

        UpdateMovementState();

        //Rotation
        UpdateMouseInput();
        UpdateRotation();


        //Movement
        UpdateMovementInput();
        UpdateJump();
        UpdateMovement();



        //Detection and PlayerState
        UpdateDetection();



        UpdatePlayerPhysicMaterial();


        Debug.Log("=====CURRENT AIR FORCE=====");
        Debug.Log(_currentAirForce);
        Debug.Log("=====Y VELOCITY=====");
        Debug.Log(_rigidbody.velocity.y);
       

    }


    private PlayerMovementState _playerMovementStateWhenJumping;
    private void UpdateJump()
    {
        switch (_jumpState)
        {
            case JumpState.Ready:

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    switch (_playerMovementState)
                    {
                        case PlayerMovementState.Ground:
                            _rigidbody.velocity = Vector3.zero;
                            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                            break;
                        case PlayerMovementState.WallRunning:
                            Vector3 direction = Vector3.zero;

                            if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                                direction = _leftHit.normal;
                            else
                                direction = _rightHit.normal;

                            _playerMovementStateWhenJumping = _playerMovementState;
                            _rigidbody.AddForce(direction * _wallJumpNormalForce + Vector3.up * _wallJumpUpForce, ForceMode.Impulse);


                            _currentAirForce = _minAirForce;
                            _playerMovementState = PlayerMovementState.Air;
                            break;
                    }

                    _jumpState = JumpState.InProgress;
                    _jumpResetRoutine = StartCoroutine(ResetJumpRoutine());
                }

                break;
            case JumpState.Resetting:
                if (_playerMovementState != PlayerMovementState.Air)
                {
                    _jumpState = JumpState.Ready;
                    _playerMovementStateWhenJumping = _playerMovementState;


                }
                break;

        }
    }


    private IEnumerator ResetJumpRoutine()
    {

        for (int i = 0; i < 5f; i++)
        {
            if (_playerMovementState != PlayerMovementState.Air && _playerMovementStateWhenJumping != _playerMovementState)
            {

                _jumpState = JumpState.Ready;

                if (_jumpResetRoutine != null)
                    StopCoroutine(_jumpResetRoutine);
                break;
            }
            yield return new WaitForSeconds(.1f);
        }

     
            _jumpState = JumpState.Resetting;
        


    }

    private void UpdatePlayerPhysicMaterial()
    {
        if (_moveDirection.sqrMagnitude <= 0.002f && _playerMovementState != PlayerMovementState.Air && _jumpState != JumpState.InProgress)
            _physicMaterial.staticFriction = 1f;
        else
            _physicMaterial.staticFriction = 0f;

    }

    private void UpdateMouseInput()
    {
        _mouseInput = new Vector2(Input.GetAxis("Mouse X") * _mouseXSensitivity, Input.GetAxis("Mouse Y") * _mouseYSensitivity * -1f);
        _mouseInput *= Time.deltaTime;
    }

    private void UpdateMovementInput()
    {
        _moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        _moveDirection = transform.rotation * _moveDirection;
    }

    private void UpdateMovement()
    {

        switch (_playerMovementState)
        {
            case PlayerMovementState.Ground:
                GroundMovementUpdate();
                break;
            case PlayerMovementState.Air:
                AirMovementUpdate();
                break;
        }



    }

    private void UpdateRotation()
    {


        
        
       // transform.Rotate(new Vector3(0f, _mouseInput.x, 0f));
       
    }



    private void AirMovementUpdate()
    {
        _currentMultiplier = .5f;
        _currentAirForce = Mathf.Clamp(_currentAirForce + _airForceRate * Time.deltaTime, _minAirForce, _maxAirForce);
       
    }




    private void UpdateDetection()
    {
        DetectionFlags tempFlags = DetectionFlags.None;


        //Bottom

        Ray bottomRay = new Ray(transform.position, -transform.up);


        bool detectedBelow = Physics.Raycast(bottomRay, out _bottomHit, _capsuleCollider.height * .5f + .4f, LayerMask.GetMask(_groundLayerMasks));
        Debug.DrawLine(bottomRay.GetPoint(0f), bottomRay.GetPoint(_capsuleCollider.height * .5f + .2f), Color.red);

        if (detectedBelow)
        {
            tempFlags |= DetectionFlags.Bottom;
        }


        bool detectedRight = false;
        bool detectedRightFront = false;
        bool detectedRightBack = false;

        bool detectedLeft = false;
        bool detectedLeftFront = false;
        bool detectedLeftBack = false;


        if (_playerMovementState == PlayerMovementState.WallRunning)
        {


            //Assumed when the player is in a Wall Running state, they already detected a wall to the left or right
            //trying to check if there is a wall in front or around the corners.
            //Left
            Vector3 directionToRayCast = _rigidbody.velocity.normalized;

            RaycastHit hit = new RaycastHit();

            //In Front
            bool detected = Physics.Raycast(transform.position + (directionToRayCast * _capsuleCollider.radius), _rigidbody.velocity.normalized, out hit, .8f, LayerMask.GetMask(_wallMask));

            if (detected)
            {
                if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                {
                    _leftHit = hit;
                    detectedLeft = true;
                }
                else
                {
                    _rightHit = hit;
                    detectedRight = true;
                }

            }
            else
            {
                //CheckCorners
                //LEFT
                if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                {
                    Ray leftFrontRay = new Ray(transform.position - (_leftHit.normal * _capsuleCollider.radius * .8f) - (_rigidbody.velocity.normalized * _capsuleCollider.radius * 1f), -_leftHit.normal);


                    Ray leftBackRay = new Ray(transform.position - (_leftHit.normal * _capsuleCollider.radius * .8f) - (_rigidbody.velocity.normalized * _capsuleCollider.radius * 3f), -_leftHit.normal);


                    CheckCorner(leftFrontRay, leftBackRay, ref detectedLeftFront, ref detectedLeftBack, ref detectedLeft, ref _leftFrontHit, ref _leftBackHit, ref _leftHit);


                }
                else //RIGHT
                {


                    Ray rightFrontRay = new Ray(transform.position - (_rightHit.normal * _capsuleCollider.radius * .8f) - (_rigidbody.velocity.normalized * _capsuleCollider.radius * 1f), -_rightHit.normal);


                    Ray rightBackRay = new Ray(transform.position - (_rightHit.normal * _capsuleCollider.radius * .8f) - (_rigidbody.velocity.normalized * _capsuleCollider.radius * 3f), -_rightHit.normal);


                    CheckCorner(rightFrontRay, rightBackRay, ref detectedRightFront, ref detectedRightBack, ref detectedRight, ref _rightFrontHit, ref _rightBackHit, ref _rightHit);
                }
            }
        }
        else
        {
            //Right 
            Ray rightRay = new Ray(transform.position + (transform.right * _capsuleCollider.radius * .8f), transform.right);

            detectedRight = Physics.Raycast(rightRay, out _rightHit, 1f, LayerMask.GetMask(_wallMask));

            //Left 
            Ray leftRay = new Ray(transform.position - (transform.right * _capsuleCollider.radius * .8f), -transform.right);

            detectedLeft = Physics.Raycast(leftRay, out _leftHit, 1f, LayerMask.GetMask(_wallMask));
        }


        if (detectedLeftFront || detectedLeftBack || detectedLeft)
            tempFlags |= DetectionFlags.LeftSide;
        if (detectedRightFront || detectedLeftBack || detectedRight)
            tempFlags |= DetectionFlags.RightSide;








        _detectionFlags = tempFlags;

    }

    private void CheckCorner(Ray frontRay, Ray backRay, ref bool detectedFront, ref bool detectedBack, ref bool detectedSide, ref RaycastHit frontHit, ref RaycastHit backHit, ref RaycastHit sideHit)
    {
        RaycastHit hit = new RaycastHit();


        detectedFront = Physics.Raycast(frontRay, out frontHit, 1.1f, LayerMask.GetMask(_wallMask));
        detectedBack = Physics.Raycast(backRay, out backHit, 1.2f, LayerMask.GetMask(_wallMask));

        if (_justTurnedACorner)
        {
            detectedBack = Physics.Raycast(backRay, out backHit, 1.2f, LayerMask.GetMask(_wallMask));
            Debug.DrawLine(backRay.GetPoint(0f), backRay.GetPoint(1.2f), Color.red);
            if (detectedBack)
            {
                _justTurnedACorner = false;
            }
            detectedSide = true;
        }
        else if (!detectedFront && detectedBack)
        {
            Vector3 startPosition = transform.position + (_rigidbody.velocity.normalized * (_capsuleCollider.radius * 2f)) + (-sideHit.normal * 2f);

            bool detected = Physics.Raycast(startPosition, -_rigidbody.velocity.normalized, out hit, 2.2f, LayerMask.GetMask(_wallMask));

            Debug.DrawLine(startPosition, startPosition + (-_rigidbody.velocity.normalized * 2f), Color.blue);
            if (detected && !detectedFront)
            {
                sideHit = hit;
                detectedFront = true;
                _justTurnedACorner = true;

            }
        }
    }

    private void UpdateMovementState()
    {


        if ((_detectionFlags & DetectionFlags.Bottom) != 0)
        {
            _playerMovementState = PlayerMovementState.Ground;
        }
        else if ((_detectionFlags & (DetectionFlags.LeftSide | DetectionFlags.RightSide)) != 0 && ((_jumpState == JumpState.InProgress && _playerMovementStateWhenJumping != PlayerMovementState.WallRunning) || (_jumpState != JumpState.InProgress)))
        {

            _playerMovementState = PlayerMovementState.WallRunning;


        }
        else
        {
            if(_playerMovementState != PlayerMovementState.Air)
            {
                _currentAirForce = _minAirForce;
            }

            _playerMovementState = PlayerMovementState.Air;
        }
            




    }


    private void GroundMovementUpdate()
    {
        _moveDirection = Vector3.ProjectOnPlane(_moveDirection, _bottomHit.normal).normalized;
        Debug.Log(_jumpState);
        if (_jumpState != JumpState.InProgress)
        {

            
            _rigidbody.drag = Mathf.Clamp(10f - _moveDirection.magnitude * 12f, 0f, 10f);
        }
        else
        {
            _rigidbody.drag = 0f;
        }
            

    }

    #endregion


    #region FixedUpdate
    private void FixedUpdate()
    {

        
        _rigidbody.MoveRotation(Quaternion.AngleAxis(_camera.transform.rotation.eulerAngles.y, Vector3.up));

        FixedUpdateRigidbodyGravity();


        switch (_playerMovementState)
        {
            case PlayerMovementState.Ground:
            case PlayerMovementState.Air:
                RegularMovementFixedUpdate();
                break;

        }

        //Max Speed
        ClampRigidbodyMaxVelocity();

    }


    private void FixedUpdateRigidbodyGravity()
    {
        switch (_playerMovementState)
        {
            case PlayerMovementState.Ground:

                _rigidbody.AddForce(-_bottomHit.normal * 100f);
                break;
            case PlayerMovementState.Air:
                _rigidbody.AddForce(Vector3.down * _currentAirForce);
                break;
        }

    }


    private void RegularMovementFixedUpdate()
    {

        _rigidbody.AddForce(_moveDirection * _moveForce * _currentMultiplier);
    }

    #endregion

    //Make sure the rigidbody's velocity doesn't exceed specifed max speed
    private void ClampRigidbodyMaxVelocity()
    {
        switch (_playerMovementState)
        {
            case PlayerMovementState.Ground:
            case PlayerMovementState.Air:
                Vector3 vecXZ = _rigidbody.velocity;
                vecXZ.y = 0f;
                if (_jumpState == JumpState.InProgress)
                {
                   
                    
                    if(_playerMovementState == PlayerMovementState.Ground)
                    {
                        if (_moveDirection.sqrMagnitude > 0f)
                        {
                            vecXZ = Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up);
                        }
                        else
                        {
                            vecXZ.x = 0f;
                            vecXZ.z = 0f;
                        }
                            

                    }
                        


                    if (vecXZ.magnitude > _moveSpeed)
                    {
                        vecXZ = vecXZ.normalized * _moveSpeed;
                    }

                    float y = Mathf.Clamp(_rigidbody.velocity.y, -40f, 40f);

                    _rigidbody.velocity = new Vector3(vecXZ.x, y, vecXZ.z);

                }
                else if (_rigidbody.velocity.magnitude > _moveSpeed && _playerMovementState == PlayerMovementState.Ground)
                {
                    _rigidbody.velocity = _rigidbody.velocity.normalized * _moveSpeed;
                } else 
                {
                    //In the Air
                    if (vecXZ.magnitude > _moveSpeed)
                        vecXZ = vecXZ.normalized * _moveSpeed;

                    vecXZ.y = Mathf.Clamp(_rigidbody.velocity.y, -40f, 40f);
                    _rigidbody.velocity = vecXZ;
                }

                break;
            case PlayerMovementState.WallRunning:

                if ((_detectionFlags & DetectionFlags.LeftSide) != 0)
                {

                    _rigidbody.velocity = Vector3.Cross(_leftHit.normal, Vector3.up) * _wallRunSpeed;

                }
                else if ((_detectionFlags & DetectionFlags.RightSide) != 0)
                {
                    //right
                    _rigidbody.velocity = Vector3.Cross(_rightHit.normal, Vector3.down) * _wallRunSpeed;
                }


                break;
        }

    }



}
