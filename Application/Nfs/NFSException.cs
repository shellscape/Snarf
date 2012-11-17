using System;

namespace Snarf.Nfs {
	public class NFSException : Exception {

		internal NFSException(uint xId, uint errorNumber) {
			XID = xId;
			ErrorNumber = errorNumber;
		}

		public uint XID { get; private set; }
		public uint ErrorNumber { get; private set; }
	}

}