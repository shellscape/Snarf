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
			Advance(length);
		}

	}
}
