﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Snarf.Udp {
	public class DatagramPacket {
		private byte[] _bytes;
		private int _length;
		private IPEndPoint _endPoint;

		public DatagramPacket(byte[] bytes, int length, IPEndPoint endPoint) : this(bytes, length) {
			_endPoint = endPoint;
		}

		public DatagramPacket(byte[] bytes, int length) {
			_length = length;
			_bytes = new byte[_length];
			Buffer.BlockCopy(bytes, 0, _bytes, 0, _length);
		}

		public byte[] Bytes {
			get { return _bytes; }
			set {
				_bytes = value;
				_length = value.Length;
			}
		}

		public int Length {
			get { return _length; }
		}

		public IPEndPoint ServerEndPoint {
			get { return _endPoint; }
			set { _endPoint = value; }
		}
	}
}