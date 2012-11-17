using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
// The FileHandle is used by NFS to represent files. It is a 32 byte piece of data that the NFS client gets from mountd or the NFS
//   server.
//
// This implementation stores 3 things in the FileHandle: the handle of the root of this file system (the handle of the mount point),
//   the handle of the file, and a flag indicating whether this handle is read only. The rest of the 32 bytes are not used, but since
//   NFS clients think two files are the same iff the FileHandles are the same, make sure this always gives out the same FileHandle by
//   setting the rest of the data to 0.


namespace Snarf.Nfs.FileSystem {
	public class FileHandle {

		internal uint root; // handle of the root of this mount point
		internal uint handle; // handle of the file
		internal uint @readonly; // is the mount read only?

		public FileHandle() {
		}

		// Initialize this FileHandle from the packet, leave the position in the packet just past the FileHandle.
		public FileHandle(NfsPacket source) {
			Read(source);
		}

		public virtual uint Root { get { return root; } }

		public virtual uint Handle { get { return handle; } }

		public virtual uint ReadOnly { get { return @readonly; } }

		internal virtual bool Read(NfsPacket packet) {

			root = packet.GetUInt(); // the first long in the packet is the handle of the root
			handle = packet.GetUInt(); // the next long is the handle of the file
			@readonly = packet.GetUInt(); // the next is a read only flag

			// The rest is nothing. There are 32 bytes in a FileHandle and this has read in 3 words, or 12 bytes.
			packet.Advance(32 - 3 * 4);

			return true;
		}

		public Boolean Set(uint root, uint handle, uint readOnly) {
			this.root = root;
			this.handle = handle;
			this.@readonly = readOnly;

			return true;
		}

		public Boolean Emit(ref NfsPacket packet) {
			packet.SetUInt(root);
			packet.SetUInt(handle);
			packet.SetUInt(@readonly);
			
			// The rest of the words of the handle should be 0.  Since there are 32 bytes in a handle, 
			// there are 8 words and the above consumed 3 of them, so there are 5 left.
			for (int i = 0; i < 5; i++) {
				packet.SetUInt(0);
			}

			return true;
		}

		internal virtual void Print() {
			Console.WriteLine("FileHandle: root: " + root + ", handle: " + handle + ", readonly: " + @readonly);
		}
	}
}
