using System;
using HAVIGAME;
using HAVIGAME.SaveLoad;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData : SaveData {
    [SerializeField] private Profile profile;
    [SerializeField] private int rate;
    [SerializeField] private int seasonCount;
    [SerializeField] private long seasonDay;
    [SerializeField] private bool premium;

    public Profile Profile => profile;
    public int Rate => rate;
    public int SeasonCount => seasonCount;
    public DateTime SeasonDay => new DateTime(seasonDay);
    public bool IsPremium {
        get {
#if CHEAT
            return true;
#else
            return premium;
#endif
        }
    }

    public PlayerSaveData() : base() {
        profile = new Profile("Player", "avatar_0", "us");
        rate = 0;
        seasonCount = 0;
        premium = false;
        seasonDay = 0;
        SetChanged();
    }

    public void ChangeName(string name) {
        if (this.profile.ChangeName(name)) {
            GameAnalytics.SetProperty("name", name);
            SetChanged();
        }
    }

    public void ChangeAvatar(string avatar) {
        if (this.profile.ChangeAvatar(avatar)) {
            GameAnalytics.SetProperty("avatar", avatar);
            SetChanged();
        }
    }

    public void ChangeCountryISO(string countryISO) {
        if (this.profile.ChangeCountryISO(countryISO)) {
            GameAnalytics.SetProperty("country_iso", countryISO);
            SetChanged();
        }
    }

    public void OnRate(int rate) {
        if (this.rate != rate) {
            this.rate = rate;
            GameAnalytics.SetProperty("rate", rate.ToString());
            SetChanged();
        }
    }

    public void OnStartGameSeason() {
        /*seasonCount++;
        /*seasonCount++;
        GameAnalytics.SetProperty("played_season", seasonCount.ToString());

        if (DateTime.Today.CompareTo(SeasonDay) > 0) {
            seasonDay = DateTime.Today.Ticks;

            EventDispatcher.Dispatch(new GameEvent.OnDailyLogin(SeasonDay));
        }
        */

        SetChanged();
    }

    public void SetPremium(bool premium) {
        if (this.premium != premium) {
            this.premium = premium;
            GameAnalytics.SetProperty("premium", premium.ToString());
            SetChanged();
        }
    }
}

[System.Serializable]
public class Profile {
    [SerializeField] private string name;
    [SerializeField] private string id;
    [SerializeField] private string avatar;
    [SerializeField] private string countryISO;

    public string Name => name;
    public string Id => id;
    public string Avatar => avatar;
    public string CountryISO => countryISO;

    public Profile(string name, string avatar, string countryISO) {
        this.id = Guid.NewGuid().ToString();
        this.name = name;
        this.avatar = avatar;
        this.countryISO = countryISO;
    }

    public bool ChangeName(string name) {
        if (!this.name.Equals(name)) {
            this.name = name;
            return true;
        }
        return false;
    }

    public bool ChangeAvatar(string avatar) {
        if (!this.avatar.Equals(avatar)) {
            this.avatar = avatar;
            return true;
        }
        return false;
    }

    public bool ChangeCountryISO(string countryISO) {
        if (!this.countryISO.Equals(countryISO)) {
            this.countryISO = countryISO;
            return true;
        }
        return false;
    }
}