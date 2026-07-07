using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME {
    public static class ReferencePool {
        private class Pool {
            private readonly Queue<IReferencePoolable> references;
            private readonly Type type;
            private int usingCount;
            private int acquireCount;
            private int releaseCount;
            private int addCount;
            private int removeCount;

            public Type ReferenceType => type;
            public int UsingReferenceCount => usingCount;
            public int AcquireReferenceCount => acquireCount;
            public int ReleaseReferenceCount => releaseCount;
            public int AddReferenceCount => addCount;
            public int RemoveReferenceCount => removeCount;

            public Pool(Type referenceType) {
                this.references = new Queue<IReferencePoolable>();
                this.type = referenceType;
                this.usingCount = 0;
                this.releaseCount = 0;
                this.releaseCount = 0;
                this.addCount = 0;
                this.removeCount = 0;
            }

            public T Acquire<T>() where T : class, IReferencePoolable, new() {
                if (typeof(T) != ReferenceType) {
                    throw new Exception("Type is invalid.");
                }

                usingCount++;
                acquireCount++;

                while (references.Count > 0) {
                    if (references.Dequeue() is T result) {
                        return result;
                    }
                }

                addCount++;
                return new T();
            }

            public IReferencePoolable Acquire() {
                usingCount++;
                acquireCount++;

                while (references.Count > 0) {
                    IReferencePoolable result = references.Dequeue();
                    if (result != null) {
                        return result;
                    }
                }

                addCount++;
                return (IReferencePoolable)Activator.CreateInstance(type);
            }

            public void Release(IReferencePoolable reference) {
                reference.Clear();

                references.Enqueue(reference);

                releaseCount++;
                usingCount--;
            }

            public void Remove() {
                removeCount += references.Count;
                references.Clear();
            }
        }

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private const int initializeCapacity = 64;
        private static Dictionary<Type, Pool> pools;

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static int Count => pools.Count;

        public static void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[ReferencePool] ReferencePool is running with initialize state {0}.", IsInitialized);
                return;
            }

            pools = new Dictionary<Type, Pool>(initializeCapacity);

            Log.Info("[ReferencePool] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        public static T Acquire<T>() where T : class, IReferencePoolable, new() {
            return GetPool(typeof(T)).Acquire<T>();
        }

        public static IReferencePoolable Acquire(Type referenceType) {
            return GetPool(referenceType).Acquire();
        }

        public static void Release(IReferencePoolable reference) {
            Type referenceType = reference.GetType();
            GetPool(referenceType).Release(reference);
        }

        public static void Remove<T>() where T : class, IReferencePoolable {
            GetPool(typeof(T)).Remove();
        }

        public static void Remove(Type referenceType) {
            GetPool(referenceType).Remove();
        }

        public static void Remove() {
            foreach (var item in pools) {
                item.Value.Remove();
            }
        }

        public static void Clear() {
            foreach (KeyValuePair<Type, Pool> referenceCollection in pools) {
                referenceCollection.Value.Remove();
            }

            pools.Clear();
        }

        private static Pool GetPool(Type referenceType) {
            Pool referenceCollection = null;
            if (!pools.TryGetValue(referenceType, out referenceCollection)) {
                referenceCollection = new Pool(referenceType);
                pools.Add(referenceType, referenceCollection);
            }

            return referenceCollection;
        }



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => CORE_MODULE - 1;
            public override InitializeEvent InitializeEvent => ReferencePool.initializeEvent;

            public override void Initialize() {
                ReferencePool.Initialize();
            }
        }
    }
}
