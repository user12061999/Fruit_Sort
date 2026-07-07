using System.Collections.Generic;

namespace HAVIGAME {
    public abstract class ModuleInitializer {
        public const int CORE_MODULE = -1000;
        public const int EXTEND_MODULE = -900;
        public const int PLUGIN = -500;
        public const int SERVICE = -100;
        public const int DEFAULT = 0;

        public static readonly Comparer comparer = new Comparer();

        public virtual int Order => DEFAULT;

        public virtual bool WaitForInitialized => false;

        public abstract InitializeEvent InitializeEvent { get; }

        public ModuleInitializer() {

        }

        public abstract void Initialize();

        public class Comparer : IComparer<ModuleInitializer> {
            public int Compare(ModuleInitializer x, ModuleInitializer y) {
                return x.Order.CompareTo(y.Order);
            }
        }
    }
}
