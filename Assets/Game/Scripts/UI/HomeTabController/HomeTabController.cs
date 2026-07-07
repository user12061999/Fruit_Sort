using HAVIGAME.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeTabController : UITabManager {

    protected override void ShowDefaultTab(bool instant = false) {
        UITaskRunner runner = new UITaskRunner();

        runner.Add(new ActionTask(() => Switch<HomePanel>(false)));

        if (GameData.Player.Rate == 0 && GameData.Classic.LevelCompleted >= 5 && GameData.Classic.LevelCompleted % 5 == 0) {
            runner.Add(new ShowFrameTask<RatePanel>());
        }

        runner.Run(null);
        GameAdvertising.TryHideBannerAd();
    }

    protected override void OnBack() {
        if (Current is HomePanel) {
            Current.Back();
        } else {
            Switch<HomePanel>();
        }
    }

    private class UITaskRunner {
        private List<UITask> tasks;
        private int current;

        public UITaskRunner() {
            this.tasks = new List<UITask>();
            this.current = 0;
        }

        public void Add(UITask task) {
            this.tasks.Add(task);
        }

        public void Run(Action onCompleted) {
            if (current >= 0 && current < tasks.Count) {
                UITask task = tasks[current];

                task.Run(() => {
                    current++;
                    Run(onCompleted);
                });
            } else {
                onCompleted?.Invoke();
            }
        }
    }

    private abstract class UITask {
        public abstract void Run(Action onCompleted);
    }

    private class ShowFrameTask<F> : UITask where F : UIFrame {
        public override void Run(Action onCompleted) {
            UIManager.Instance.Push<F>().OnHiddenCallback(frame => onCompleted?.Invoke());
        }
    }

    private class ActionTask : UITask {
        private Action action;

        public ActionTask(Action action) {
            this.action = action;
        }

        public override void Run(Action onCompleted) {
            action?.Invoke();
            onCompleted?.Invoke();
        }
    }
}

