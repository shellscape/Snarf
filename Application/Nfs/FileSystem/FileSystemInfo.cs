// this class is used to encapsulate all of the file system information.  The
//   idea is that it be replaced by native code to get better NFS behavior.
namespace Snarf.Nfs.FileSystem {

	/// <summary>
	/// Used to encapsulate all of the file system information. The idea is that it be replaced by native code to get better NFS behavior.
	/// </summary>
	public class FileSystemInfo {

		internal FileSystemInfo() {
		}

		internal virtual void SetFS(string path) {
		}

		internal virtual uint TransferSize { get { return 8192; } }
		internal virtual uint BlockSize { get { return 512; } }
		internal virtual uint TotalBlocks { get { return 0; } }
		internal virtual uint FreeBlocks { get { return 0; } }
		internal virtual uint AvailableBlocks { get { return 0; } }
	}
}