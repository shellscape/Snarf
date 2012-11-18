using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {

	[DataContract(Name = "apphandles")]
	public class HandleManager : Shellscape.Configuration.Config<HandleManager> {

		private object _lock = new object();
		private List<String> _handles = new List<string>() { null };

		[DataMember(Name="handles")]
		public List<String> Handles {
			get { return _handles; }
			set { _handles = value; }
		}

		public uint GetHandle(String name) {
			lock (_lock) {
				if (_handles.Contains(name)) {
					return (uint)_handles.IndexOf(name);
				}
				var handleId = _handles.Count;
				_handles.Add(name);
				this.Save();

				return (uint)handleId;
			}
		}

		public String GetName(uint handleId) {
			lock (_lock) {
				if (_handles.Count >= handleId) {
					return _handles[(int)handleId];
				}
				throw new Exception("HandleManager.GetName: handleId not found.");
			}
		}
	
		protected override string ApplicationName {
			get { return Shellscape.Utilities.AssemblyMeta.AssemblyName; }
		}

		protected override void SetDefaults() {
			_lock = new object();
			this.FileName = "app.handles";
#if DEBUG
			this.AppDataPath = this.StorePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
#endif
		}
	}
}