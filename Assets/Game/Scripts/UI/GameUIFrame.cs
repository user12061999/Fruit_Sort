using HAVIGAME.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameUIFrame : UIFrame {
    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("screen_go").Add("screen_name", gameObject.name.ToLower().Replace("(clone)", "")));
    }
}

public abstract class GameUITab : UITab {
    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("screen_go").Add("screen_name", gameObject.name.ToLower().Replace("(clone)", "")));
    }
}
