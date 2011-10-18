using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace BlueBlocksLib.SetUtils {

	public class Set<T> : ICollection<T>, IEnumerable<T> {
		Dictionary<T, bool> set;

		public Set() {
			set = new Dictionary<T, bool>();
		}

		public Set(int size) {
			set = new Dictionary<T, bool>(size);
		}

		public void AddRange(IEnumerable<T> list) {
			foreach (T item in list) {
				set[item] = true;
			}
		}

		public bool Intersects(Set<T> other) {
			if (other.Count < this.Count) {
				foreach (T item in other) {
					if (this.Contains(item)) {
						return true;
					}
				}
			} else {
				foreach (T item in this) {
					if (other.Contains(item)) {
						return true;
					}
				}
			}
			return false;
		}

		public bool Intersects(IEnumerable<T> other) {
			foreach (T item in other) {
				if (this.Contains(item)) {
					return true;
				}
			}
			return false;
		}

		public T[] ToArray() {
			T[] arr = new T[set.Count];
			set.Keys.CopyTo(arr, 0);
			return arr;
		}

		#region ICollection<T> Members

		public void Add(T item) {
			set[item] = true;
		}

		public void Clear() {
			set.Clear();
		}

		public bool Contains(T item) {
			return set.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex) {
			set.Keys.CopyTo(array, arrayIndex);
		}

		public int Count {
			get { return set.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove(T item) {
			return set.Remove(item);
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			return new Enumerator(set);
		}

		#endregion

		public class Enumerator : IEnumerator<T> {

			Dictionary<T, bool>.Enumerator enumerator;
			Dictionary<T, bool> set;

			public Enumerator(Dictionary<T, bool> set) {
				enumerator = set.GetEnumerator();
				this.set = set;
			}

			#region IEnumerator<T> Members

			public T Current {
				get { return enumerator.Current.Key; }
			}

			#endregion

			~Enumerator() {
				Dispose(false);
			}

			private void Dispose(bool disposing) {
				if (disposing) {
					enumerator.Dispose();
				}
			}

			#region IDisposable Members

			public void Dispose() {
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			#region IEnumerator Members

			object IEnumerator.Current {
				get { return enumerator.Current; }
			}

			public bool MoveNext() {
				return enumerator.MoveNext();
			}

			public void Reset() {
				enumerator = set.GetEnumerator();
			}

			#endregion
		}

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return new Enumerator(set);
		}

		#endregion
	}
}
