// resused from http://growl-for-windows.googlecode.com/svn-history/r125/trunk/Growl/Growl.UDPLegacy/UdpListener.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Snarf.Udp {

	/// <summary>
	/// Simple class to represent state when used with a UdpListener
	/// </summary>
	public class UdpState {
		public UdpClient Udp;
		public IPEndPoint Endpoint;
		public AsyncCallback Callback;
	}

	/// <summary>
	/// A basic listener that listens for incoming UDP messages on the specified port
	/// and passes the event on to application code whenever a message is received.
	/// </summary>
	public class UdpListener : IDisposable {

		private static object syncLock = new object();
		private static Dictionary<IPAddress, IPAddress> masks;

		private Boolean _started = false;

		/// <summary>
		/// The port to listen for messages on
		/// </summary>
		protected int port;
		/// <summary>
		/// Indicates if messages from remote machines should be allowed or not
		/// </summary>
		protected bool localMessagesOnly = true;
		/// <summary>
		/// The underlying <see cref="UdpClient"/>
		/// </summary>
		protected UdpClient udp;
		/// <summary>
		/// Event handlder for the <see cref="PacketReceived"/> event
		/// </summary>
		/// <param name="bytes">The raw packet data</param>
		/// <param name="receivedFrom">The host that sent the message</param>
		/// <param name="isLocal">Indicates if the request came from the local machine</param>
		/// <param name="isLAN">Indicates if the request came from the LAN</param>
		public delegate void PacketHandler(byte[] bytes, IPEndPoint receivedFrom);
		/// <summary>
		/// Fires when a message is received
		/// </summary>
		public event PacketHandler PacketReceived;

		/// <summary>
		/// Creates a new <see cref="UdpListener"/>
		/// </summary>
		/// <param name="port">The port to listen for messages on</param>
		/// <param name="localMessagesOnly"><c>true</c> to only listen for messages from the local machine;<c>false</c> to listen for messages from any source</param>
		public UdpListener(int port, bool localMessagesOnly) {
			this.port = port;
			this.localMessagesOnly = localMessagesOnly;
		}

		public int Port { get { return port; } }

		public UdpClient Client { get { return udp; } }

		public void Start() {
			Start(null);
		}

		/// <summary>
		/// Starts listening for messages on the specified port
		/// </summary>
		public void Start(AsyncCallback callback) {

			if (_started) {
				return;
			}

			_started = true;

			IPAddress address = (this.localMessagesOnly ? IPAddress.Loopback : IPAddress.Any);
			//IPAddress address = Dns.GetHostAddresses(Dns.GetHostName()).Where(o => o.IsIPv6LinkLocal == false && o.IsIPv6Teredo == false).FirstOrDefault();
			IPEndPoint endpoint = new IPEndPoint(address, this.port);
			this.udp = new UdpClient(endpoint);
			this.port = ((IPEndPoint)udp.Client.LocalEndPoint).Port; // if 0 is passed, the system will assign a port.

			if (callback == null) {
				callback = new AsyncCallback(this.ProcessPacket);
			}

			UdpState state = new UdpState();
			state.Udp = udp;
			state.Endpoint = endpoint;
			state.Callback = callback;

			udp.BeginReceive(callback, state);
		}

		/// <summary>
		/// Stops listening for messages and frees the port
		/// </summary>
		public void Stop() {
			if (!_started) {
				return;
			}

			_started = false;

			try {
				this.udp.Close();
				this.udp = null;
			}
			finally {
			}
		}

		/// <summary>
		/// When a message is received by the listener, the raw data is read from the packet
		/// and the <see cref="PacketReceived"/> event is fired.
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/></param>
		private void ProcessPacket(IAsyncResult ar) {
			try {
				UdpClient udp = (UdpClient)((UdpState)(ar.AsyncState)).Udp;
				IPEndPoint endpoint = (IPEndPoint)((UdpState)(ar.AsyncState)).Endpoint;
				AsyncCallback callback = (AsyncCallback)((UdpState)(ar.AsyncState)).Callback;

				byte[] bytes = udp.EndReceive(ar, ref endpoint);

				IPAddress localAddress = IPAddress.Loopback;
				IPEndPoint localEndpoint = (IPEndPoint)udp.Client.LocalEndPoint;
				if (localEndpoint != null) localAddress = localEndpoint.Address;

				//bool isLocal = IPAddress.IsLoopback(endpoint.Address);
				//bool isLAN = IsInSameSubnet(localAddress, endpoint.Address);
				//string receivedFrom = endpoint.ToString();

				// start listening again
				udp.BeginReceive(callback, ar.AsyncState);

				// bubble up the event
				if (this.PacketReceived != null) this.PacketReceived(bytes, endpoint);
			}
			catch {
				// swallow any exceptions (this handles the case when Growl is stopped while still listening for network notifications)
			}
		}

		public static bool IsInSameSubnet(IPAddress localAddress, IPAddress otherAddress) {
			try {
				// handle loopback addresses and IPv6 local addresses
				if (IPAddress.IsLoopback(otherAddress)
						|| otherAddress.IsIPv6LinkLocal
						|| otherAddress.IsIPv6SiteLocal)
					return true;

				IPAddress subnetMask = GetLocalSubnetMask(localAddress);
				IPAddress network1 = GetNetworkAddress(localAddress, subnetMask);
				IPAddress network2 = GetNetworkAddress(otherAddress, subnetMask);
				return network1.Equals(network2);
			}
			catch {
				Console.WriteLine(String.Format("Could not determine subnet. Local address: {0} - Remote Address: {1}", localAddress, otherAddress));
			}
			return false;
		}

		public static IPAddress GetLocalSubnetMask(IPAddress ipaddress) {
			lock (syncLock) {
				if (masks == null) {
					masks = new Dictionary<IPAddress, IPAddress>();

					NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
					foreach (NetworkInterface ni in interfaces) {
						//Console.WriteLine(ni.Description);

						UnicastIPAddressInformationCollection unicastAddresses = ni.GetIPProperties().UnicastAddresses;
						foreach (UnicastIPAddressInformation unicastAddress in unicastAddresses) {
							IPAddress mask = (unicastAddress.IPv4Mask != null ? unicastAddress.IPv4Mask : IPAddress.None);
							masks.Add(unicastAddress.Address, mask);

							//Console.WriteLine("\tIP Address is {0}", unicastAddress.Address);
							//Console.WriteLine("\tSubnet Mask is {0}", mask);
						}
					}
				}
			}

			if (masks.ContainsKey(ipaddress))
				return masks[ipaddress];
			else
				return IPAddress.None;
		}

		public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask) {
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for (int i = 0; i < broadcastAddress.Length; i++) {
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress(broadcastAddress);
		}

		#region IDisposable Members

		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				try {
					if (this.udp != null) this.udp.Close();
				}
				catch {
					// suppress
				}
			}
		}

		#endregion
	}
}