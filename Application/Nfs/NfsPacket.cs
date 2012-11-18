using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {

	public class NfsPacket : UdpPacket {

		public NfsPacket(DatagramPacket datagram) : base(datagram) { }

		public NfsPacket(int size) : base(size) { }

		public uint XID { get; set; }
		public uint RpcVersion { get; set; }
		public uint ProgramID { get; set; }
		public uint NfsVersion { get; set; }
		public uint ProcedureID { get; set; }

		public String RemoteHost { get; set; }

		// struct auth_unix - RFC 1057- page 12
		private uint AuthStamp { get; set; }
		public String AuthMachineName { get; set; }
		public uint AuthClientID { get; set; }
		
		public virtual void AddReplyHeader(uint xid) {
			SetUInt(xid);
			SetUInt((uint)RpcSignalType.Reply);
			SetUInt((uint)RpcMessageResult.Accepted);
			AddNullAuthentication();
			SetUInt((uint)RpcProcedure.Success);
		}

		public virtual void AddNullAuthentication() {
			SetUInt(0); // the type
			SetUInt(0); // the length
		}

		public virtual void ReadAuthentication() {
			uint type = GetUInt();
			uint length = GetUInt();
			int startPos = this.Position; // know where we started before we got the info we need, so we can skip ahead.

			if (type == (uint)RpcAuthFlavor.UNIX) {
				AuthStamp = GetUInt();
				AuthMachineName = GetString();
				AuthClientID = GetUInt();

				// skip the gids portion
				int advance = (int)length - (this.Position - startPos);
				if (advance > 0) {
					Advance((uint)advance);
				}		
			}
			else {
				Advance(length);
			}

			//struct auth_unix {
			//	unsigned int stamp;
			//	string machinename<255>;
			//	unsigned int uid;
			//	unsigned int gid; 
			//	unsigned int gids<16>;
			//}
		}

	}
}
