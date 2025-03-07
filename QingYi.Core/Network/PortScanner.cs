using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#pragma warning disable IDE0028, IDE0090, IDE0300, IDE0305, CA1861

namespace QingYi.Core.Network
{
    /// <summary>
    /// This class provides methods to scan open ports on the system and filter them based on the application name.
    /// </summary>
    public class PortScanner
    {
        /// <summary>
        /// Represents information about a port, including protocol, local address, foreign address, state, process ID, and application name.
        /// </summary>
        public class PortInfo
        {
            /// <summary>
            /// Gets or sets the protocol (TCP/UDP) of the port.
            /// </summary>
            public string Protocol { get; set; }

            /// <summary>
            /// Gets or sets the local address of the port.
            /// </summary>
            public string LocalAddress { get; set; }

            /// <summary>
            /// Gets or sets the foreign address of the port.
            /// </summary>
            public string ForeignAddress { get; set; }

            /// <summary>
            /// Gets or sets the state of the port (e.g., LISTENING, ESTABLISHED).
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// Gets or sets the process ID (PID) associated with the port.
            /// </summary>
            public string PID { get; set; }

            /// <summary>
            /// Gets or sets the application name that is using the port.
            /// </summary>
            public string ApplicationName { get; set; }
        }

        /// <summary>
        /// Gets a list of open ports on the system.
        /// </summary>
        /// <returns>A list of <see cref="PortInfo"/> objects representing the open ports.</returns>
        public static List<PortInfo> GetOpenPorts()
        {
            List<PortInfo> portInfos = new List<PortInfo>();

            // Execute netstat command
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano", // -a: Display all connections and listening ports, -n: Display numerical addresses and port numbers, -o: Display process ID
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();

            // Read command output
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse output
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.Contains("TCP") || line.Contains("UDP"))
                {
                    // Split the line and remove extra spaces
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Ensure at least 5 elements
                    if (parts.Length >= 5)
                    {
                        string protocol = parts[0];
                        string localAddress = parts[1];
                        string foreignAddress = parts[2];
                        string state = parts[3];
                        string pid = parts[4];

                        PortInfo portInfo = new PortInfo
                        {
                            Protocol = protocol,
                            LocalAddress = localAddress,
                            ForeignAddress = foreignAddress,
                            State = state,
                            PID = pid
                        };

                        // Get the corresponding application name by PID
                        try
                        {
                            Process appProcess = Process.GetProcessById(int.Parse(pid));
                            portInfo.ApplicationName = appProcess.ProcessName;
                        }
                        catch (Exception ex)
                        {
                            portInfo.ApplicationName = "Unknown (Error: " + ex.Message + ")";
                        }

                        portInfos.Add(portInfo);
                    }
                }
            }

            return portInfos;
        }

        /// <summary>
        /// Filters the list of open ports by the application name.
        /// </summary>
        /// <param name="applicationName">The name of the application to filter by.</param>
        /// <returns>A list of <see cref="PortInfo"/> objects for the specified application.</returns>
        public static List<PortInfo> GetFilteredPortsByApplication(string applicationName)
        {
            List<PortInfo> portInfos = new List<PortInfo>();

            // Execute netstat command
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano", // -a: Display all connections and listening ports, -n: Display numerical addresses and port numbers, -o: Display process ID
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();

            // Read command output
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse output
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Only process lines containing TCP or UDP
                if (line.Contains("TCP") || line.Contains("UDP"))
                {
                    // Split the line and remove extra spaces
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Ensure at least 5 elements
                    if (parts.Length >= 5)
                    {
                        string protocol = parts[0];
                        string localAddress = parts[1];
                        string foreignAddress = parts[2];
                        string state = parts[3];
                        string pid = parts[4];

                        // Filtering condition: exclude local addresses (127.x.x.x, 192.168.x.x) and IPv6 addresses (containing '[')
                        if ((localAddress.StartsWith("127.") || !localAddress.Contains("192.168")) && !localAddress.Contains('['))
                        {
                            PortInfo portInfo = new PortInfo
                            {
                                Protocol = protocol,
                                LocalAddress = localAddress,
                                ForeignAddress = foreignAddress,
                                State = state,
                                PID = pid
                            };

                            // Get the corresponding application name by PID
                            try
                            {
                                Process appProcess = Process.GetProcessById(int.Parse(pid));
                                portInfo.ApplicationName = appProcess.ProcessName;
                            }
                            catch (Exception ex)
                            {
                                portInfo.ApplicationName = "Unknown (Error: " + ex.Message + ")";
                            }

                            portInfos.Add(portInfo);
                        }
                    }
                }
            }

            // Use FilterByApplicationName to filter ports by application name
            return FilterByApplicationName(portInfos, applicationName);
        }

        /// <summary>
        /// Filters the list of <see cref="PortInfo"/> objects based on the application name.
        /// </summary>
        /// <param name="portInfos">The list of <see cref="PortInfo"/> objects to filter.</param>
        /// <param name="applicationName">The name of the application to filter by.</param>
        /// <returns>A list of <see cref="PortInfo"/> objects for the specified application name.</returns>
        public static List<PortInfo> FilterByApplicationName(List<PortInfo> portInfos, string applicationName) => portInfos.Where(p => p.ApplicationName.Equals(applicationName, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
