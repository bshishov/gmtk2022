using System;
using TSUtils.Sounds;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RealDie : MonoBehaviour
{
    public enum DieState
    {
        Disabled,
        Idle,
        Selected,
        Rolling,
    }
    public DieState State { get; private set; }
    public int Value { get; private set; }
    
    public event Action MouseEnter;  
    public event Action MouseExit;
    public event Action RollingFinished;

    [Header("Mechanic")]
    public float ThrowForce = 1f;
    public float YDirection;
    public float YOffset = 0.5f;

    [Header("Sounds")] 
    [SerializeField] private SoundAsset ThrowSound; 
    [SerializeField] private SoundAsset CollideSound;

    [Header("Hover")] 
    public float HoverAnimationTime = 0.2f;
    
    private Rigidbody _body;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _isStandingStill;
    private float _lastSoundPlayedTime;
    
    // HoverAnimation
    private Vector3 _idlePosition;
    private Vector3 _hoverTargetPosition;
    private float _hoverProgress;
    
    private const float VelocityThreshold = 0.05f;
    private const float AngularVelocityThreshold = 0.01f;

    private readonly Vector3[] _sides = {
        Vector3.up,
        Vector3.forward,
        Vector3.down,
        Vector3.back,
        Vector3.left,
        Vector3.right,
    };
    
    private void Start()
    {
        _body = GetComponent<Rigidbody>();

        var t = transform;
        _startPosition = t.position;
        _startRotation = t.rotation;

        Physics.gravity = new Vector3(0, -20, 0);
        ToIdleState();
    }

    private void ToIdleState()
    {
        _idlePosition = transform.position;
        _hoverTargetPosition = _idlePosition + new Vector3(0, 1f, 0);
        _hoverProgress = 0f;
        State = DieState.Idle;
        _body.isKinematic = true;
    }

    public void Throw(Vector3 impulse)
    {
        State = DieState.Rolling;
        _body.isKinematic = false;
        
        Debug.Log($"Throwing cube {impulse}");
        _body.AddForceAtPosition(impulse, transform.position + new Vector3(0, YOffset + 2, 0));

        var sound = SoundManager.Instance.Play(ThrowSound);
        if (sound != null)
            sound.Volume *= Mathf.Clamp01(impulse.magnitude * 0.1f);
    }
    
    public void ResetToOriginalPosition()
    {
        var t = transform;
        t.position = _startPosition;
        t.rotation = _startRotation;
        _body.velocity = Vector3.zero;
        _body.angularVelocity = Vector3.zero;
        
        ToIdleState();
    }

    public void Update()
    {
        if (State == DieState.Selected)
        {
            _hoverProgress = Mathf.Clamp01(_hoverProgress + Time.deltaTime / HoverAnimationTime);
            transform.position = Vector3.Lerp(_idlePosition, _hoverTargetPosition, _hoverProgress);
        }

        if (State == DieState.Idle)
        {
            _hoverProgress = Mathf.Clamp01(_hoverProgress - Time.deltaTime / HoverAnimationTime);
            transform.position = Vector3.Lerp(_idlePosition, _hoverTargetPosition, _hoverProgress);
        }
    }

    public void FixedUpdate()
    {
        var velocity = _body.velocity;
        var angularVelocity = _body.angularVelocity;

        var wasStanding = _isStandingStill;
        _isStandingStill = velocity.magnitude < VelocityThreshold && angularVelocity.magnitude < AngularVelocityThreshold;
        if (!wasStanding && _isStandingStill)
        {
            var localUp = transform.InverseTransformDirection(Vector3.up);

            var bestSideIndex = -1;
            var bestSideDotProduct = -1f;
            for (var i = 0; i < _sides.Length; i++)
            {
                var dot = Vector3.Dot(localUp, _sides[i]);
                if (dot > bestSideDotProduct)
                {
                    bestSideDotProduct = dot;
                    bestSideIndex = i;
                }
            }
            
            Value = bestSideIndex + 1;
            Debug.Log($"Value: {Value} Best side: {bestSideIndex} ({bestSideDotProduct})");
            ToIdleState();
            RollingFinished?.Invoke();
        }
    }
    
    private void OnMouseEnter()
    {
        if(State == DieState.Idle || State == DieState.Selected)
            MouseEnter?.Invoke();
    }

    private void OnMouseExit()
    {
        if(State == DieState.Idle || State == DieState.Selected)
            MouseExit?.Invoke();
    }

    public void Select()
    {
        State = DieState.Selected;
    }
    
    public void Unselect()
    {
        State = DieState.Idle;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(200, 10, 100, 20), State.ToString());
    }

    private void OnCollisionEnter(Collision other)
    {
        if(Time.time - _lastSoundPlayedTime < 0.05f)
            return;
        
        var sound = SoundManager.Instance.Play(CollideSound);
        _lastSoundPlayedTime = Time.time;
        if (sound != null)
        {
            sound.Volume *= Mathf.Clamp01(other.impulse.magnitude * 0.1f);
        }
    }
}