using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME {
    public interface IEventArgs {

    }

    public class Dispatcher<T> {

        private class Listener {

            private readonly List<Action> listeners;
            private readonly List<Action> cache;
            private bool dispatching;

            public bool Dispatching => dispatching;

            public Listener(int capacity = 8) {
                this.listeners = new List<Action>(capacity);
                this.cache = new List<Action>(capacity);
                this.dispatching = false;
            }

            public void AddListener(Action callback) {
                listeners.Add(callback);
            }
            public void RemoveListener(Action callback) {
                listeners.Remove(callback);
            }
            public void Dispatch() {
                dispatching = true;
                cache.Clear();
                cache.AddRange(listeners);

                foreach (Action action in cache) {
                    action?.Invoke();
                }
                dispatching = false;
            }
        }

        private readonly Dictionary<T, Listener> listeners;

        public Dispatcher(int capacity = 64) {
            listeners = new Dictionary<T, Listener>(capacity);
        }

        public void AddListener(T key, Action callback) {
            Listener listener;
            if (listeners.TryGetValue(key, out listener)) {
                listener.AddListener(callback);
            } else {
                listener = new Listener();
                listener.AddListener(callback);
                listeners.Add(key, listener);
            }
        }

        public void RemoveListener(T key, Action callback) {
            Listener listener;
            if (listeners.TryGetValue(key, out listener)) {
                listener.RemoveListener(callback);
            }
        }

        public void Dispatch(T key) {
            Listener listener;
            if (listeners.TryGetValue(key, out listener)) {
                if (!listener.Dispatching) {
                    listener.Dispatch();
                } else {
                    Log.Error(Utility.Text.Format("[Dispatcher] Sending event {0} failed! Dispatcher is running.", typeof(T).Name));
                }
            }
        }
    }


    public class GenericDispatcher {

        private class Listener {

            private readonly Dictionary<int, Action<IEventArgs>> listeners;
            private readonly List<int> cache;
            private bool dispatching;

            public bool Dispatching => dispatching;

            public Listener(int capacity = 8) {
                this.listeners = new Dictionary<int, Action<IEventArgs>>(capacity);
                this.cache = new List<int>(capacity);
                this.dispatching = false;
            }

            public void AddListener<T>(Action<T> callback) where T : IEventArgs {
                int key = callback.GetHashCode();
                listeners[key] = ToConvert(callback);
            }

            public void RemoveListener<T>(Action<T> callback) where T : IEventArgs {
                int key = callback.GetHashCode();
                listeners.Remove(key);
            }

            public void Dispatch<T>(T eventArgs) where T : IEventArgs {
                dispatching = true;
                cache.Clear();
                cache.AddRange(listeners.Keys);

                foreach (int key in cache) {
                    Action<IEventArgs> listener;
                    if (listeners.TryGetValue(key, out listener)) {
                        listener?.Invoke(eventArgs);
                    }
                }
                dispatching = false;
            }

            private Action<IEventArgs> ToConvert<T>(Action<T> callback) {
                return (eventArgs) => callback?.Invoke((T)eventArgs);
            }
        }

        private readonly Dictionary<Type, Listener> listeners;

        public GenericDispatcher(int capacity = 64) {
            listeners = new Dictionary<Type, Listener>(capacity);
        }

        public void AddListener<T>(Action<T> callback) where T : IEventArgs {
            Listener listener;
            Type key = typeof(T);
            if (listeners.TryGetValue(key, out listener)) {
                listener.AddListener(callback);
            }
            else {
                listener = new Listener();
                listener.AddListener(callback);
                listeners.Add(key, listener);
            }
        }

        public void RemoveListener<T>(Action<T> callback) where T : IEventArgs {
            Listener listener;
            if (listeners.TryGetValue(typeof(T), out listener)) {
                listener.RemoveListener(callback);
            }
        }

        public void Dispatch<T>(T eventArgs) where T : IEventArgs {
            Listener listener;
            if (listeners.TryGetValue(typeof(T), out listener)) {
                if (!listener.Dispatching) {
                    listener.Dispatch(eventArgs);
                } else {
                    Log.Error(Utility.Text.Format("[Dispatcher] Sending event {0} failed! Dispatcher is running.", typeof(T).Name));
                }
            }
        }
    }

    public static class EventDispatcher {
        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        private const int initializeCapacity = 64;
        private static Dispatcher<int> integerDispatcher = new Dispatcher<int>();
        private static Dispatcher<string> stringDispatcher = new Dispatcher<string>();
        private static GenericDispatcher genericDispatcher = new GenericDispatcher();

        public static bool IsInitialized => initializeEvent.IsInitialized;

        public static void Initialize() {
            if (initializeEvent.IsRunning) {
                Log.Warning("[EventDispatcher] EventDispatcher is running with initialize state {0}", IsInitialized);
                return;
            }

            integerDispatcher = new Dispatcher<int>(initializeCapacity);
            stringDispatcher = new Dispatcher<string>(initializeCapacity);
            genericDispatcher = new GenericDispatcher(initializeCapacity);

            Log.Info("[EventDispatcher] Initialize completed.");
            initializeEvent.Invoke(true);
        }

        #region Integer Dispatcher

        /// <summary>
        /// Add an event listener.
        /// </summary>
        /// <param name="key"> Unique key to the event. </param>
        /// <param name="callback"> Event callback. </param>
        public static void AddListener(int key, Action callback) {
            integerDispatcher.AddListener(key, callback);
        }

        /// <summary>
        /// Remove an event listener.
        /// </summary>
        /// <param name="key"> Unique key to filter events. </param>
        /// <param name="callback"> Event callback. </param>
        public static void RemoveListener(int key, Action callback) {
            integerDispatcher.RemoveListener(key, callback);
        }

        /// <summary>
        /// Broadcast an event.
        /// </summary>
        /// <param name="key"> Unique key to filter events. </param>
        public static void Dispatch(int key) {
            integerDispatcher.Dispatch(key);
        }

        #endregion

        #region String Dispatcher

        /// <summary>
        /// Add an event listener.
        /// </summary>
        /// <param name="key"> Unique key to the event. </param>
        /// <param name="callback"> Event callback. </param>
        public static void AddListener(string key, Action callback) {
            stringDispatcher.AddListener(key, callback);
        }

        /// <summary>
        /// Remove an event listener.
        /// </summary>
        /// <param name="key"> Unique key to filter events. </param>
        /// <param name="callback"> Event callback. </param>
        public static void RemoveListener(string key, Action callback) {
            stringDispatcher.RemoveListener(key, callback);
        }

        /// <summary>
        /// Broadcast an event.
        /// </summary>
        /// <param name="key"> Unique key to filter events. </param>
        public static void Dispatch(string key) {
            stringDispatcher.Dispatch(key);
        }

        #endregion

        #region Generic Dispatcher

        /// <summary>
        /// Add an event listener.
        /// </summary>
        /// <typeparam name="T"> The type of event. </typeparam>
        /// <param name="callback"> Event callback. </param>
        public static void AddListener<T>(Action<T> callback) where T : IEventArgs {
            genericDispatcher.AddListener(callback);
        }

        /// <summary>
        /// Remove an event listener.
        /// </summary>
        /// <typeparam name="T"> The type of event. </typeparam>
        /// <param name="callback"> Event callback. </param>
        public static void RemoveListener<T>(Action<T> callback) where T : IEventArgs {
            genericDispatcher.RemoveListener(callback);
        }

        /// <summary>
        /// Broadcast an event.
        /// </summary>
        /// <typeparam name="T"> The type of event. </typeparam>
        /// <param name="eventArgs"> Event argument. </param>
        public static void Dispatch<T>(T eventArgs) where T : IEventArgs {
            genericDispatcher.Dispatch(eventArgs);
        }

        #endregion


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => CORE_MODULE;
            public override InitializeEvent InitializeEvent => EventDispatcher.initializeEvent;

            public override void Initialize() {
                EventDispatcher.Initialize();
            }
        }
    }
}
