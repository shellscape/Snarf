using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {
	public class NfsHandler : BaseHandler {

		public const int NFS_PORT = 2049;
		public const int NFS_VERSION = 2;
		public const int NFS_PROGRAMID = 100003;

		public const int NFS_TRUE = 1;
		public const int NFS_FALSE = 0;

		private FileSystem.NfsDirectory _directory = null;
		private FileSystem.NfsIO _io = null;

		public NfsHandler() : base(NfsHandler.NFS_PORT, NfsHandler.NFS_PROGRAMID) {
			// read directory support
			_directory = new FileSystem.NfsDirectory(new FileSystem.FileSystemInfo());

			// read and write
			_io = new FileSystem.NfsIO();		

		}

		protected override void Process(NfsPacket packet, IPEndPoint receivedFrom) {
			Console.WriteLine("NfsHandler.Process : recievedFrom: " + receivedFrom.ToString());
			Console.WriteLine("NfsHandler.Process: Procedure -> " + packet.ProcedureID + ":" + ((NfsProcedure)packet.ProcedureID).ToString());

			// get rid of authentication recorde in packet, we don't use them
			packet.ReadAuthentication();
			packet.ReadAuthentication();

			if (packet.ProcedureID == (int)NfsProcedure.NULL) {
				base.SendNull(packet, receivedFrom);
				return;
			}

			NfsPacket result;
			uint xid = packet.XID;

			try {
				switch (packet.ProcedureID) {
					case (int)NfsProcedure.GETATTR:
						result = _directory.GetAttr(packet);
						break;
					case (int)NfsProcedure.SETATTR:
						result = _directory.SetAttr(xid, packet);
						break;
					case (int)NfsProcedure.LOOKUP:
						result = _directory.Lookup(packet);
						break;
					case (int)NfsProcedure.READ:
						result = _io.Read(packet);
						break;
					case (int)NfsProcedure.WRITE:
						result = _io.Write(xid, packet);
						break;
					case (int)NfsProcedure.CREATE:
						result = _directory.Create(xid, packet);
						break;
					case (int)NfsProcedure.REMOVE:
						result = _directory.Remove(xid, packet);
						break;
					case (int)NfsProcedure.RENAME:
						result = _directory.Rename(xid, packet);
						break;
					case (int)NfsProcedure.MKDIR:
						result = _directory.Mkdir(xid, packet);
						break;
					case (int)NfsProcedure.RMDIR:
						result = _directory.Rmdir(xid, packet);
						break;
					case (int)NfsProcedure.READDIR:
						result = _directory.ReadDirectory(packet);
						break;
					case (int)NfsProcedure.STATFS:
						result = _directory.StatFS(packet);
						break;
					default:
						Console.Error.WriteLine("Unsupported NFS procedure called (" + packet.ProcedureID + ") from " + receivedFrom.ToString() + "\n");
						throw new NFSException(packet.XID, (uint)NfsReply.ERR_IO);
				}
			}
			catch (NFSException e) {
				// make a reply packet that includes the error
				result = new NfsPacket(64);
				result.AddReplyHeader(packet.XID);
				result.SetUInt(e.ErrorNumber);
			}

			Send(result, receivedFrom);
		}
	}
}
