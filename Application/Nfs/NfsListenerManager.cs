using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {
	
	public static class NfsListenerManager {

		private static List<Tuple<int, int, UdpListener>> _cache;

		static NfsListenerManager() {
			_cache = new List<Tuple<int, int, UdpListener>>();
		}

		public static UdpListener GetListener(int port, int programId) {
			var tuple = _cache.Where(o => o.Item1 == port).FirstOrDefault();
			UdpListener listener = null;

			if (tuple == null) {
				listener = new UdpListener(port, false);
				listener.Start();

				_cache.Add(new Tuple<int,int,UdpListener>(port, programId, listener));
			}
			else {
				listener = tuple.Item3;
			}
			return listener;
		}

	}
}
