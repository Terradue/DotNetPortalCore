using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace Terradue.Cloud {
    public class MachineDiagnostics {

        private float totalPhysicalMemory = 0;
        private float usedPhysicalMemory = 0;
        private float freePhysicalMemory = 0;
        private float userCPU = 0;
        private float systemCPU = 0;
        private float idleCPU = 0;

        public MachineDiagnostics() {
        }

        public void GetSample() {

            if (RunningPlatform() == Platform.Mac) {

                GroupCollection physMem = GetPhysicalMemoryOSX();

                totalPhysicalMemory = float.Parse(physMem["used"].Value) + float.Parse(physMem["free"].Value);
                usedPhysicalMemory = float.Parse(physMem["used"].Value);
                freePhysicalMemory = float.Parse(physMem["free"].Value);

                GroupCollection cpuUsage = GetCPUOSX();

                userCPU = float.Parse(cpuUsage["user"].Value);
                systemCPU = float.Parse(cpuUsage["system"].Value);
                idleCPU = float.Parse(cpuUsage["idle"].Value);

            }

            if (RunningPlatform() == Platform.Linux) {

                GroupCollection physMem = GetPhysicalMemoryLinux();
                try {
                    totalPhysicalMemory = float.Parse(physMem["total"].Value);
                    usedPhysicalMemory = float.Parse(physMem["used"].Value);
                    freePhysicalMemory = float.Parse(physMem["free"].Value);
                }
                catch (FormatException e){
                    throw new FormatException(String.Format("{0}, {1}, {2} : {3}",physMem[1].Value,physMem[2].Value,physMem[3].Value,physMem),e);
                }

                GroupCollection cpuUsage = GetCPULinux();
                try {
                    userCPU = float.Parse(cpuUsage["user"].Value);
                    systemCPU = float.Parse(cpuUsage["system"].Value) + float.Parse(cpuUsage["ni"].Value) + float.Parse(cpuUsage["wait"].Value) + float.Parse(cpuUsage["hi"].Value) + float.Parse(cpuUsage["si"].Value) + float.Parse(cpuUsage["st"].Value);
                    idleCPU = float.Parse(cpuUsage["idle"].Value);
                }
                catch (FormatException e){
                        throw new FormatException(cpuUsage.ToString(),e);
                }

            }

        }

        public float UserCPU {
            get {
                return userCPU;
            }
        }

        public float SystemCPU {
            get {
                return systemCPU;
            }
        }

        public float IdleCPU {
            get {
                return idleCPU;
            }
        }

        public float TotalPhysicalMemory {
            get {

                return totalPhysicalMemory;
            }
        }

        public float UsedPhysicalMemory {
            get {
                return usedPhysicalMemory;
            }
        }

        public float FreePhysicalMemory {
            get {
                return freePhysicalMemory;
            }
        }

        /// <summary>
        /// Gets the physical memory for OS X.
        /// </summary>
        /// <returns>The physical memory in MB.</returns>
        private GroupCollection GetPhysicalMemoryOSX() {

            String physMem = ExecuteShellCommand("top", "-l 1");

            if (physMem == null)
                return null;

            Regex regex = new Regex("^PhysMem: (?<wired>[0-9]*)M wired, (?<active>[0-9]*)M active, (?<inactive>[0-9]*)M inactive, (?<used>[0-9]*)M used, (?<free>[0-9]*)M free.$",RegexOptions.Multiline);

            Match match = regex.Match(physMem);

            if (match.Success) {
                return match.Groups;
            }

            return null;

        }

        private GroupCollection GetCPUOSX() {

            String cpuUsage = ExecuteShellCommand("top", "-l 1'");

            if (cpuUsage == null)
                return null;

            Regex regex = new Regex("^CPU usage: (?<user>[-+]?[0-9]*\\.?[0-9]*)% user, (?<system>[-+]?[0-9]*\\.?[0-9]*)% sys, (?<idle>[-+]?[0-9]*\\.?[0-9]*)% idle$",RegexOptions.Multiline);
            Match match = regex.Match(cpuUsage);

            if (match.Success) {
                return match.Groups;
            }

            return null;

        }

        private GroupCollection GetPhysicalMemoryLinux() {

            String physMem = ExecuteShellCommand("top", "-n 1 -b");

            if (physMem == null)
                return null;

            Regex regex = new Regex("^Mem:\\ *(?<total>[0-9]*)k total,\\ *(?<used>[0-9]*)k used,\\ *(?<free>[0-9]*)k free,\\ *(?<buffers>[0-9]*)k buffers$",RegexOptions.Multiline);

            foreach (Match match in regex.Matches(physMem)) {
                return match.Groups;

            }
            
            regex = new Regex("^KiB Mem : *(?<total>[0-9]*) total, *(?<free>[0-9]*) free, *(?<used>[0-9]*) used, *(?<buffers>[0-9]*) buff\\/cache$", RegexOptions.Multiline);

            foreach (Match match in regex.Matches(physMem)) {
                return match.Groups;

            }

            throw new SystemException(string.Format("Wrong Mem Linux Diagnostic : {0}", physMem));

        }

        private GroupCollection GetCPULinux() {

            String cpuUsage = ExecuteShellCommand("top", "-n 1 -b");

            if (cpuUsage == null)
                return null;

            Regex regex = new Regex("^\\ *Cpu\\(s\\):\\ *(?<user>[0-9]*\\.?[0-9]*)%us,\\ *(?<system>[0-9]*\\.?[0-9]*)%sy,\\ *(?<ni>[0-9]*\\.?[0-9]*)%ni,\\ *(?<idle>[0-9]*\\.?[0-9]*)%id,\\ *(?<wait>[0-9]*\\.?[0-9]*)%wa,\\ *(?<hi>[0-9]*\\.?[0-9]*)%hi,\\ *(?<si>[0-9]*\\.?[0-9]*)%si,\\ *(?<st>[0-9]*\\.?[0-9]*)%st\\ *$",RegexOptions.Multiline);


            foreach (Match match in regex.Matches(cpuUsage)) {
                return match.Groups;
            }

            regex = new Regex("^%Cpu\\(s\\):\\ *(?<user>[0-9]*\\.?[0-9]*)\\ us,\\ *(?<system>[0-9]*\\.?[0-9]*)\\ sy,\\ *(?<ni>[0-9]*\\.?[0-9]*)\\ ni,\\ *(?<idle>[0-9]*\\.?[0-9]*)\\ id,\\ *(?<wait>[0-9]*\\.?[0-9]*)\\ wa,\\ *(?<hi>[0-9]*\\.?[0-9]*)\\ hi,\\ *(?<si>[0-9]*\\.?[0-9]*)\\ si,\\ *(?<st>[0-9]*\\.?[0-9]*)\\ st$", RegexOptions.Multiline);


            foreach (Match match in regex.Matches(cpuUsage)) {
                return match.Groups;
            }

            throw new SystemException(string.Format("Wrong CPU Linux Diagnostic : {0}", cpuUsage));

        }

        /// <summary>
        /// Executes the shell command.
        /// </summary>
        /// <returns>The shell command.</returns>
        /// <param name="command">Command.</param>
        private string ExecuteShellCommand(string command, string arguments) {
            // create the ProcessStartInfo using "sh" as the program to be run,
            // and "-c " as the parameters.
            // Incidentally, /c tells cmd that we want it to execute the command that follows,
            // and then exit.
            System.Diagnostics.ProcessStartInfo procStartInfo =
					new System.Diagnostics.ProcessStartInfo( command, arguments );

            // The following commands are needed to redirect the standard output.
            // This means that it will be redirected to the Process.StandardOutput StreamReader.
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            // Get the output into a string
            string result = proc.StandardOutput.ReadToEnd();
            return result;
        }

        public enum Platform {
            Windows,
            Linux,
            Mac
        }

        public static Platform RunningPlatform() {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Unix:
				// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
				// Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return Platform.Mac;
                    else
                        return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }
    }
}

