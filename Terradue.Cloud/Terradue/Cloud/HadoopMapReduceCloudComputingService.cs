using System;
using System.Collections.Generic;

namespace Terradue.Cloud
{
	public class HadoopMapReduceCloudComputingService : CloudComputingService
	{

		private ProcessCloudComputingService hadoop_jobtracker;
		private ProcessCloudComputingService hadoop_tasktracker;
		private String aggregatedStatus = "Unknown" ;
        private String aggregatedDebug = null ;

		public HadoopMapReduceCloudComputingService () : base () {
            hadoop_jobtracker = new ProcessCloudComputingService("hadoop-0.20-jobtracker", "Hadoop Job Tracker", true, true);
            hadoop_tasktracker = new ProcessCloudComputingService("hadoop-0.20-tasktracker", "Hadoop Task Tracker", true, true);

            if (hadoop_jobtracker.Status == "Started" && hadoop_tasktracker.Status == "Started") {
                aggregatedStatus = "Started";
            }

            if (hadoop_jobtracker.Status == "Stopped" || hadoop_tasktracker.Status == "Stopped"){
                aggregatedStatus = "Stopped";
                aggregatedDebug = hadoop_jobtracker.Debug + hadoop_tasktracker.Debug;
            }

            if (hadoop_jobtracker.Status == "Unrecognized" || hadoop_tasktracker.Status == "Unrecognized") {
                aggregatedStatus = "Error";
                aggregatedDebug = hadoop_jobtracker.Debug + hadoop_tasktracker.Debug;
            }

            Id = "hadoop-mapred";

		}

		#region implemented abstract members of CloudComputingService

		public override string Id {
			get ;
			protected set ;
		}

		public override string Name {
			get {
				return "Hadoop MapReduce";
			}
			set {
			}
		}

		public override string Status {
			get {
				return aggregatedStatus;
			}
			protected set {}
		}

        public override string Debug {
            get {
                return aggregatedDebug;
            }
            protected set {}
        }

        public override void Start() {
            hadoop_jobtracker.Start();
            hadoop_tasktracker.Start();
        }

        public override void Stop() {
            hadoop_jobtracker.Stop();
            hadoop_tasktracker.Stop();
        }

		#endregion
	}
}

