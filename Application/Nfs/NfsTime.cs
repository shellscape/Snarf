using System;

namespace Snarf.Nfs {
	public class NfsTime {

		private uint _seconds;
		private uint _milliseconds;
		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public NfsTime(NfsPacket packet) {
			Read(packet);
		}

		public NfsTime(uint seconds, uint milliseconds) {
			Set(seconds, milliseconds);
		}

		public NfsTime(DateTime dateTime) {
			Set(GetSeconds(dateTime), GetMilliseconds(dateTime));
		}

		public NfsTime(uint time) {
			// convert a 64 bit uint into two 32 bit uints to pass to the regular set function
			uint top = time >> 16;
			uint bottom = time & 0xffff;

			bottom += (top & 0xffff) << 16;
			top = top >> 16;

			Set(top, bottom);
		}

		internal virtual bool Read(NfsPacket packet) {
			_seconds = packet.GetUInt();
			_milliseconds = packet.GetUInt();
			return true;
		}

		internal virtual bool Emit(ref NfsPacket packet) {
			packet.SetUInt(_seconds);
			packet.SetUInt(_milliseconds);
			return true;
		}

		internal virtual bool Set(uint seconds, uint milliseconds) {
			_seconds = seconds;
			_milliseconds = milliseconds;
			return true;
		}

		internal virtual void Print() {
			Console.Write("Timeval: sec=" + _seconds + " msec=" + _milliseconds + "\n");
		}

		//public static uint GetSeconds(uint localTime) {
		//	uint startTime = 27111902 << 32 + 54590 << 16 + 32768;
		//	uint delta = localTime - startTime;
		//	delta /= 1000;
		//	return delta;
		//}

		//public static uint GetMilliSeconds(uint localTime) {
		//	uint startTime = 27111902 << 32 + 54590 << 16 + 32768;
		//	uint delta = localTime - startTime;
		//	delta %= 1000;
		//	return delta;
		//}

		//public static uint GetSeconds(uint localTime) {
		//	return localTime / (1 << 32);
		//}

		//public static uint GetMilliSeconds(uint localTime) {
		//	return localTime % (1 << 32);
		//}


		/// <summary>
		/// Returns the UNIX Timestamp value
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static uint GetMilliseconds(DateTime dateTime) {
			return (uint)(dateTime.ToUniversalTime() - UnixEpoch).Milliseconds;
		}

		/// <summary>
		/// Returns the UNIX Timestamp value
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static uint GetSeconds(DateTime dateTime) {
			return (uint)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;
		}
	}
}