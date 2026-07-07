using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace HAVIGAME {
    public class Executor : Singleton<Executor> {
        private const int milisecondsPerSecond = 1000;
        private const int initializeCapacity = 64;

        private WaitForEndOfFrame waitForEndOfFrameCached;
        private Queue<Action> actionQueue;
        private Queue<IEnumerator> enumeratorQueue;
        private Queue<Coroutine> coroutineCancelQueue;
        private Dictionary<int, WaitForSeconds> waitForSecondCached;
        private Dictionary<int, WaitForSecondsRealtime> waitForSecondRealtimeCached;

        public WaitForEndOfFrame WaitForEndOfFrame() {
            return waitForEndOfFrameCached;
        }

        protected override void OnAwake() {
            base.OnAwake();

            waitForEndOfFrameCached = new WaitForEndOfFrame();
            actionQueue = new Queue<Action>(initializeCapacity);
            enumeratorQueue = new Queue<IEnumerator>(initializeCapacity);
            coroutineCancelQueue = new Queue<Coroutine>(initializeCapacity);
            waitForSecondCached = new Dictionary<int, WaitForSeconds>(initializeCapacity);
            waitForSecondRealtimeCached = new Dictionary<int, WaitForSecondsRealtime>(initializeCapacity);
        }

        public WaitForSeconds WaitForSeconds(float seconds) {
            int miliseconds = Mathf.CeilToInt(seconds * milisecondsPerSecond);
            if (waitForSecondCached.ContainsKey(miliseconds)) {
                return waitForSecondCached[miliseconds];
            }
            else {
                WaitForSeconds waitForSeconds = new WaitForSeconds(seconds);
                waitForSecondCached.Add(miliseconds, waitForSeconds);
                return waitForSeconds;
            }
        }

        public WaitForSeconds WaitForSeconds(int seconds) {
            int miliseconds = seconds * milisecondsPerSecond;
            if (waitForSecondCached.ContainsKey(miliseconds)) {
                return waitForSecondCached[miliseconds];
            }
            else {
                WaitForSeconds waitForSeconds = new WaitForSeconds(seconds);
                waitForSecondCached.Add(miliseconds, waitForSeconds);
                return waitForSeconds;
            }
        }

        public WaitForSecondsRealtime WaitForSecondsRealtime(float seconds) {
            int miliseconds = Mathf.CeilToInt(seconds * milisecondsPerSecond);
            if (waitForSecondRealtimeCached.ContainsKey(miliseconds)) {
                return waitForSecondRealtimeCached[miliseconds];
            }
            else {
                WaitForSecondsRealtime waitForSeconds = new WaitForSecondsRealtime(seconds);
                waitForSecondRealtimeCached.Add(miliseconds, waitForSeconds);
                return waitForSeconds;
            }
        }

        public WaitForSecondsRealtime WaitForSecondsRealtime(int seconds) {
            int miliseconds = seconds * milisecondsPerSecond;
            if (waitForSecondRealtimeCached.ContainsKey(miliseconds)) {
                return waitForSecondRealtimeCached[miliseconds];
            }
            else {
                WaitForSecondsRealtime waitForSeconds = new WaitForSecondsRealtime(seconds);
                waitForSecondRealtimeCached.Add(miliseconds, waitForSeconds);
                return waitForSeconds;
            }
        }

        public void Update() {
            int actionCount = actionQueue.Count;
            for (int i = 0; i < actionCount; i++) {
                if (actionQueue.Count > 0) {
                    Action action = actionQueue.Dequeue();
                    action?.Invoke();
                }
            }

            int enumeratorCount = enumeratorQueue.Count;
            for (int i = 0; i < enumeratorCount; i++) {
                if (enumeratorQueue.Count > 0) {
                    IEnumerator enumerator = enumeratorQueue.Dequeue();
                    if (enumerator != null) StartCoroutine(enumerator);
                }
            }

            int coroutineCancelCount = coroutineCancelQueue.Count;
            for (int i = 0; i < coroutineCancelCount; i++) {
                if (coroutineCancelQueue.Count > 0) {
                    Coroutine coroutine = coroutineCancelQueue.Dequeue();
                    if (coroutine != null) StopCoroutine(coroutine);
                }
            }
        }

        public Coroutine Run(IEnumerator enumerator) {
            return Instance.StartCoroutine(enumerator);
        }

        public void Stop(Coroutine coroutine) {
            Instance.StopCoroutine(coroutine);
        }

        public void Run(Action action) {
            action?.Invoke();
        }

        public Coroutine Run(Action action, float delay, bool ignoreTimeScale = false) {
            return Instance.StartCoroutine(IDelayEnumerator(action, delay, ignoreTimeScale));
        }

        public void RunOnMainTheard(IEnumerator enumerator) {
            lock (enumeratorQueue) {
                enumeratorQueue.Enqueue(enumerator);
            }
        }

        public void StopOnMainTheard(Coroutine coroutine) {
            lock (coroutineCancelQueue) {
                coroutineCancelQueue.Enqueue(coroutine);
            }
        }

        public void RunOnMainTheard(Action action) {
            lock (actionQueue) {
                actionQueue.Enqueue(action);
            }
        }

        public void RunOnMainTheard(Action action, float delay, bool ignoreTimeScale = false) {
            RunOnMainTheard(IDelayEnumerator(action, delay, ignoreTimeScale));
        }

        private IEnumerator IDelayEnumerator(Action action, float delay, bool ignoreTimeScale) {
            if (ignoreTimeScale) {
                yield return WaitForSecondsRealtime(delay);
            }
            else {
                yield return WaitForSeconds(delay);
            }
            action?.Invoke();
        }
    }
}