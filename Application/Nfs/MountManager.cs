using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Snarf.Nfs {

	public class Mount {

		public String ClientID { get; set; }
		public String MountPoint { get; set; }

	}

	[DataContract(Name = "appmounts")]
	public class MountManager : Shellscape.Configuration.Config<MountManager> {

		private object _lock = new object();
		private List<Mount> _mounts = new List<Mount>();

		[DataMember(Name="mounts")]
		public List<Mount> Mounts {
			get { return _mounts; }
			set { _mounts = value; }
		}

		public Boolean Add(String clientId, String mountPoint) {
			var mount = _mounts.Where(o => o.ClientID == clientId && o.MountPoint == mountPoint).FirstOrDefault();
			if (mount != null) {
				return false;
			}
			_mounts.Add(new Mount() { ClientID = clientId, MountPoint = mountPoint });
			this.Save();
			return true;
		}

		public Boolean Remove(String clientId) {
			var mount = _mounts.Where(o => o.ClientID == clientId).FirstOrDefault();
			if (mount != null && _mounts.Contains(mount)) {
				_mounts.Remove(mount);
				this.Save();
			}
			else {
				return false;
				//throw new Exception("MountManager.Remove: mount(" + clientId + ") not found.");
			}
			return true;
		}
	
		protected override string ApplicationName {
			get { return Shellscape.Utilities.AssemblyMeta.AssemblyName; }
		}

		protected override void SetDefaults() {
			_lock = new object();
			this.FileName = "app.mounts";
#if DEBUG
			this.AppDataPath = this.StorePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
#endif
		}
	}
}