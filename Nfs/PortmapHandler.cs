using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {

	public class PortmapHandler : BaseHandler {

		public const int PORTMAP_PORT = 111;
		public const int PORTMAP_VERSION = 2;
		public const int PORTMAP_PROGRAMID = 100000;

		public const int PORTMAP_TRUE = 1;
		public const int PORTMAP_FALSE = 0;

		public enum PortmapProcedure : int {
			NULL = 0,
			SET = 1,
			UNSET = 2,
			GETPORT = 3,
			DUMP = 4,
			CALLIT = 5
		}

		public enum PortmapMessage : int {
			SUCCESS = 0,
			PROG_UNAVAIL = 1,
			PROG_MISMATCH = 2,
			PROC_UNAVAIL = 3,
			GARBAGE_ARGS = 4
		}

		public PortmapHandler() : base(PORTMAP_PORT, PORTMAP_PROGRAMID) {

		}

		protected override void Process(NfsPacket packet, IPEndPoint receivedFrom) {
			Console.WriteLine("PortmapHandler.Process : recievedFrom: " + receivedFrom.ToString());
			Console.WriteLine("PortmapHandler.Process: Procedure -> " + packet.ProcedureID + ":" + ((PortmapProcedure)packet.ProcedureID).ToString());

			switch (packet.ProcedureID) {
				case (int)PortmapProcedure.NULL:
					Null(packet, receivedFrom);
					break;
				case (int)PortmapProcedure.GETPORT:
					GetPort(packet, receivedFrom);
					break;
				case (int)PortmapProcedure.SET:
					break;
				case (int)PortmapProcedure.UNSET:
					break;
				case (int)PortmapProcedure.DUMP:
					break;
				case (int)PortmapProcedure.CALLIT:
					break;
				default:
					break;
			}
		}

		private void Null(NfsPacket sourcePacket, IPEndPoint receivedFrom) {
			// Put together an XDR reply packet
			NfsPacket packet = new NfsPacket(128);

			packet.SetUInt(sourcePacket.XID);
			packet.SetUInt((uint)RpcSignalType.Reply);
			packet.SetUInt((uint)RpcMessageResult.Accepted);

			// Put on a NULL authentication
			packet.AddNullAuthentication();

			packet.SetUInt((uint)RpcProcedure.Success);

			Send(packet, receivedFrom);
		}

		private void GetPort(NfsPacket sourcePacket, IPEndPoint receivedFrom) {

			// skip past the authentication records
			sourcePacket.ReadAuthentication();
			sourcePacket.ReadAuthentication();

			// Collect the arguments to the procedure
			uint programId = sourcePacket.GetUInt();
			uint version = sourcePacket.GetUInt();
			uint protocol = sourcePacket.GetUInt();

			// Put together an XDR reply packet
			NfsPacket packet = new NfsPacket(128);

			packet.SetUInt(sourcePacket.XID);
			packet.SetUInt((uint)RpcSignalType.Reply);
			packet.SetUInt((uint)RpcMessageResult.Accepted);

			packet.AddNullAuthentication();

			if (!NfsHandlerManager.IsProgramRegistered((int)programId)) {
				packet.SetUInt((uint)RpcProcedure.ProgramUnavailable);
			}
			else {
				// TODO: add version checking. we're only doing v2 right now.
				// version mismatch gets the ProgMismatch value.
				//			result.AddLong(RPCConsts.RPCProgMismatch);
				//			result.AddLong(versmin);
				//			result.AddLong(versmax);
				int port = NfsHandlerManager.GetPort((int)programId);

				if (port == 0) {
					packet.SetUInt((uint)RpcProcedure.ProgramMismatch);
				}
				else {
					packet.SetUInt((uint)RpcProcedure.Success);
					packet.SetUInt((uint)port);
				}
			}

			Send(packet, receivedFrom);
		}

		private void Set(NfsPacket sourcePacket, IPEndPoint receivedFrom) {
			// skip past the authentication records
			sourcePacket.ReadAuthentication();
			sourcePacket.ReadAuthentication();

			// Collect the arguments to the procedure
			uint programId = sourcePacket.GetUInt();
			uint version = sourcePacket.GetUInt();
			uint protocol = sourcePacket.GetUInt();
			uint port = sourcePacket.GetUInt();

			NfsPacket packet = new NfsPacket(128);
			
			packet.AddReplyHeader(sourcePacket.XID);
			packet.SetUInt(PORTMAP_TRUE);

			//	portmapMapping toadd = new portmapMapping(prog, vers, prot);
			//	toadd.SetPort(port);

			//	XDRPacket result = new XDRPacket(128);
			//	result.AddReplyHeader(xid);

			//	// look for the chain of versions for this program
			//	long? pl = new long?(prog);
			//	portmapMapping chain = (portmapMapping)mappings[pl];
			//	if (chain == null) {
			//		mappings.Add(pl, toadd);
			//		result.AddLong(Portmap.PM_TRUE);
			//	}
			//	else {
			//		// See if this version is already registered in the chain
			//		while (chain != null) {
			//			if (chain.Version() == vers && chain.Protocol() == prot) {
			//				result.AddLong(Portmap.PM_FALSE);
			//				break;
			//			}
			//			else if (chain.Next() == null) {
			//				chain.SetNext(toadd);
			//				result.AddLong(Portmap.PM_TRUE);
			//				break;
			//			}
			//			chain = chain.Next();
			//		}
			//	}

			Send(packet, receivedFrom);
		}

	}
}