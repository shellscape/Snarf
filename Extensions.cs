using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snarf {
	public static class Extensions {

		public static Boolean Is(this FileAttributes attributes, FileAttributes value) {
			return ((attributes & value) == value);
		}

		public static void BubbleSort<T>(this IList<T> list) {
			BubbleSort<T>(list, Comparer<T>.Default);
		}

		private static void BubbleSort<T>(IList<T> list, IComparer<T> comparer) {
			bool stillGoing = true;
			while (stillGoing) {
				stillGoing = false;
				for (int i = 0; i < list.Count - 1; i++) {
					T x = list[i];
					T y = list[i + 1];
					if (comparer.Compare(x, y) > 0) {
						list[i] = y;
						list[i + 1] = x;
						stillGoing = true;
					}
				}
			}
		}

	}
}
