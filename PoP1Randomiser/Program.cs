using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;

class Program
{
    const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    const VirtualKeyCode VK_F11 = VirtualKeyCode.F11;
    const int VK_L = 0x4C;

    static void Main()
    {
        Process dosboxProcess = GetDOSBoxProcess();

        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, dosboxProcess.Id);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open DOSBox-X process.");
            return;
        }

        long baseAddress = (long)dosboxProcess.MainModule.BaseAddress.ToInt64();
        long addressToModify = baseAddress + 0x13F6DA8;
        int[] offsets = { 0x23C70 };

        IntPtr finalAddress = new IntPtr(ReadPointerAddress(hProcess, (IntPtr)addressToModify, offsets));

        const int bufferSize = 4;
        byte[] newValueBuffer = new byte[bufferSize];

        Console.WriteLine("Press F11+L in DOSBox-X to modify memory...");

        InputSimulator inputSimulator = new InputSimulator();

        while (true)
        {
            if (inputSimulator.InputDeviceState.IsKeyDown(VK_F11) && inputSimulator.InputDeviceState.IsKeyDown((VirtualKeyCode)VK_L))
            {
                Thread.Sleep(200);
                Random random = new Random();
                random.NextBytes(newValueBuffer);

                bool success = WriteProcessMemory(hProcess, finalAddress, newValueBuffer, (uint)bufferSize, out _);
                if (success)
                {
                    Console.WriteLine("Memory address modified successfully!");
                }
                else
                {
                    Console.WriteLine("Failed to modify memory address.");
                }
            }
        }
    }

    static Process GetDOSBoxProcess()
    {
        Process[] processes = Process.GetProcessesByName("dosbox-x");

        if (processes.Length > 0)
        {
            return processes[0];
        }

        return null;
    }

    static long ReadPointerAddress(IntPtr hProcess, IntPtr baseAddress, int[] offsets)
    {
        long address = baseAddress.ToInt64();

        foreach (int offset in offsets)
        {
            byte[] buffer = new byte[sizeof(long)];

            ReadProcessMemory(hProcess, (IntPtr)address, buffer, sizeof(long), out _);

            address = BitConverter.ToInt64(buffer, 0) + offset;
        }

        return address;
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);
    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);
}
