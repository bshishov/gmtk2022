using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private SoundAsset ThrowCompletedSound;
    [SerializeField] private SoundAsset SelectedSound;

    [Header("Hover")] 
    [SerializeField] private float HoverAnimationTime = 0.2f;
    
    [Header("FX")]
    [SerializeField] private GameObject RollSuccessFx;
    [SerializeField] private GameObject CollisionFx;
    [SerializeField] private GameObject SelectedFx;

    private Rigidbody _body;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _isStandingStill;
    private float _lastSoundPlayedTime;
    private SoundManager.SoundHandler _selectedSound;
    
    // HoverAnimation
    private Vector3 _idlePosition;
    private Vector3 _hoverTargetPosition;
    private float _hoverProgress;
    private GameObject _selectedFxInstance;
    
    private const float VelocityThreshold = 0.05f;
    private const float AngularVelocityThreshold = 0.01f;

    private readonly Vector3[] _sides = {
        Vector3.up,
        Vector3.left,
        Vector3.back,
        Vector3.forward,
        Vector3.right,
        Vector3.down,
    };

    private ParticleSystem[] _particleSystems;
    private Quaternion _idleRotation;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();

        var t = transform;
        _startPosition = t.position;
        _startRotation = t.rotation;

        Physics.gravity = new Vector3(0, -20, 0);
        ToIdleState();
        
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    public void StopAllParticleSystems()
    {
        foreach (var system in _particleSystems) system.Stop();
    }
    
    public void StartAllParticleSystems()
    {
        foreach (var system in _particleSystems) system.Play();
    }

    private void ToIdleState()
    {
        _idlePosition = transform.position;
        _idleRotation = transform.rotation;
        _hoverTargetPosition = _idlePosition + new Vector3(0, 1f, 0);
        _hoverProgress = 0f;
        State = DieState.Idle;
        _body.isKinematic = true;
    }

    public void Throw(Vector3 impulse)
    {
        _body.isKinematic = false;

        impulse *= ThrowForce;
        
        Debug.Log($"Throwing cube {impulse}");
        _body.AddForceAtPosition(impulse, transform.position + new Vector3(0, YOffset + 2, 0));

        var sound = SoundManager.Instance.Play(ThrowSound);
        if (sound != null)
            sound.Volume *= Mathf.Clamp01(impulse.magnitude * 0.1f);

        StartCoroutine(Delayed(() =>
        {
            State = DieState.Rolling;
        }, 0.1f));
    }

    private IEnumerator Delayed(Action action, float delay)
    {
        if (action == null)
            yield break;
        yield return new WaitForSeconds(delay);
        action.Invoke();
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
            transform.Rotate(200f * Time.deltaTime, 200f * Time.deltaTime, 200f * Time.deltaTime);
        }

        if (State == DieState.Idle)
        {
            _hoverProgress = Mathf.Clamp01(_hoverProgress - Time.deltaTime / HoverAnimationTime);
            transform.position = Vector3.Lerp(_idlePosition, _hoverTargetPosition, _hoverProgress);
            transform.rotation = _idleRotation;
        }
    }

    public void FixedUpdate()
    {
        if (State == DieState.Rolling)
        {
            var velocity = _body.velocity;
            var angularVelocity = _body.angularVelocity;
            var wasStanding = _isStandingStill;
            _isStandingStill = velocity.magnitude < VelocityThreshold &&
                               angularVelocity.magnitude < AngularVelocityThreshold;
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
                if (RollSuccessFx != null)
                    Instantiate(RollSuccessFx, transform.position, Quaternion.identity);
                RollingFinished?.Invoke();
                SoundManager.Instance.Play(ThrowCompletedSound);
            }
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
        if (_selectedFxInstance == null && SelectedFx)
        {
            _selectedFxInstance = Instantiate(SelectedFx, transform);
        }
        State = DieState.Selected;


        _selectedSound?.Stop();

        _selectedSound = SoundManager.Instance.Play(SelectedSound);
        _selectedSound.IsLooped = true;
    }
    
    public void Unselect()
    {
        if (_selectedFxInstance)
        {
            Destroy(_selectedFxInstance);
        }

        _selectedSound?.Stop();
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

        if (CollisionFx != null)
        {
            var collisionFxInstance = Instantiate(CollisionFx, transform.position, CollisionFx.transform.localRotation);
            Destroy(collisionFxInstance, 2f);
        }
    }
}