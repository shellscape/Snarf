using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {

	public enum RpcSignalType : int {
		Call = 0,
		Reply = 1
	}

	public enum RpcMessageResult : int {
		Accepted = 0,
		Denied = 0
	}

	public enum RpcProcedure : int {
		Success = 0,
		ProgramUnavailable = 1,
		ProgramMismatch = 2,
		ProcedureUnavail = 3,
		GarbageArguments = 4
	}

	public enum RpcAuthFlavor : int { 
		NULL = 0,
		UNIX = 1, 
		SHORT = 2, 
		DES = 3 
	};


	public enum NfsProcedure : int {
		NULL = 0,
		GETATTR = 1,
		SETATTR = 2,
		ROOT = 3,
		LOOKUP = 4,
		READLINK = 5,
		READ = 6,
		WRITECACHE = 7,
		WRITE = 8,
		CREATE = 9,
		REMOVE = 10,
		RENAME = 11,
		LINK = 12,
		SYMLINK = 13,
		MKDIR = 14,
		RMDIR = 15,
		READDIR = 16,
		STATFS = 17
	}

	public enum NfsReply : int {
		OK = 0,
		ERR_PERM = 1,
		ERR_NOENT = 2,
		ERR_IO = 5,
		ERR_NXIO = 6,
		ERR_ACCES = 13,
		ERR_EXIST = 17,
		ERR_NODEV = 19,
		ERR_NOTDIR = 20,
		ERR_ISDIR = 21,
		ERR_FBIG = 27,
		ERR_NOSPC = 28,
		ERR_ROFS = 30,
		ERR_NAMETOOLONG = 63,
		ERR_NOTEMPTY = 66,
		ERR_DQUOT = 69,
		ERR_STALE = 70,
		ERR_WFLUSH = 99
	}

	public enum RpcPrototcol : int {
		TCP = 6,
		UDP = 17
	}

}
