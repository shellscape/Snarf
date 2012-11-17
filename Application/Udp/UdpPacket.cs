using System;
using System.Net;
using System.Net.Sockets;

namespace Snarf.Udp {

	public class UdpPacket {

		private byte[] _data;
		private int _length;
		private int _position;

		// store information about where the packet came from
		private IPAddress _source;
		private int _port;

		public UdpPacket(DatagramPacket datagram) {
			_data = datagram.Bytes;
			_length = datagram.Length;
			_position = 0;
			_source = datagram.ServerEndPoint == null ? null : datagram.ServerEndPoint.Address;
			_port = datagram.ServerEndPoint == null ? 0 : datagram.ServerEndPoint.Port;

			//Console.Write("source: " + _source);
			//Console.Write(" port: " + _port);
			//Console.Write(" data length: " + _length + "\n");
		}

		public UdpPacket(int size) {
			_length = size;
			_data = new byte[_length];
			_position = 0;
		}

		public byte[] Data { get { return _data; } }

		/// <summary>
		/// Returns the length of the data written to the buffer. This is sometimes different than Data.Length.
		/// </summary>
		public int Length { get { return _position; } }
		public int Position { get { return _position; } }
		public IPAddress Source { get { return _source; } }
		public int Port { get { return _port; } }

		#region .    Get Methods    .

		public uint GetUInt() {

			uint one = _data[_position];
			one <<= 24;
			uint two = _data[_position + 1];
			two <<= 16;
			uint three = _data[_position + 2];
			three <<= 8;
			uint four = _data[_position + 3];

			uint result = one | two | three | four;

			_position += 4;

			return result;
		}

		public long GetLong() {

			long one = _data[_position];
			one <<= 24;
			long two = _data[_position + 1];
			two <<= 16;
			long three = _data[_position + 2];
			three <<= 8;
			long four = _data[_position + 3];

			long result = one | two | three | four;

			_position += 4;

			return result;
		}

		public char GetChar() {
			char value = (char)_data[_position];

			++_position;

			return value;
		}

		public String GetString() {
			uint length = GetUInt();

			String value = "";

			for (int charCount = 0; charCount < length; ++charCount) {
				value += GetChar();
			}

			if (length % 4 != 0) {
				_position += (int)(4 - length % 4);
			}

			return value;
		}

		public virtual long GetData(byte[] buffer) {
			uint plen = GetUInt(); // how much data is in the packet
			if (plen + _position >= _data.Length) {
				Console.Error.WriteLine("GetData: packet is too small\n");
				return -1;
			}

			// try to copy the data into the buffer
			Array.Copy(_data, _position, buffer, 0, (int)plen);

			Advance(plen);
			return plen;
		}

		#endregion

		#region .    Set Methods    .

		public void Set(int value) {
			SetUInt((uint)value);
		}

		public void SetUInt(uint value) {
			Need(4);

			uint one = value;
			uint two = value;
			two >>= 8;
			uint three = value;
			three >>= 16;
			uint four = value;
			four >>= 24;

			_data[_position] = (byte)four;
			_data[_position + 1] = (byte)three;
			_data[_position + 2] = (byte)two;
			_data[_position + 3] = (byte)one;

			_position += 4;
		}

		public void Set(String value) {
			
			Need(4 + value.Length + 3); // Extra in case of pad

			SetUInt((uint)value.Length);

			foreach (Char c in value) {
				_data[_position] = (byte)c;
				++_position;
			}

			if (value.Length % 4 != 0) {
				for (int pad = 4 - value.Length % 4; pad != 0; --pad) {
					_data[_position] = 0;
					++_position;
				}
			}
		}

		private void Need(int need) {
			while (_position + need >= _data.Length) {
				Byte[] newData = new Byte[_data.Length * 2];
				_data.CopyTo(newData, 0);

				_data = newData;
			}
		}

		public virtual long SetData(int len, byte[] data) {
			SetUInt((uint)len);
			Array.Copy(data, 0, _data, _position, len);

			Advance(len);
			return 0;
		}

		public virtual long SetData(byte[] toadd) {
			return SetData(toadd.Length, toadd);
		}

		#endregion
		
		// Add the standard procedure you requested was called reply header
		public virtual void Advance(long length) {
			long words = (length + 3) / 4;
			long delta = 4 * words;
			_position += (int)delta;
		}

		public virtual void Reset() {
			_position = 0;
		}

	}
}