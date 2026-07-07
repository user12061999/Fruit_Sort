using UnityEngine;

[CreateAssetMenu(fileName = "DailyLoginMission", menuName = "Game/Data/Mission/DailyLoginMission")]
public class DailyLoginMission : Mission<GameEvent.OnDailyLogin> {
    protected override void OnUpdate(GameEvent.OnDailyLogin args) {
        AddProgress(1);
    }
}
