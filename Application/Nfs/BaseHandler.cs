using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Snarf.Udp;

namespace Snarf.Nfs {
	public class BaseHandler : UdpHandler {

		public BaseHandler(int port, int programId) : base() {

			this.ProgramID = programId;

			NfsHandlerManager.RegisterHandler(this);

			_listener = NfsListenerManager.GetListener(port, programId);
			_listener.PacketReceived += OnPacketReceived;
		}

		public int ProgramID { get; private set; }

		protected virtual void Process(NfsPacket packet, IPEndPoint receivedFrom) {
			//Console.WriteLine("NfsHandler.Process : recievedFrom: " + receivedFrom.ToString());
		}
		
		protected override void OnPacketReceived(byte[] bytes, IPEndPoint receivedFrom) {
			var datagram = new DatagramPacket(bytes, bytes.Length, receivedFrom);
			var packet = new NfsPacket(datagram);
			var e = new UdpPacketReceivedEventArgs(packet, receivedFrom);

			//Console.WriteLine("\nPacket Received : EndPoint: " + _listener.Client.Client.LocalEndPoint.ToString());

			uint xId = packet.XID = packet.GetUInt();
			uint type = packet.GetUInt();

			if (type == (int)RpcSignalType.Call) {
				Call(ref packet, xId);
			}
			else if (type == (int)RpcSignalType.Reply) {
				Reply(ref packet, xId);
			}

			//RaisePacketReceived(e);

			if (packet.ProgramID == this.ProgramID) {
				Console.WriteLine("Found program: " + packet.ProgramID);
				Process(packet, receivedFrom);
			}
		}

		protected virtual void Call(ref NfsPacket packet, uint xId) {
			uint rpcVersion = packet.RpcVersion = packet.GetUInt();
			uint programId = packet.ProgramID = packet.GetUInt();
			uint version = packet.NfsVersion = packet.GetUInt();
			uint procedure = packet.ProcedureID = packet.GetUInt();

			IPHostEntry entry = Dns.GetHostEntry(packet.Source);
			String[] parts = entry.HostName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length >= 0) {
				packet.RemoteHost = parts[0];
			}

			if (programId == this.ProgramID) {
				Console.WriteLine("\nCall: rpcVersion = " + rpcVersion + " programId = " + programId + " version = " + version + " procedure = " + procedure);
			}
		}

		protected virtual void Reply(ref NfsPacket packet, uint xId) {
			Console.WriteLine("Reply: I don't handle these");
		}

		protected void SendNull(NfsPacket sourcePacket, IPEndPoint receivedFrom) {

			NfsPacket packet = new NfsPacket(128);

			packet.AddReplyHeader(sourcePacket.XID);

			Send(packet, receivedFrom);
		}
	}
}
