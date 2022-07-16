using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BookPage : MonoBehaviour
{
    private Renderer _renderer;
    private static readonly int Threshold = Shader.PropertyToID("_Threshold");
    private Coroutine _coroutine;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void TransitionIn(float transitionTime)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PageDissolveAnimation(0, 1, transitionTime));
    }

    public void TransitionOut(float transitionTime)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PageDissolveAnimation(1, 0, transitionTime));
    }


    private IEnumerator PageDissolveAnimation(float start, float end, float transitionTime)
    {
        var t = 0f;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            _renderer.material.SetFloat(Threshold, Mathf.Lerp(start, end, t / transitionTime));
            yield return null;
        }
    }
}