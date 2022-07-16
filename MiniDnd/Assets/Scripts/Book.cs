using System;
using System.Collections;
using TSUtils.Sounds;
using UnityEngine;

public class Book : MonoBehaviour
{
    [SerializeField] private float PageTransitionTime = 0.5f;
    [SerializeField] private SoundAsset PageFlipSound;
    [SerializeField] private BookPage RightPage;
    [SerializeField] private BookPage LeftPage;
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
        RightPage.TransitionOut(PageTransitionTime);
        LeftPage.TransitionOut(PageTransitionTime);
        yield return new WaitForSeconds(1.0f);
        contentUpdateCallback?.Invoke();
        RightPage.TransitionIn(PageTransitionTime);
        LeftPage.TransitionIn(PageTransitionTime);
    }
}