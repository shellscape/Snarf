using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;
using Snarf.Nfs;

namespace Snarf {
	class Program {
		static void Main(string[] args) {

			MountManager.Init();
			HandleManager.Init();

			var nfs = new NfsHandler();
			var mount = new MountHandler();
			var portmap = new PortmapHandler();

			nfs.Start();
			mount.Start();
			portmap.Start();

		}
	}
}
