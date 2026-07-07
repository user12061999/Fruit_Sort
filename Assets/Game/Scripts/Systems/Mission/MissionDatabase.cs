using HAVIGAME;
using SimpleJSON;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionDatabase", menuName = "Database/MissionDatabase")]
public class MissionDatabase : Database<MissionDatabase, string, Mission> {

#if UNITY_EDITOR
    protected override bool Installable => true;
#endif

    public void StartMissions(bool restart = false) {
        foreach (var item in database) {
            StartMission(item, restart);
        }
    }

    public void StartMission(Mission mission, bool restart = false) {
        mission.Reset();

        if (restart) {
            GameData.Missions.ClearData(mission.Id);
        } else {
            string data = GameData.Missions.GetData(mission.Id);

            if (!string.IsNullOrEmpty(data)) {
                JSONNode node = JSONNode.Parse(data);
                mission.FromJSON(node);
            }
        }

        mission.Start();
    }

    public void SaveMissions() {
        foreach (var item in database) {
            if (item.State != MissionStates.None) {
                SaveMission(item);
            }
        }
    }

    public void SaveMission(Mission mission) {
        JSONNode node = mission.ToJSON();
        string data = node.ToString();
        GameData.Missions.SetData(mission.Id, data);
    }
}
