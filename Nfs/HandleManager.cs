using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {
	public static class HandleManager {

		private static object _lock = new object();
		private static List<String> _handles = new List<string>(){ null };

		public static uint GetHandle(String name){
			lock (_lock) {
				if (_handles.Contains(name)) {
					return (uint)_handles.IndexOf(name);
				}
				var handleId = _handles.Count;
				_handles.Add(name);

				return (uint)handleId;
			}
		}

		public static String GetName(uint handleId) {
			lock (_lock) {
				if (_handles.Count >= handleId) {
					return _handles[(int)handleId];
				}
				throw new Exception("HandleManager.GetName: handleId not found.");
			}
		}

	}
}
