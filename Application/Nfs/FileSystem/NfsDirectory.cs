using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Snarf.Nfs.FileSystem {

	internal class NfsDirectory {

		private FileSystemInfo _fsInfo;
		
		// keep these between calls so subsequent calls getting the rest of the contents of a directory are fast.
		private String _cachedDirectories;
		private List<String> _cachedFiles;

		public NfsDirectory(FileSystemInfo fsInfo) {
			_fsInfo = fsInfo;
		}

		public virtual NfsPacket GetAttr(NfsPacket packet) {
			try {
				FileHandle f = new FileHandle(packet);
				NfsFileAttributes fa = new NfsFileAttributes();

				string file = GetNameFromHandle(f.Handle, packet.XID);
				
				if (!File.Exists(file) && !Directory.Exists(file)) {
					throw new NFSException(packet.XID, (uint)NfsReply.ERR_NOENT);
				}

				Console.WriteLine("NfsDirectory.GetAttr: file: " + file);
				fa.Load(file);
								
				NfsPacket reply = new NfsPacket(96);
				reply.AddReplyHeader(packet.XID);
				reply.SetUInt((uint)NfsReply.OK);

				fa.Emit(ref reply);
				
				return reply;
			}
			catch (FileNotFoundException) {
				throw new NFSException(packet.XID, (uint)NfsReply.ERR_NOENT);
			}
		}

		public virtual NfsPacket SetAttr(uint xid, NfsPacket packet) {
			try {
				FileHandle f = new FileHandle(packet);
				String fileName = GetNameFromHandle(f.Handle, xid);

				// the attributes
				int mode = (int)packet.GetUInt();
				int uid = (int)packet.GetUInt();
				int gid = (int)packet.GetUInt();
				int size = (int)packet.GetUInt();
				NfsTime atime = new NfsTime(packet);
				NfsTime mtime = new NfsTime(packet);

				// the only attribute that can be set is the size can be set to 0 to truncate the file 
				if (size == 0) {
					// truncate by deleting and recreating the file
					using (var fs = new FileStream(fileName, FileMode.Truncate, FileAccess.ReadWrite)) { }
				}

				NfsPacket reply = new NfsPacket(128);
				reply.AddReplyHeader(xid);
				reply.SetUInt((uint)NfsReply.OK);
				
				NfsFileAttributes fa = new NfsFileAttributes();
				fa.Load(fileName);
				fa.Emit(ref reply);
				
				return reply;
			}
			catch (FileNotFoundException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOENT);
			}
			catch (IOException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_PERM);
			}
		}

		public virtual NfsPacket Lookup(NfsPacket packet) {
			try {
				FileHandle dir = new FileHandle(packet);
				String entry = packet.GetString();
				String dirName = GetNameFromHandle(dir.Handle, packet.XID);
				String fileName = Path.Combine(dirName, entry);

				if (File.Exists(fileName) != true) {
					throw new NFSException(packet.XID, (uint)NfsReply.ERR_NOENT);
				}

				NfsFileAttributes attributes = new NfsFileAttributes();
				attributes.Load(fileName);

				// make a FileHandle for this new path
				uint handleId = HandleManager.Current.GetHandle(fileName);
				
				FileHandle handle = new FileHandle();
				handle.Set(dir.Root, handleId, dir.ReadOnly);

				// make the reply
				NfsPacket reply = new NfsPacket(128);
				reply.AddReplyHeader(packet.XID);
				reply.SetUInt((uint)NfsReply.OK);

				handle.Emit(ref reply);
				attributes.Emit(ref reply);

				return reply;

			}
			catch (FileNotFoundException) {
				throw new NFSException(packet.XID, (uint)NfsReply.ERR_NOENT);
			}
		}

		public virtual NfsPacket ReadDirectory(NfsPacket packet) {
			
			FileHandle fh = new FileHandle(packet);
			uint cookie = packet.GetUInt();
			uint count = packet.GetUInt();
			uint xId = packet.XID;

			// if this is a new call to readdir (cookie=0) or it is a new directory to read, replace the cache.
			string dirName = GetNameFromHandle(fh.Handle, xId);
			
			//Console.Write("Reading dir " + dirName + " cookie=" + cookie + " count=" + count + "\n");

			if (cookie == 0 || (dirName.Equals(_cachedDirectories) == false)) {

				if (!Directory.Exists(dirName)) {
					throw new NFSException(xId, (uint)NfsReply.ERR_NOENT);
				}

				List<String> dirFiles = Directory.GetFiles(dirName).Select(f => new FileInfo(f).Name).ToList();
				dirFiles.AddRange(Directory.GetDirectories(dirName).Select(d => new DirectoryInfo(d).Name));

				if (dirFiles == null) {
					throw new NFSException(xId, (uint)NfsReply.ERR_NOENT);
				}

				//Console.WriteLine("dir has " + dirFiles.Count + " entries");

				if (dirFiles.Count <= 0) {
					throw new NFSException(xId, (uint)NfsReply.ERR_NOENT);
				}

				dirFiles.BubbleSort();
				dirFiles.Insert(0, "..");
				dirFiles.Insert(0, ".");
				
				_cachedFiles = dirFiles;
				_cachedDirectories = dirName;
			}

			// prepare the reply packet.
			NfsPacket reply = new NfsPacket((int)count);
			reply.AddReplyHeader(xId);
			reply.SetUInt((uint)NfsReply.OK);

			// Add files to the list until there are no more files or all of the count bytes have been used.
			
			int current = reply.Length;
			bool more = false; // are there more files to get
			
			// if there are any files to add
			if (_cachedFiles != null && _cachedFiles.Count > 0) {
				for (int i = (int)cookie; i < _cachedFiles.Count; i++) {
					// see if there is enough room for another file - 3 longs of id,
					//   the name (rounded up to 4 bytes) and a trailing long 
					//   indicating whether there are more files to get
					int needed = 3 * 4 + (_cachedFiles[i].Length + 3) + 8;
					if (needed + current >= count) {
						more = true;
						break;
					}
					// get the handle for this file
					string fileName = Path.Combine(_cachedDirectories, _cachedFiles[i]);
					uint handle = HandleManager.Current.GetHandle(fileName);

					// add an entry to the packet for this file
					reply.SetUInt(NfsHandler.NFS_TRUE);
					reply.SetUInt(handle);
					reply.Set(_cachedFiles[i]);
					reply.SetUInt((uint)i + 1); // this is the cookie
					
					current = reply.Length;
				}
			}
			reply.SetUInt(NfsHandler.NFS_FALSE); // no more entries in this packet

			// tell the client if this packet has returned the last of the files
			if (more) {
				reply.SetUInt(NfsHandler.NFS_FALSE);
			}
			else {
				reply.SetUInt(NfsHandler.NFS_TRUE);
			}

			return reply;
		}

		public virtual NfsPacket Create(uint xid, NfsPacket packet) {
			try {
				FileHandle dirFH = new FileHandle(packet);
				string entry = packet.GetString();
				string dirName = GetNameFromHandle(dirFH.Handle, xid);
				string path = Path.Combine(dirName, entry);

				// make the file

				if (File.Exists(path)) {
					throw new NFSException(xid, (uint)NfsReply.ERR_EXIST);
				}

				using (var file = File.Create(path)) { }

				// make a new handle for this file
				FileHandle fh = new FileHandle();
				long handle = HandleManager.Current.GetHandle(path);
				fh.Set(dirFH.Root, (uint)handle, dirFH.ReadOnly);

				// get the attributes of this new file
				NfsFileAttributes fa = new NfsFileAttributes();
				fa.Load(path);

				// create the reply packet
				NfsPacket reply = new NfsPacket(128);
				reply.AddReplyHeader(xid);
				reply.SetUInt((uint)NfsReply.OK);
				fh.Emit(ref reply);
				fa.Emit(ref reply);
				return reply;

			}
			catch (FileNotFoundException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}
			catch (IOException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}
			catch (System.Security.SecurityException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_PERM);
			}
		}

		public virtual NfsPacket Remove(uint xid, NfsPacket packet) {
			FileHandle fileHandle = new FileHandle(packet);
			string entry = packet.GetString();

			// open and delete the file
			string dirName = GetNameFromHandle(fileHandle.Handle, xid);
			var fd = new FileInfo(Path.Combine(dirName, entry));
			if (fd.Exists == false) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOENT);
			}
			try {
				fd.Delete();
			}
			catch (Exception) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}

			// create the reply packet
			NfsPacket reply = new NfsPacket(128);
			reply.AddReplyHeader(xid);
			reply.SetUInt((uint)NfsReply.OK);
			return reply;
		}

		public virtual NfsPacket Mkdir(uint xid, NfsPacket packet) {
			try {
				FileHandle fileHandle = new FileHandle(packet);
				string entry = packet.GetString();

				string dirName = GetNameFromHandle(fileHandle.Handle, xid);
				string newdir = Path.Combine(dirName, entry);

				var dir = new DirectoryInfo(newdir);

				if (dir.Exists) {
					throw new NFSException(xid, (uint)NfsReply.ERR_EXIST);
				}

				dir.Create();

				// make a FileHandle for this directory
				long handle = HandleManager.Current.GetHandle(newdir);
				FileHandle newFH = new FileHandle();
				newFH.Set(fileHandle.Root, (uint)handle, fileHandle.ReadOnly);

				// get the attributes
				NfsFileAttributes fa = new NfsFileAttributes();
				fa.Load(newdir);

				NfsPacket reply = new NfsPacket(128);
				reply.AddReplyHeader(xid);
				reply.SetUInt((uint)NfsReply.OK);
				newFH.Emit(ref reply);
				fa.Emit(ref reply);
				return reply;

			}
			catch (FileNotFoundException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}
		}

		public virtual NfsPacket Rmdir(uint xid, NfsPacket packet) {
			FileHandle fileHandle = new FileHandle(packet);
			string name = packet.GetString();

			string dirname = GetNameFromHandle(fileHandle.Handle, xid);
			var fd = new FileInfo(Path.Combine(dirname, name));
			// do some correctness checking
			if (fd.Exists == false) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOENT);
			}
			if (NfsFileAttributes.IsDirectory(fd.FullName) == false) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOTDIR);
			}
			// try to remove the directory
			try {
				fd.Delete();
			}
			catch (Exception) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}

			NfsPacket reply = new NfsPacket(128);
			reply.AddReplyHeader(xid);
			reply.SetUInt((uint)NfsReply.OK);
			return reply;
		}

		public virtual NfsPacket StatFS(NfsPacket packet) {
			FileHandle fh = new FileHandle(packet);
			// tell the fsinfo the path to get information about
			_fsInfo.SetFS(GetNameFromHandle(fh.Handle, packet.XID));

			NfsPacket reply = new NfsPacket(128);
			reply.AddReplyHeader(packet.XID);
			reply.SetUInt((uint)NfsReply.OK);
			reply.SetUInt(_fsInfo.TransferSize);
			reply.SetUInt(_fsInfo.BlockSize);
			reply.SetUInt(_fsInfo.TotalBlocks);
			reply.SetUInt(_fsInfo.FreeBlocks);
			reply.SetUInt(_fsInfo.AvailableBlocks);
			return reply;
		}

		public virtual NfsPacket Rename(uint xid, NfsPacket packet) {
			// collect arguments from RPC packet
			FileHandle sourceHandle = new FileHandle(packet);
			string srcentry = packet.GetString();

			FileHandle destHandle = new FileHandle(packet);
			string destentry = packet.GetString();

			// compute the path names specified
			String srcdir = GetNameFromHandle(sourceHandle.Handle, xid);
			String destdir = GetNameFromHandle(destHandle.Handle, xid);

			FileInfo source = new FileInfo(Path.Combine(srcdir, srcentry));
			FileInfo dest = new FileInfo(Path.Combine(destdir, destentry));

			if (source.Exists == false) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOENT);
			}

			if (dest.Exists) {
				throw new NFSException(xid, (uint)NfsReply.ERR_EXIST);
			}

			try {
				source.MoveTo(dest.FullName);
			}
			catch (Exception) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}

			NfsPacket reply = new NfsPacket(128);
			reply.AddReplyHeader(xid);
			reply.SetUInt((uint)NfsReply.OK);
			return reply;
		}

		// local procedure to get the associated with a handle, throws an exception if there is a problem.
		private string GetNameFromHandle(uint handle, uint xid) {
			String result = HandleManager.Current.GetName(handle);
			if (result == null) {
				throw new NFSException(xid, (int)NfsReply.ERR_STALE);
			}
			return result;
		}
	}
}