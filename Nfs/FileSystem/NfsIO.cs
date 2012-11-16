using System;
using System.IO;

namespace Snarf.Nfs.FileSystem {

	public class NfsIO {

		internal NfsIO() { }

		internal virtual NfsPacket Write(uint xid, NfsPacket packet) {
			string fileName = null;

			try {
				// read info out of packet
				FileHandle fh = new FileHandle(packet);
				uint beginoffset = packet.GetUInt();
				uint offset = packet.GetUInt();
				uint totalcount = packet.GetUInt();
				// next comes the data which is a uint of size and the bytes.
				uint datalen = packet.GetUInt();
				uint packetOffset = (uint)packet.Position;
				packet.Advance(datalen);

				// carry out the write operation
				fileName = HandleManager.GetName(fh.Handle);
				if (fileName == null) {
					throw new NFSException(xid, (uint)NfsReply.ERR_STALE);
				}
				// XXX comment out print lines to improve performance
				// System.out.print("Write(" + fileName + ", " + offset + ", " + 
				//		     datalen + ")\n");
				using (StreamWriter sw = new StreamWriter(fileName)) {
					sw.BaseStream.Seek(offset, SeekOrigin.Begin);
					sw.BaseStream.Write(packet.Data, (int)packetOffset, (int)datalen);
				}

				// load in new file attributes
				NfsFileAttributes fa = new NfsFileAttributes();
				fa.Load(fileName);

				// create the reply packet
				NfsPacket result = new NfsPacket(128);
				result.Reset();
				result.AddReplyHeader(xid);
				result.SetUInt((uint)NfsReply.OK);
				fa.Emit(ref result);

				return result;
			}
			catch (IOException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}
			catch (System.Security.SecurityException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_PERM);
			}
		}

		//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
		//ORIGINAL LINE: NfsPacket Read(uint xid, NfsPacket packet, NfsPacketCache packetCache) throws NFSException
		internal virtual NfsPacket Read(uint xid, NfsPacket packet) {
			try {
				// collect data out of the packet
				FileHandle fh = new FileHandle(packet);
				uint offset = packet.GetUInt();
				uint count = packet.GetUInt();
				uint totalCount = packet.GetUInt(); // not used

				// do the operation
				string fileName = HandleManager.GetName(fh.Handle);
				if (fileName == null) {
					throw new NFSException(xid, (uint)NfsReply.ERR_STALE);
				}

				if (count <= 0) {
					Console.Error.WriteLine("\tRead: invalid value for count " + count);
					throw new NFSException(xid, (uint)NfsReply.ERR_IO);
				}

				int numberRead;
				byte[] readbuf;

				using (StreamReader sr = new StreamReader(fileName)) {
					sr.BaseStream.Seek(offset, SeekOrigin.Begin);
					readbuf = new byte[(int)count];
					numberRead = sr.BaseStream.Read(readbuf, 0, (int)count);
				}

				// Make sure something was read in */
				if (numberRead < 0) {
					Console.Error.WriteLine("\tRead error: number read is " + numberRead);
					numberRead = 0;
				}

				// load in file attributes.
				NfsFileAttributes fa = new NfsFileAttributes();
				fa.Load(fileName);

				// put together the reply packet, this will copy the read
				//   data which is a little inefficient
				NfsPacket reply = new NfsPacket(128 + numberRead);
				reply.AddReplyHeader(xid);
				reply.SetUInt((uint)NfsReply.OK);
				fa.Emit(ref reply);
				reply.SetData(numberRead, readbuf);

				return reply;

			}
			catch (FileNotFoundException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_NOENT);
			}
			catch (IOException) {
				throw new NFSException(xid, (uint)NfsReply.ERR_IO);
			}
		}

		internal virtual NfsPacket getNfsPacket(int desiredSize) {
			return new NfsPacket(desiredSize);
		}
	}

}