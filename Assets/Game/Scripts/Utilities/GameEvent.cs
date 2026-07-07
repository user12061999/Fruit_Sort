using HAVIGAME;
using System;
using System.Collections.Generic;

public static class GameEvent {
    public class PlayerInventoryChanged : IEventArgs {
        public ItemContainer Inventory { get; private set; }
        public ItemStack ItemStackChange { get; private set; }
        public bool IsAdded { get; private set; }

        public PlayerInventoryChanged(ItemContainer inventory, ItemStack itemStackChange, bool isAdded) {
            Inventory = inventory;
            ItemStackChange = itemStackChange;
            IsAdded = isAdded;
        }
    }
    public class DestroyBlockShape : IEventArgs {
        public DestroyBlockShape() {
            
        }
    }
    public struct LevelTimeChanged : IEventArgs {
        public int TotalSeconds { get; private set; }
        public int ElapsedSeconds { get; private set; }
        public int RemainingSeconds { get; private set; }

        public LevelTimeChanged(int totalSeconds, int elapsedSeconds, int remainingSeconds) {
            TotalSeconds = totalSeconds;
            ElapsedSeconds = elapsedSeconds;
            RemainingSeconds = remainingSeconds;
        }
    }

    public struct HeartRegenChanged : IEventArgs {
        public int TotalSeconds { get; private set; }
        public int ElapsedSeconds { get; private set; }
        public int RemainingSeconds { get; private set; }

        public HeartRegenChanged(int totalSeconds, int elapsedSeconds, int remainingSeconds) {
            TotalSeconds = totalSeconds;
            ElapsedSeconds = elapsedSeconds;
            RemainingSeconds = remainingSeconds;
        }
    }

    public struct InfinityHeartChanged : IEventArgs {
        public bool IsInfinity { get; private set; }
        public DateTime InfinityTime { get; private set; }

        public InfinityHeartChanged(bool isInfinity, DateTime infinityTime) {
            IsInfinity = isInfinity;
            InfinityTime = infinityTime;
        }
    }

    public class LevelInfomationChanged : IEventArgs {
        
        public int CurrentCombo { get; private set; }
        public int CurrentStar { get; private set; }
        public int StarChanged { get; private set; }
        public float ComboTime { get; private set; }
        public bool DoubleStar { get; private set; }

        public LevelInfomationChanged(int currentCombo, int currentStar, int starChanged, float comboTime, bool doubleStar) {
            
            CurrentCombo = currentCombo;
            CurrentStar = currentStar;
            StarChanged = starChanged;
            ComboTime = comboTime;
            DoubleStar = doubleStar;
        }
    }

    public class MergeItem : IEventArgs
    {
       

        
    }

    public class BoosterUnlocked : IEventArgs {
        public int BoosterId { get; private set; }

        public BoosterUnlocked(int boosterId) {
            BoosterId = boosterId;
        }
    }

    public class LevelCountdownChanged : IEventArgs {
        public bool IsPaused { get; private set; }

        public LevelCountdownChanged(bool isPaused) {
            IsPaused = isPaused;
        }
    }

    public class OnDailyLogin : IEventArgs
    {
        public OnDailyLogin(DateTime seasonDay)
        {
            throw new NotImplementedException();
        }
    }

    public class MissionCompleted: IEventArgs
    {
        public MissionCompleted(string id)
        {
            throw new NotImplementedException();
        }
    }
}

