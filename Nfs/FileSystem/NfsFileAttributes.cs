using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs.FileSystem {

	// The file attributes that NFS needs.
	//
	// From:
	// Sun Microsystems, Inc.                                         [Page 15]
	// RFC 1094                NFS: Network File System              March 1989
	//
	public class NfsFileAttributes {

		private FileType type;
		private FilePermissions mode;

		private uint nlink;
		private uint uid;
		private uint gid;
		private uint size;
		private uint blocksize;
		private uint rdev;
		private uint blocks;
		private uint fsid;
		private uint fileid;

		private NfsTime lastAccessed;
		private NfsTime lastModified;
		private NfsTime lastChanged;

		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct FILETIME {
			public uint DateTimeLow;
			public uint DateTimeHigh;
		}

		public struct BY_HANDLE_FILE_INFORMATION {
			public uint FileAttributes;
			public FILETIME CreationTime;
			public FILETIME LastAccessTime;
			public FILETIME LastWriteTime;
			public uint VolumeSerialNumber;
			public uint FileSizeHigh;
			public uint FileSizeLow;
			public uint NumberOfLinks;
			public uint FileIndexHigh;
			public uint FileIndexLow;
		}

		public NfsFileAttributes() { }

		public bool Read(NfsPacket packet) {
			type = (FileType)packet.GetUInt();
			mode = (FilePermissions)packet.GetUInt();
			nlink = packet.GetUInt();
			uid = packet.GetUInt();
			gid = packet.GetUInt();
			size = packet.GetUInt();
			blocksize = packet.GetUInt();
			rdev = packet.GetUInt();
			blocks = packet.GetUInt();
			fsid = packet.GetUInt();
			fileid = packet.GetUInt();

			if (!lastAccessed.Read(packet)) {
				return false;
			}
			if (!lastModified.Read(packet)) {
				return false;
			}
			if (!lastChanged.Read(packet)) {
				return false;
			}

			return true;
		}

		public bool Emit(ref NfsPacket packet) {
			packet.SetUInt((uint)type);
			packet.SetUInt((uint)mode);
			packet.SetUInt(nlink);
			packet.SetUInt(uid);
			packet.SetUInt(gid);
			packet.SetUInt(size);
			packet.SetUInt(blocksize);
			packet.SetUInt(rdev);
			packet.SetUInt(blocks);
			packet.SetUInt(fsid);
			packet.SetUInt(fileid);

			if (!lastAccessed.Emit(ref packet)) {
				return false;
			}
			if (!lastModified.Emit(ref packet)) {
				return false;
			}
			if (!lastChanged.Emit(ref packet)) {
				return false;
			}

			return true;
		}

		public uint Load(String path) {

			if (!File.Exists(path) && !Directory.Exists(path)) {
				throw new FileNotFoundException();
			}

			mode = 0;

			FileAttributes fileAttributes = File.GetAttributes(path);
			Boolean isDirectory = fileAttributes.Is(FileAttributes.Directory);
			Boolean canRead = NfsFileAttributes.CanRead(path);
			Boolean canWrite = NfsFileAttributes.CanWrite(path);

			if (!isDirectory) {
				type = FileType.NFREG;
				mode = FilePermissions.UPFILE;
			}
			else if (isDirectory) {
				type = FileType.NFDIR;
				mode = FilePermissions.UPDIR;
			}
			else {
				Console.Error.WriteLine("NfsFileAttributes.Load: " + path + " has unknown type");
				type = FileType.NFNON;
				mode = 0; // don't know what kind of file system object this is
			}

			//if (!isDirectory) {
			//	using (var fs = new FileStream(path, FileMode.Open)) {
			//		canRead = fs.CanRead;
			//		canWrite = fs.CanWrite;
			//	}
			//}
			//else {
			//	canRead = true;
			//	canWrite = !fileAttributes.Is(FileAttributes.ReadOnly);
			//}

			if (canRead) {
				mode |= FilePermissions.UP_OREAD | FilePermissions.UP_GREAD | FilePermissions.UP_WREAD;
				mode |= FilePermissions.UP_OEXEC | FilePermissions.UP_GEXEC | FilePermissions.UP_WEXEC;
			}
			if (canWrite) {
				mode |= FilePermissions.UP_OWRITE | FilePermissions.UP_GWRITE | FilePermissions.UP_WWRITE;
			}

			// from now on assume either file or directory
			if (!isDirectory) {
				nlink = 1;
			}
			else { // directories always have 2 links
				nlink = 2;
			}

			FileInfo file = new FileInfo(path);

			uid = 0;
			gid = 0;
			size = isDirectory ? 512 : (uint)(new FileInfo(path).Length);
			blocksize = 512; // XXX common value, how do I get this in java?
			rdev = 0;
			blocks = (size + blocksize - 1) / blocksize;
			fsid = isDirectory ? 0 : GetFileSystemId(file);
			fileid = (uint)HandleManager.GetHandle(path);

			lastAccessed = new NfsTime(file.LastAccessTime);
			lastChanged = new NfsTime(file.LastWriteTime);
			lastModified = new NfsTime(file.LastWriteTime);

			return (uint)NfsReply.OK;
		}

		public uint GetFileSystemId(FileInfo file) {
			BY_HANDLE_FILE_INFORMATION objectFileInfo = new BY_HANDLE_FILE_INFORMATION();

			try {
				using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					GetFileInformationByHandle(fs.SafeFileHandle.DangerousGetHandle(), out objectFileInfo);
				}
			}
			catch (Exception) {
				return 0;
			}

			uint fileIndex = (objectFileInfo.FileIndexHigh << 32) + objectFileInfo.FileIndexLow;

			return fileIndex;
		}

		public static bool IsDirectory(string path) {
			FileAttributes fileAttributes = File.GetAttributes(path);
			return fileAttributes.Is(FileAttributes.Directory);
		}

		private static Boolean Can(String path, FileIOPermissionAccess value) {
			FileIOPermission fp = new FileIOPermission(value, path);

			try {
				fp.Demand();
			}
			catch (System.Security.SecurityException) {
				return false;
			}

			return true;
		}

		public static Boolean CanRead(String path) {
			return Can(path, FileIOPermissionAccess.Read);
		}

		public static Boolean CanWrite(String path) {
			return Can(path, FileIOPermissionAccess.Write);
		}

	}

}
