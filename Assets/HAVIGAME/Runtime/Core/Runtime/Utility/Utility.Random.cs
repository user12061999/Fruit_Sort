using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HAVIGAME {
    public static partial class Utility {
        public static class Random {
            public class ShuffleBag<T> : ICollection<T> {
                private List<T> bag;

                private int cursor = 0;

                public ShuffleBag() : this(4) {
                }

                public ShuffleBag(int capacity) {
                    bag = new List<T>(capacity);
                }

                public ShuffleBag(T[] initalValues) : this(initalValues.Length) {
                    for (int i = 0; i < initalValues.Length; i++) {
                        Add(initalValues[i]);
                    }
                }

                public ShuffleBag(IEnumerable<T> initalValues) : this(8) {
                    foreach (var item in initalValues) {
                        Add(item);
                    }
                }

                public T this[int index] {
                    get { return bag[index]; }
                    set { bag[index] = value; }
                }

                public T Next() {
                    if (cursor < 1) {
                        cursor = bag.Count - 1;
                        if (bag.Count < 1) {
                            return default(T);
                        }
                        return bag[0];
                    }

                    int grab = Mathf.FloorToInt(UnityEngine.Random.value * (cursor + 1));
                    T temp = bag[grab];
                    bag[grab] = this.bag[this.cursor];
                    bag[cursor] = temp;
                    cursor--;
                    return temp;
                }

                public int Count {
                    get { return bag.Count; }
                }

                public bool IsReadOnly {
                    get { return false; }
                }

                public int IndexOf(T item) {
                    return bag.IndexOf(item);
                }

                public bool Contains(T item) {
                    return bag.Contains(item);
                }

                public void Add(T item) {
                    bag.Add(item);
                    cursor = bag.Count - 1;
                }

                public bool Remove(T item) {
                    cursor = bag.Count - 2;
                    return bag.Remove(item);
                }

                public void RemoveAt(int index) {
                    cursor = bag.Count - 2;
                    bag.RemoveAt(index);
                }

                public void Clear() {
                    bag.Clear();
                }

                public void CopyTo(T[] array, int arrayIndex) {
                    for (int i = 0; i < Count; i++) {
                        array[arrayIndex + i] = this[i];
                    }
                }

                public IEnumerator<T> GetEnumerator() {
                    return bag.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator() {
                    return bag.GetEnumerator();
                }
            }

            public class Bag<T> : ICollection<T> {
                private List<T> bag;

                public Bag() : this(4) {
                }

                public Bag(int capacity) {
                    bag = new List<T>(capacity);
                }

                public Bag(T[] initalValues) : this(initalValues.Length) {
                    for (int i = 0; i < initalValues.Length; i++) {
                        Add(initalValues[i]);
                    }
                }

                public Bag(IEnumerable<T> initalValues) : this(8) {
                    foreach (var item in initalValues) {
                        Add(item);
                    }
                }

                public T this[int index] {
                    get { return bag[index]; }
                    set { bag[index] = value; }
                }

                public T Next() {
                    if (bag.Count < 1) {
                        return default(T);
                    }

                    int index = UnityEngine.Random.Range(0, bag.Count);
                    T temp = bag[index];
                    bag.RemoveAt(index);
                    return temp;
                }

                public int Count {
                    get { return bag.Count; }
                }

                public bool IsReadOnly {
                    get { return false; }
                }

                public int IndexOf(T item) {
                    return bag.IndexOf(item);
                }

                public bool Contains(T item) {
                    return bag.Contains(item);
                }

                public void Add(T item) {
                    bag.Add(item);
                }

                public bool Remove(T item) {
                    return bag.Remove(item);
                }

                public void RemoveAt(int index) {
                    bag.RemoveAt(index);
                }

                public void Clear() {
                    bag.Clear();
                }

                public void CopyTo(T[] array, int arrayIndex) {
                    for (int i = 0; i < Count; i++) {
                        array[arrayIndex + i] = this[i];
                    }
                }

                public IEnumerator<T> GetEnumerator() {
                    return bag.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator() {
                    return bag.GetEnumerator();
                }
            }
        }
    }
}
