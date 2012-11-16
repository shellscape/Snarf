using System;
using System.IO;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;

namespace Snarf.Udp {

	public enum UdpReceiverState {
		Stopped,
		Stopping,
		Starting,
		Started
	}

	public class UdpPacketReceivedEventArgs : EventArgs {

		public UdpPacketReceivedEventArgs(UdpPacket packet, IPEndPoint receivedFrom) {
			Packet = packet;
			ReceivedFrom = receivedFrom;
		}

		public UdpPacket Packet { get; private set; }
		public IPEndPoint ReceivedFrom { get; private set; }
	}

	public class UdpHandler : IDisposable {

		public event EventHandler<UdpPacketReceivedEventArgs> PacketReceived = null;

		private Thread _thread = null;
		private bool _disposed = false;
		protected UdpListener _listener = null;
		private long _runState = (long)UdpReceiverState.Stopped;
		protected Guid _uniqueId = Guid.Empty;

		public UdpHandler() { }

		public UdpHandler(int port) {

			_uniqueId = Guid.NewGuid();

			_listener = new UdpListener(port, false);
			_listener.PacketReceived += OnPacketReceived;
		}

		~UdpHandler() {
			this.Dispose(false);
		}

		public int Port { get { return _listener.Port; } }

		public virtual void Start() {
			if (this._thread == null || this._thread.ThreadState == ThreadState.Stopped) {
				this._thread = new Thread(new ThreadStart(this.ThreadStart));
				this._thread.Name = String.Format(CultureInfo.InvariantCulture, "UdpHandler:{0}", _uniqueId);
			}
			else if (this._thread.ThreadState == ThreadState.Running) {
				//throw new ThreadStateException("The request handling process is already running.");
				return; // allow 
			}

			if (this._thread.ThreadState != ThreadState.Unstarted) {
				throw new ThreadStateException("The request handling process was not properly initialized so it could not be started.");
			}
			this._thread.Start();

			long waitTime = DateTime.Now.Ticks + TimeSpan.TicksPerSecond * 10;
			while (_runState != (long)UdpReceiverState.Started) {
				Thread.Sleep(100);
				if (DateTime.Now.Ticks > waitTime) {
					throw new TimeoutException("Unable to start the request handling process.");
				}
			}
		}

		public virtual void Stop() {
			Interlocked.Exchange(ref this._runState, (long)UdpReceiverState.Stopping);
			if (_runState == (long)UdpReceiverState.Starting) {
				this._listener.Stop();
			}
			long waitTime = DateTime.Now.Ticks + TimeSpan.TicksPerSecond * 10;
			while (_runState != (long)UdpReceiverState.Stopped) {
				Thread.Sleep(100);
				if (DateTime.Now.Ticks > waitTime) {
					throw new TimeoutException("Unable to stop the web server process.");
				}
			}

			this._thread = null;
		}

		private void ThreadStart() {
			Interlocked.Exchange(ref this._runState, (long)UdpReceiverState.Starting);
			try {
				if (_runState != (long)UdpReceiverState.Started) {
					this._listener.Start();
					Interlocked.Exchange(ref this._runState, (long)UdpReceiverState.Started);
				}

				try {
					while (_runState == (long)UdpReceiverState.Started) {
						//NfsListenerContext context = this._listener.GetContext();
						//this.OnPacketReceived(context);
					}
				}
				catch (Exception) {
					// This will occur when the listener gets shut down.
					// Just swallow it and move on.
				}
			}
			finally {
				Interlocked.Exchange(ref this._runState, (long)UdpReceiverState.Stopped);
			}
		}

		protected virtual void OnPacketReceived(byte[] bytes, IPEndPoint receivedFrom) {
			var datagram = new DatagramPacket(bytes, bytes.Length, receivedFrom);
			var packet = new UdpPacket(datagram);
			var e = new UdpPacketReceivedEventArgs(packet, receivedFrom);			
			RaisePacketReceived(e);
		}

		protected void RaisePacketReceived(UdpPacketReceivedEventArgs e) {
			try {
				if (this.PacketReceived != null) {
					this.PacketReceived.BeginInvoke(this, e, null, null);
				}
			}
			catch {
				// Swallow the exception and/or log it, but you probably don't want to exit
				// just because an incoming request handler failed.
			}
		}

		public void Send(UdpPacket packet, IPEndPoint dest) {
			Console.WriteLine("Sending Data: length: " + packet.Length + " -> " + dest.ToString());

			_listener.Client.Send(packet.Data, packet.Length, dest);
		}

		#region .    IDisposable    .

		public virtual void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (this._disposed) {
				return;
			}
			if (disposing) {
				if (_runState != (long)UdpReceiverState.Stopped) {
					this.Stop();
				}
				if (this._thread != null) {
					this._thread.Abort();
					this._thread = null;
				}
			}
			this._disposed = true;
		}

		#endregion
	}

}