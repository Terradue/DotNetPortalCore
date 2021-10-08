using System;
using System.IO;
using System.Runtime.Serialization;
using Mono.Unix;

namespace Terradue.Cloud {
    [Serializable]
    [DataContract]
    public partial class CloudComputingDriveInfo {
        private string name;

        private UnixDriveInfo driveInfo;

        protected string error;

        [DataMember]
        public string Name {
            get {
                return name;
            }
            protected set {
                name = value;
            }
        }

        [DataMember]
        public virtual long AvailableFreeSpace {
            get {
                try {
                    return driveInfo.AvailableFreeSpace;
                } catch (Exception) {
                    return -1;
                }

            }
        }

        [DataMember]
        public virtual long TotalSize {
            get {
                try {
                    return driveInfo.TotalSize;
                } catch (Exception) {
                    return -1;
                }
            }
        }

        [DataMember]
        public virtual string Error {
            get {
                return error;
            }
        }

        public CloudComputingDriveInfo(string name) {
            this.name = name;
        }

        public CloudComputingDriveInfo(string name, string dirPath) {
            this.driveInfo = new UnixFileInfo(dirPath).GetDriveInfo();
            this.name = name;
        }
    }
}

