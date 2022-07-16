using System;
using System.Collections;
using TSUtils.Sounds;
using UnityEngine;

public class Book : MonoBehaviour
{
    [SerializeField] private SoundAsset PageFlipSound;
    [SerializeField] private BookPage Page;
    [SerializeField] private Animator Animator;
    
    private static readonly int FlipPageTriggerName = Animator.StringToHash("FlipPage");
    
    public void FlipPage(Action contentUpdateCallback)
    {
        Animator.SetTrigger(FlipPageTriggerName);
        StartCoroutine(TransitionRoutine(contentUpdateCallback));
        SoundManager.Instance.Play(PageFlipSound);
    }

    IEnumerator TransitionRoutine(Action contentUpdateCallback)
    {
        yield return new WaitForSeconds(0.8f);
        Page.TransitionOut();
        yield return new WaitForSeconds(1.0f);
        contentUpdateCallback?.Invoke();
        Page.TransitionIn();
    }
}