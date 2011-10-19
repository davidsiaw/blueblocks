using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlocksLib.BaseClasses
{
	public struct Pair<T1, T2> {
		public T1 a;
		public T2 b;

		public override bool Equals(object obj) {
			Pair<T1, T2> otherPair = (Pair<T1, T2>)obj;
			return otherPair.a.Equals(a) && otherPair.b.Equals(b);
		}

		public override int GetHashCode() {
			return a.GetHashCode() ^ (b.GetHashCode() << 4) % 49157;
		}
	}
}
