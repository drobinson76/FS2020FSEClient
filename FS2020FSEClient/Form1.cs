using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FS2020FSEClient {
    public partial class Form1 : Form {
        readonly UInt64[] latOffset = { 0x0305ACD8, 0x18 };
        readonly UInt64[] lonOffset = { 0x0305ACD8, 0x20 };
        readonly UInt64[] acTypeOffset = { 0x03AB62C8, 0x698 };
        readonly UInt64[] leftTankOffset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 0) };
        readonly UInt64[] rightTankOffset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 1) };
        readonly UInt64[] leftAuxOffset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 2) };
        readonly UInt64[] rightAuxOffset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 3) };
        readonly UInt64[] center3Offset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 4) };
        readonly UInt64[] center2Offset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 5) };
        readonly UInt64[] center1Offset = { 0x032BC038, 0x1920, 0x3C8 + (0x28 * 6) };
        readonly UInt64[] pilotMassOffset = { 0x03D3EF10, 0x28, 0x6C8, 0x650, 0x68, 0x114 };
        readonly UInt64[] copilotMassOffset = { 0x03D3EF10, 0x28, 0x6C8, 0x650, 0x68, 0x114 + (0x138 * 1) };


        Dictionary<string, offset> offsetDict = new Dictionary<string, offset>();
        Process gameProcess = Process.GetProcessesByName("FlightSimulator").FirstOrDefault();
        MemoryClass m;
        bool flightStarted = false;
        Stopwatch flightTimer = new Stopwatch();
        public Form1() {
            InitializeComponent();

            offsetDict.Add("Lat", new offset() { offsetName = "Lat", offsetArray = new UInt64[]{ 0x0305ACD8, 0x18 }, offsetType = typeof(double) });
            offsetDict.Add("Lon", new offset() { offsetName = "Lon", offsetArray = new UInt64[]{ 0x0305ACD8, 0x20 }, offsetType = typeof(double) });
            offsetDict.Add("Aicraft Type", new offset() { offsetName = "Aicraft Type", offsetArray = new UInt64[]{ 0x03AB62C8, 0x698 }, offsetType = typeof(string) });
            offsetDict.Add("Payloads", new offset() { offsetName = "Payloads", offsetArray = new UInt64[] { 0x03D3EF10, 0x28, 0x6C8, 0x650, 0x68, 0x114 }, offsetType = typeof(float), arrayEndMulti = 0x138 });
            offsetDict.Add("Fuel Tanks", new offset() { offsetName = "Fuel Tanks", offsetArray = new UInt64[] { 0x032BC038, 0x1920, 0x3C8 }, offsetType = typeof(float), arrayEndMulti = 0x28 });
            new AircraftStats(new fuelTank[] { new fuelTank(0, 37.0f, "LEFT MAIN"), new fuelTank(1, 37.0f, "RIGHT MAIN") }, "Bonanza G36 Asobo", 7, "Beechcraft Bonanza A36", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 165.0f, "LEFT MAIN"), new fuelTank(1, 165.0f, "RIGHT MAIN") }, "Cessna 208B Grand Caravan EX", 15, "Cessna 208 Caravan", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 28.0f, "LEFT MAIN"), new fuelTank(1, 28.0f, "RIGHT MAIN") }, "Cessna Skyhawk G1000 Asobo", 5, "Cessna 172 Skyhawk", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 14.0f, "LEFT MAIN"), new fuelTank(1, 14.0f, "RIGHT MAIN") }, "DA40-NG Asobo", 5, "Diamond DA40D DiamondStar", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 26.0f, "LEFT MAIN"), new fuelTank(1, 26.0f, "RIGHT MAIN"), new fuelTank(2, 18.5f, "LEFT AUX"), new fuelTank(3, 18.5f, "RIGHT AUX") }, "DA62 Asobo", 9, "Diamond DA62", 2);
            new AircraftStats(new fuelTank[] { new fuelTank(6, 29.0f, "CENTER1") }, "DR400 Asobo", 3, "Robin DR400", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 146.0f, "LEFT MAIN"), new fuelTank(1, 146.0f, "RIGHT MAIN") }, "TBM 930 Asobo", 8, "Socata TBM 850", 1);
            new AircraftStats(new fuelTank[] { new fuelTank(0, 17.0f, "LEFT MAIN"), new fuelTank(1, 17.0f, "RIGHT MAIN") }, "Asobo XCub", 3, "Piper PA-18 Super Cub", 1);

            var hProcess = Win32.OpenProcess((uint)ProcessAccessFlags.All, false, (uint)gameProcess.Id);
            m = new MemoryClass(hProcess);

        }

        private void button1_Click(object sender, EventArgs e) {
            richTextBox1.Clear();
            string ac = m.ReadString(m.addressFromOffsetList(offsetDict["Aicraft Type"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress), 40).Split(new[] { '\0' }, 2)[0];
            richTextBox1.AppendText(ac + "\n");
            foreach (fuelTank f in AircraftStats.allAircraft[ac].fuelTanks) {
                richTextBox1.AppendText(f.name + ": " + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(f.tankArrayIndex), (UInt64)gameProcess.MainModule.BaseAddress)) + "\n");
            }

            for(uint i = 0; i < AircraftStats.allAircraft[ac].payloadArrayLength; i++) { 
                richTextBox1.AppendText(i.ToString() + ": " + m.ReadFloat(m.addressFromOffsetList(offsetDict["Payloads"].getOffsetAtArrayIndex(i), (UInt64)gameProcess.MainModule.BaseAddress)) + "\n");
            }
        }

        private void startEndFlight_Button_Click(object sender, EventArgs e) {
            if (flightStarted) {
                flightTimer.Stop();
                long flightTime = flightTimer.ElapsedMilliseconds / 1000;
                string ac = m.ReadString(m.addressFromOffsetList(offsetDict["Aicraft Type"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress), 40).Split(new[] { '\0' }, 2)[0];
                double lat = m.ReadDouble(m.addressFromOffsetList(offsetDict["Lat"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress));
                double lon = m.ReadDouble(m.addressFromOffsetList(offsetDict["Lon"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress));
                Random random = new Random();
                double mixtureDamage = random.NextDouble(0.0, flightTime);
                double chtDamage = random.NextDouble(0.0, mixtureDamage);

                string url = "http://server.fseconomy.net/fsagentx?md5sum=dc8e8e74f259e9a6fcb14bb2e4b71a49&user=" + Username_textbox.Text + "&pass=" + Password_textbox.Text +
                    "&action=arrive&rentalTime=" + flightTime + "&lat=" + lat + "&lon=" + lon +
                    "&c=" + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(6), (UInt64)gameProcess.MainModule.BaseAddress)) / 5.926f +
                    "&lm=" + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(0), (UInt64)gameProcess.MainModule.BaseAddress)) / 5.926f +
                    "&la=" + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(2), (UInt64)gameProcess.MainModule.BaseAddress)) / 5.926f +
                    "&let=" + 0.0 +
                    "&rm=" + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(1), (UInt64)gameProcess.MainModule.BaseAddress)) / 5.926f +
                    "&ra=" + m.ReadFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(3), (UInt64)gameProcess.MainModule.BaseAddress)) / 5.926f +
                    "&rt=" + 0.0 +
                    "&c2=" + 0.0 +
                    "&c3=" + 0.0 +
                    "&x1=" + 0.0 +
                    "&x2=" + 0.0;

                for (int i = 1; i <= AircraftStats.allAircraft[ac].numOfEngines; i++) {
                    url += "&mixture" + i + "=" + mixtureDamage + "&heat" + i + "=" + chtDamage + "&time" + i + "=" + flightTime;
                }

                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                var response = client.Execute(request);
                richTextBox1.Text = response.Content;
                flightStarted = false;
                startEndFlight_Button.Text = "Start Flight";
            } else {
                string ac = m.ReadString(m.addressFromOffsetList(offsetDict["Aicraft Type"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress), 40).Split(new[] { '\0' }, 2)[0];
                double lat = m.ReadDouble(m.addressFromOffsetList(offsetDict["Lat"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress));
                double lon = m.ReadDouble(m.addressFromOffsetList(offsetDict["Lon"].getOffset(), (UInt64)gameProcess.MainModule.BaseAddress));
                string url = "http://server.fseconomy.net/fsagentx?md5sum=dc8e8e74f259e9a6fcb14bb2e4b71a49&user=" + Username_textbox.Text + "&pass=" + Password_textbox.Text + "&action=startFlight&" +
                "lat=" + lat +
                "&lon=" + lon +
                "&aircraft=" + AircraftStats.allAircraft[ac].fseFriendlyName;

                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                richTextBox1.Text = response.Content;
                Regex regex = new Regex(@"(?<=<payloadWeight>).*(?=</payloadWeight>)");
                float totalPayload = int.Parse(regex.Match(response.Content).Value) * 2.205f;
                regex = new Regex(@"(?<=<fuel>).*(?=</fuel>)");
                float fuelTotalGal = Array.ConvertAll(regex.Match(response.Content).Value.Split(' '), s => float.TryParse(s, out var i) ? i : 0).Sum();
                float fuelLeftLb = fuelTotalGal * 5.926f;
                foreach (fuelTank f in AircraftStats.allAircraft[ac].fuelTanks) {
                    float fuelToPut = 0.0f;
                    if (fuelLeftLb > (f.maxFuel * 5.926f)) {
                        fuelToPut = f.maxFuel * 5.926f;
                        fuelLeftLb -= (f.maxFuel * 5.926f);
                    } else if (fuelLeftLb > 0) {
                        fuelToPut = fuelLeftLb;
                        fuelLeftLb = 0.0f;
                    }
                    m.WriteFloat(m.addressFromOffsetList(offsetDict["Fuel Tanks"].getOffsetAtArrayIndex(f.tankArrayIndex), (UInt64)gameProcess.MainModule.BaseAddress), fuelToPut);
                }
                float payloadPerPayloadArea = totalPayload / AircraftStats.allAircraft[ac].payloadArrayLength;
                for (uint i = 0; i < AircraftStats.allAircraft[ac].payloadArrayLength; i++) {
                    m.WriteFloat(m.addressFromOffsetList(offsetDict["Payloads"].getOffsetAtArrayIndex(i), (UInt64)gameProcess.MainModule.BaseAddress), payloadPerPayloadArea);
                }
                startEndFlight_Button.Text = "End Flight";
                flightTimer.Reset();
                flightTimer.Start();
                flightStarted = true;

            }
        }
    }

    public static class RandomExtensions {
        public static double NextDouble(
            this Random random,
            double minValue,
            double maxValue) {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }

    public class AircraftStats {
        public static Dictionary<string, AircraftStats> allAircraft = new Dictionary<string, AircraftStats>();
        public fuelTank[] fuelTanks;
        string name;
        public uint payloadArrayLength;
        public string fseFriendlyName;
        public int numOfEngines;
        public AircraftStats(fuelTank[] tanks, string acName, uint payloadAreas, string fseName, int engines) {
            fuelTanks = tanks;
            name = acName;
            payloadArrayLength = payloadAreas;
            fseFriendlyName = fseName;
            numOfEngines = engines;
            allAircraft.Add(name, this);
        }
    }

    public struct fuelTank {
        public uint tankArrayIndex;
        public float maxFuel;
        public string name;

        public fuelTank(uint index, float max, string _name) {
            tankArrayIndex = index;
            maxFuel = max;
            name = _name;
        }
    }

    public struct offset {
        public string offsetName;
        public UInt64[] offsetArray;
        public Type offsetType;
        public uint arrayEndMulti;

        public UInt64[] getOffsetAtArrayIndex(uint index) {
            UInt64[] ret = (UInt64[])offsetArray.Clone();
            ret[ret.Length - 1] += (UInt64)(index * arrayEndMulti);
            return ret;
        }

        public UInt64[] getOffset() {
            return offsetArray;
        }
    }
    public class MemoryClass {
        IntPtr process;
        public MemoryClass(IntPtr _process) {
            process = _process;
        }

        public UInt64 addressFromOffsetList(IEnumerable<UInt64> offsets, UInt64 baseAddress) {
            UInt64 curOffset = baseAddress;
            for (int i = 0; i < offsets.Count() - 1; i++) {
                curOffset = ReadUint64(curOffset + offsets.ElementAt(i));
            }
            return curOffset + offsets.Last();
        }

        public UInt64 ReadUint64(UInt64 address) {
            try {
                return BitConverter.ToUInt64(ReadByteArray((IntPtr)address, 8U), 0);
            } catch {
                return 0;
            }
        }

        public double ReadDouble(UInt64 address) {
            try {
                return BitConverter.ToDouble(ReadByteArray((IntPtr)address, 8U), 0);
            } catch {
                return 0;
            }
        }

        public float ReadFloat(UInt64 address) {
            try {
                return BitConverter.ToSingle(ReadByteArray((IntPtr)address, 4U), 0);
            } catch {
                return 0;
            }
        }

        public string ReadString(UInt64 address, uint maxStringLength) {
            try {
                return Encoding.UTF8.GetString(ReadByteArray((IntPtr)address, maxStringLength));
            } catch {
                return "";
            }
        }

        public bool WriteFloat(UInt64 address, float data) {
            try {
                return WriteByteArray((IntPtr)address, BitConverter.GetBytes(data));
            } catch {
                return false;
            }
        }

        public byte[] ReadByteArray(IntPtr address, uint size) {
            if (process == IntPtr.Zero)
                throw new Exception("process is not set");
            try {
                uint lpflOldProtect;
                Win32.VirtualProtectEx(process, address, (UIntPtr)size, (uint)VirtualMemoryProtection.PAGE_READWRITE, out lpflOldProtect);
                byte[] lpBuffer = new byte[size];
                Win32.ReadProcessMemory(process, address, lpBuffer, size, 0U);
                Win32.VirtualProtectEx(process, address, (UIntPtr)size, lpflOldProtect, out lpflOldProtect);
                return lpBuffer;
            } catch {
                throw new Exception("Error reading byte array");
            }
        }

        public bool WriteByteArray(IntPtr pOffset, byte[] pBytes) {
            if (process == IntPtr.Zero)
                throw new Exception("process is not set");
            try {
                uint lpflOldProtect;
                Win32.VirtualProtectEx(process, pOffset, (UIntPtr)((ulong)pBytes.Length), 4U, out lpflOldProtect);
                bool flag = Win32.WriteProcessMemory(process, pOffset, pBytes, (uint)pBytes.Length, 0U);
                Win32.VirtualProtectEx(process, pOffset, (UIntPtr)((ulong)pBytes.Length), lpflOldProtect, out lpflOldProtect);
                return flag;
            } catch {
                return false;
            }
        }

    }

    public enum AllocationType : uint {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    public enum ProcessAccessFlags : uint {
        All = 2035711, // 0x001F0FFF
        Terminate = 1,
        CreateThread = 2,
        VMOperation = 8,
        VMRead = 16, // 0x00000010
        VMWrite = 32, // 0x00000020
        DupHandle = 64, // 0x00000040
        SetInformation = 512, // 0x00000200
        QueryInformation = 1024, // 0x00000400
        Synchronize = 1048576, // 0x00100000
    }

    public enum VirtualMemoryProtection : uint {
        PAGE_NOACCESS = 1,
        PAGE_READONLY = 2,
        PAGE_READWRITE = 4,
        PAGE_WRITECOPY = 8,
        PAGE_EXECUTE = 16, // 0x00000010
        PAGE_EXECUTE_READ = 32, // 0x00000020
        PAGE_EXECUTE_READWRITE = 64, // 0x00000040
        PAGE_EXECUTE_WRITECOPY = 128, // 0x00000080
        PAGE_GUARD = 256, // 0x00000100
        PAGE_NOCACHE = 512, // 0x00000200
        PROCESS_ALL_ACCESS = 2035711, // 0x001F0FFF
    }

    public static class Win32 {

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint dwSize,
            uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            AllocationType dwFreeType);
    }

}
