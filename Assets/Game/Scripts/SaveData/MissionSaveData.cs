using HAVIGAME.SaveLoad;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MissionSaveData : SaveData {
    [SerializeField] private List<MissionData> missions;

    public MissionSaveData() {
        this.missions = new List<MissionData>();
    }

    public string GetData(string id) {
        MissionData missionData = GetMissionData(id);

        if (missionData != null) {
            return missionData.data;
        } else {
            return null;
        }
    }

    public void SetData(string id, string data) {
        MissionData missionData = GetMissionData(id);

        if (missionData == null) {
            missionData = new MissionData(id, data);
            missions.Add(missionData);
        } else {
            missionData.data = data;
        }
        SetChanged();
    }

    public void ClearData(string id) {
        MissionData missionData = GetMissionData(id);

        if (missionData != null) {
            missions.Remove(missionData);
            SetChanged();
        }
    }

    public void ClearAll() {
        missions.Clear();
        SetChanged();
    }

    private MissionData GetMissionData(string missionId) {
        foreach (var mission in missions) {
            if (mission.id.Equals(missionId)) {
                return mission;
            }
        }

        return null;
    }

    public override void OnBeforeSave() {
        base.OnBeforeSave();

        MissionDatabase.Instance.SaveMissions();
    }

    [System.Serializable]
    private class MissionData {
        public string id;
        public string data;

        public MissionData(string id, string data) {
            this.id = id;
            this.data = data;
        }
    }
}
