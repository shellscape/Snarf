using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {
	public static class NfsHandlerManager {

		private static List<BaseHandler> _cache;

		static NfsHandlerManager() {
			_cache = new List<BaseHandler>();
		}

		public static void RegisterHandler(BaseHandler handler) {
			if (_cache.Contains(handler)) {
				throw new Exception("Handler has already been registered");
			}
			_cache.Add(handler);
		}

		public static int GetPort(int programId) {
			var handler = _cache.Where(o => o.ProgramID == programId).FirstOrDefault();

			if (handler != null) {
				return handler.Port;
			}
			return 0;
		}

		public static Boolean IsProgramRegistered(int programId) {
			var handler = _cache.Where(o => o.ProgramID == programId).FirstOrDefault();
			return handler != null;
		}
	}
}
