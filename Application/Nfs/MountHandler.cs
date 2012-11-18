using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {

	public class MountHandler : BaseHandler {

		public const int MOUNT_PORT = 2049; // mountd doesn't have assigned port
		public const int MOUNT_VERSION = 1;
		public const int MOUNT_PROGRAMID = 100005;

		public enum MountProcedure : int {
			NULL = 0,
			MNT = 1,
			UMNT = 3
		}

		public MountHandler() : base(MOUNT_PORT, MOUNT_PROGRAMID) { // system will set the port

		}

		protected override void Process(NfsPacket packet, IPEndPoint receivedFrom) {
			Console.WriteLine("MountHandler.Process : recievedFrom: " + receivedFrom.ToString());
			Console.WriteLine("MountHandler.Process: Procedure -> " + packet.ProcedureID + ":" + ((MountProcedure)packet.ProcedureID).ToString());

			switch (packet.ProcedureID) {
				case (int)MountProcedure.NULL:
					base.SendNull(packet, receivedFrom);
					break;
				case (int)MountProcedure.MNT:
					Mount(packet, receivedFrom);
					break;
				case (int)MountProcedure.UMNT:
					Unmount(packet, receivedFrom);
					break;
				default:
					break;

			}
		}
		
		private void Mount(NfsPacket sourcePacket, IPEndPoint receivedFrom) {

			NfsReply replyCode = NfsReply.OK;
			NfsPacket packet = new NfsPacket(128);

			packet.AddReplyHeader(sourcePacket.XID);

			// skip past the authentication records
			sourcePacket.ReadAuthentication();
			sourcePacket.ReadAuthentication();

			// next should be a dirpath, which is a string.  Replace unix style path with local style path
			String path = sourcePacket.GetString();
			
			if (path == null) {
				replyCode = NfsReply.ERR_STALE;
			}
			else {
				String original = path.Clone() as String;

				path = NfsPath.ToWin(path);

				Console.WriteLine("MountHandler.Mount : requested: " + original + ", actual: " + path);

				//if (!Directory.Exists(path)) {
				//	replyCode = NfsReply.ERR_EXIST;
				//}
			}

			// Try to validate this mount, if there is an error make an error packet, otherwise send back the handle.

			if (replyCode != NfsReply.OK) {
				packet.SetUInt((uint)replyCode);
				
			}
			else if (false){ //exports.Matches(packet.Source(), path) == false) {
				// No permission for this mount in the exports file
				//result.AddLong(NFS.NFSERR_PERM);
				//Console.Error.WriteLine("!!! Mount request for " + path + "from " + packet.Source() + " denied.\n");
			}
			else {
				// put together a file handle
				uint handle = HandleManager.Current.GetHandle(path);
				var fileHandle = new FileSystem.FileHandle();

				fileHandle.Set(handle, (uint)handle, 0);

				packet.SetUInt((uint)replyCode);

				fileHandle.Emit(ref packet);
			}

			if (replyCode == NfsReply.OK) {
				MountManager.Current.Add(sourcePacket.RemoteHost, path);
			}
			
			Send(packet, receivedFrom);
		}
		
		private void Unmount(NfsPacket sourcePacket, IPEndPoint receivedFrom) {

			// skip past the authentication records
			sourcePacket.ReadAuthentication();
			sourcePacket.ReadAuthentication();

			String path = sourcePacket.GetString();
			NfsPacket packet = new NfsPacket(128);

			HandleManager.Current.GetHandle(path);
			
			packet.AddReplyHeader(sourcePacket.XID);
			packet.SetUInt((uint)NfsReply.OK);

			Console.WriteLine("MountHandler.Unmount : requested: " + path);

			MountManager.Current.Remove(sourcePacket.RemoteHost);

			Send(packet, receivedFrom);
		}


		
	}
}
