using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs.FileSystem {

	public enum FileType : int {
		NFNON = 0,
		NFREG = 1,
		NFDIR = 2,
		NFBLK = 3,
		NFCHR = 4,
		NFLNK = 5
	}

	public enum FilePermissions : uint {
		UPDIR = 0040000, // This is a directory
		UPCHRS = 0020000, // This is a character special file
		UPBLKS = 0060000, // This is a block special file
		UPFILE = 0100000, // This is a regular file
		UPSLINK = 0120000, // This is a symbolic link file
		UPSOCK = 0140000, // This is a named socket
		UPSUID = 0004000, // Set user id on execution.
		UPSGID = 0002000, // Set group id on execution.
		UPSTICKY = 0001000, // Save swapped text even after use.
		UP_OREAD = 0000400, // Read permission for owner.
		UP_OWRITE = 0000200, // Write permission for owner.
		UP_OEXEC = 0000100, // Execute and search permission owner.
		UP_GREAD = 0000040, // Read permission for group.
		UP_GWRITE = 0000020, // Write permission for group.
		UP_GEXEC = 0000010, // Execute and search permission group.
		UP_WREAD = 0000004, // Read permission for world.
		UP_WWRITE = 0000002, // Write permission for world.
		UP_WEXEC = 0000001 // Execute and search permission world.
	}
	
}
