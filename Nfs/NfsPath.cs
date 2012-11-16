using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {
	public static class NfsPath {

		public static String ToWin(string unixPath) {

			String[] parts = unixPath.Split(new char[] { Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder result = new StringBuilder();

			foreach (var part in parts) {
				result.Append(part);
				if (part == parts[0] && !part.Contains(Path.VolumeSeparatorChar)) {
					result.Append(Path.VolumeSeparatorChar);
				}
				result.Append(Path.DirectorySeparatorChar);
			}

			return result.ToString();
		}

		public static String ToUnix(string windowsPath) {

			String[] parts = windowsPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder result = new StringBuilder();

			foreach (var part in parts) {
				result.Append(part);
				if (part == parts[0] && !part.Contains(Path.VolumeSeparatorChar)) {
					result.Append(Path.VolumeSeparatorChar);
				}
				result.Append(Path.AltDirectorySeparatorChar);
			}

			return result.ToString();
		}

	}
}
