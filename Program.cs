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

			//var udp = new UdpHandler(111);
			//udp.PacketReceived += udp_PacketReceived;
			//udp.Start();

			var nfs = new NfsHandler();
			var mount = new MountHandler();
			var portmap = new PortmapHandler();

			nfs.Start();
			mount.Start();
			portmap.Start();

		}

		static void udp_PacketReceived(object sender, UdpPacketReceivedEventArgs e) {
			Console.WriteLine("data received from: " + e.ReceivedFrom.ToString());
			Console.WriteLine("data length: " + e.Packet.GetUInt().ToString());
		}

		static void udp_PacketReceived(byte[] bytes, string receivedFrom, bool isLocal, bool isLAN) {
			
		}
	}
}
