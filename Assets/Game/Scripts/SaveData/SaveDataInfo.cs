using HAVIGAME.SaveLoad;
using System;
using UnityEngine;

[System.Serializable]
public class SaveDataInfo : SaveData {
    public static int dataVersion = 0;

    [SerializeField] private int version;
    [SerializeField] private long timeCreated;
    [SerializeField] private long timeSaved;

    public override bool IsChanged => true;
    public int Version {
        get { return version; }
        set { version = value; }
    }

    public DateTime TimeCreated => new DateTime(timeCreated);
    public DateTime TimeSaved => new DateTime(timeSaved);

    public SaveDataInfo() : base() {
        version = dataVersion;
        timeCreated = DateTime.Now.Ticks;
        timeSaved = DateTime.Now.Ticks;
    }

    public override void OnBeforeSave() {
        base.OnBeforeSave();

        timeSaved = DateTime.Now.Ticks;
    }
}
