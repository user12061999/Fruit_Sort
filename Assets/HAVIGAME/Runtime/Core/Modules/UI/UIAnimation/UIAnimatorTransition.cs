using System;
using System.Collections;
using UnityEngine;

namespace HAVIGAME.UI {
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("HAVIGAME/UI/UI Transition/Animator Transition")]
    public class UIAnimatorTransition : UITransition {
        [SerializeField] private Animator animator;
        [SerializeField] private string showAnimationClip = "Show";
        [SerializeField] private string hideAnimationClip = "Hide";

        private int showTriggerHash;
        private int hideTriggerHash;
        private Coroutine animationCoroutine;

        public override bool IsPlaying => animator.IsInTransition(0);

        public override void Initialize() {
            showTriggerHash = Animator.StringToHash(showAnimationClip);
            hideTriggerHash = Animator.StringToHash(hideAnimationClip);
        }

        public override void PlayHideAnimation(Action onCompleted) {
            if (animationCoroutine != null) {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(PlayAnimation(hideTriggerHash, hideAnimationClip, onCompleted));
        }

        public override void PlayShowAnimation(Action onCompleted) {
            if (animationCoroutine != null) {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(PlayAnimation(showTriggerHash, showAnimationClip, onCompleted));
        }

        private IEnumerator PlayAnimation(int trigger, string animationClip, Action onCompleted = null) {
            animator.SetTrigger(trigger);

            bool stateReached = false;
            while (!stateReached) {
                if (!IsPlaying) {
                    stateReached = animator.GetCurrentAnimatorStateInfo(0).IsName(animationClip);
                }
                yield return Executor.Instance.WaitForEndOfFrame();
            }

            onCompleted?.Invoke();
        }
    }
}