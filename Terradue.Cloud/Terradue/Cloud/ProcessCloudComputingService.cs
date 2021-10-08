using System;
using System.Collections.Generic;

namespace Terradue.Cloud
{
	public class ProcessCloudComputingService : CloudComputingService
	{
        private bool UsesSystemCtlCommand;

		private String status = "Unknown";

        private String debug = null;

		private void GetStatus(){
			try {
				System.Diagnostics.Process sc = new System.Diagnostics.Process();
				sc.EnableRaisingEvents = false;
                if (UsesSystemCtlCommand) {
                    sc.StartInfo.FileName = "/usr/bin/systemctl";
                    sc.StartInfo.Arguments = "status " + this.Id;
                } else {
                    sc.StartInfo.FileName = "/sbin/service";
                    sc.StartInfo.Arguments = this.Id + " status";
                }
                sc.StartInfo.RedirectStandardOutput = true;
                sc.StartInfo.RedirectStandardError = true;
                sc.StartInfo.UseShellExecute = false;
                sc.StartInfo.CreateNoWindow = true;
				sc.Start();
				sc.WaitForExit();
				switch ( sc.ExitCode ) {
				case 0 : status = "Started";
					break;
				case 1 : status = "Unrecognized";
                        debug = sc.StandardError.ReadToEnd();
					break;
				case 3 : status = "Stopped";
                        debug = sc.StandardError.ReadToEnd();
					break;
				}
			} catch (Exception e) {
				status = "Error";
                debug = e.Message;
			}
		}

		#region implemented abstract members of CloudComputingService

		public override string Id {
			get ;
			protected set ;
		}

		public override string Name {
			get ;
			set ;
		}

        public override string Debug {
            get {
                return debug;
            }
            protected set {
                debug = value;
            }
        }

        public override void Start () {
            try {
                System.Diagnostics.Process sc = new System.Diagnostics.Process();
                sc.EnableRaisingEvents = false;
                if (UsesSystemCtlCommand) {
                    sc.StartInfo.FileName = "/usr/bin/systemctl";
                    sc.StartInfo.Arguments = "start " + this.Id;
                } else {
                    sc.StartInfo.FileName = "/sbin/service" + this.Id;
                    sc.StartInfo.Arguments = this.Id + " start";
                }
                sc.StartInfo.UseShellExecute = true;
                sc.Start();
                sc.WaitForExit();
                switch ( sc.ExitCode ) {
                case 0 : status = "Started";
                    break;
                case 1 : status = "Unrecognized";
                    break;
                case 3 : status = "Stopped";
                    break;
                }
            } catch (Exception e) {
                status = "Error";
            }
        }

        public override void Stop () {
            try {
                System.Diagnostics.Process sc = new System.Diagnostics.Process();
                sc.EnableRaisingEvents = false;
                if (UsesSystemCtlCommand) {
                    sc.StartInfo.FileName = "/usr/bin/systemctl";
                    sc.StartInfo.Arguments = "stop " + this.Id;
                } else {
                    sc.StartInfo.FileName = "/sbin/service" + this.Id;
                    sc.StartInfo.Arguments = this.Id + " stop";
                }
                sc.StartInfo.UseShellExecute = true;
                sc.Start();
                sc.WaitForExit();
                switch ( sc.ExitCode ) {
                    case 0 : status = "Started";
                        break;
                        case 1 : status = "Unrecognized";
                        break;
                        case 3 : status = "Stopped";
                        break;
                }
            } catch (Exception e) {
                status = "Error";
            }
        }

		#endregion

		public override string Status {
			get {
				return status;
			}
			protected set {}
		}



		public ProcessCloudComputingService (string Id, string Name) : base()
		{
			this.Name = Name;
			this.Id = Id;
		}

		public ProcessCloudComputingService (string Id, string Name, bool direct) : this(Id, Name)
		{
			if (direct)
				GetStatus ();
		}

        public ProcessCloudComputingService(string Id, string Name, bool direct, bool useSystemctl) : this(Id, Name) {

            this.UsesSystemCtlCommand = useSystemctl;
            if (direct)
                GetStatus();
        }

    }
}

