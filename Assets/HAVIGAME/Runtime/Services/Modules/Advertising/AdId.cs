using UnityEngine;

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public abstract class AdId {
        [SerializeField] protected AdGroup group;
        [SerializeField] protected LoadType autoLoadType = LoadType.All;

        public bool IsNullOrEmpty => string.IsNullOrEmpty(Current());

        public AdGroup Group => group;

        public LoadType AutoLoadType => autoLoadType;

        public virtual string Current() {
            return string.Empty;
        }

        public virtual string Next() {
            return Current();
        }

        public virtual string Reset() {
            return Current();
        }

        public abstract AdId Clone();
    }

    [System.Serializable]
    [CategoryMenu("Single ID")]
    public class SingleAdId : AdId {
        [SerializeField] protected StringPropertyReadonly id = StringPropertyReadonly.Create();

        public override string Current() {
            return id.Get();
        }

        public override AdId Clone() {
            SingleAdId adId = new SingleAdId();
            adId.group = group;
            adId.autoLoadType = autoLoadType;
            adId.id = id;
            return adId;
        }
    }

    [System.Serializable]
    [CategoryMenu("Cross Platform ID")]
    public class CrossPlatformAdId : AdId {
        [SerializeField] protected StringPropertyReadonly android = StringPropertyReadonly.Create();
        [SerializeField] protected StringPropertyReadonly ios = StringPropertyReadonly.Create();

        public override string Current() {
#if UNITY_ANDROID
            return android.Get();
#else
            return ios.Get();
#endif
        }

        public override AdId Clone() {
            CrossPlatformAdId adId = new CrossPlatformAdId();
            adId.group = group;
            adId.autoLoadType = autoLoadType;
            adId.android = android;
            adId.ios = ios;
            return adId;
        }
    }

    [System.Serializable]
    [CategoryMenu("Sequence ID", 1)]
    public class SequenceAdId : AdId {
        [SerializeField] protected StringPropertyReadonly[] sequenceIds;

        private int index = 0;

        public override string Current() {
            return sequenceIds[index].Get();
        }

        public override string Next() {
            index++;
            if (index >= sequenceIds.Length) {
                index = 0;
            }

            return Current();
        }

        public override string Reset() {
            index = 0;
            return Current();
        }

        public override AdId Clone() {
            SequenceAdId adId = new SequenceAdId();
            adId.group = group;
            adId.autoLoadType = autoLoadType;
            adId.sequenceIds = sequenceIds;
            return adId;
        }
    }
}
