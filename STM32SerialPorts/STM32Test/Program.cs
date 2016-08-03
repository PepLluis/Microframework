/// STM32F4 Microframework multiple Serial Ports lab
/// (C) PepLluis 2015

#region Imports
using System;
using System.IO.Ports;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using System.Text;
#endregion

namespace STM32_SerialPorts
{
    // PepLluis Agost 2015
    public class Program
    {
        // const int Port_A = 0;
        // PB = 16-31
        // PC = 32-47
        // PD     = 48; // Port GPIO D = 48-63
        // GPIO12 = 12; // Led - Green
        // GPIO13 = 13; // Led - Orange
        // GPIO14 = 14; // Led - Red
        // GPIO15 = 15; // Led - Blue
        static OutputPort Green     = new OutputPort((Cpu.Pin) STM32F4_Port.D + GP.IO0, false);
        static OutputPort Orange    = new OutputPort((Cpu.Pin) STM32F4_Port.D + GP.IO13, false);
        static OutputPort Red       = new OutputPort((Cpu.Pin) STM32F4_Port.D + GP.IO14, false);
        static OutputPort Blue      = new OutputPort((Cpu.Pin) STM32F4_Port.D + GP.IO15, false);

        static InterruptPort pushBtn = new InterruptPort((Cpu.Pin) STM32F4_Port.A + GP.IO0, false, InterruptPort.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);
        static SerialPort serialPort1;
        static SerialPort serialPort2;
        static SerialPort serialPort3;
        static byte[] buffer = Encoding.UTF8.GetBytes("@00RD0400001053*"); // Simulate Omrom Serial port comm
        static bool reset;

        public static void Main()
        {
            // Threading system timers
            Timer blueSequence      = new Timer(new TimerCallback(blueTask),    false, 10000, 250);
            Timer redSequence       = new Timer(new TimerCallback(redTask),     false, 10000, 500);
            Timer orangeSequence    = new Timer(new TimerCallback(orangeTask),  false, 10000, 1000);
            // input interrupt
            pushBtn.OnInterrupt += pushBtn_OnInterrupt;
            //
            openPort();
            // Main Loop
            while (true)
            {
                if (serialPortFailure)
                {
                    // Flag serial port failure blinking green leed
                    System.Threading.Thread.Sleep(50);
                    Green.Write(true);
                    System.Threading.Thread.Sleep(50);
                    Green.Write(false);
                }
                else
                {
                    reset = pushBtn.Read();
                    System.Threading.Thread.Sleep(2000);
                    if (pushBtn.Read() && reset)
                    {
                        hold = true;
                        pushBtn.OnInterrupt -= pushBtn_OnInterrupt;
                        Red.Write(true);
                        Orange.Write(true);
                        Blue.Write(true);
                        Thread.Sleep(10000);
                        pushBtn.OnInterrupt += pushBtn_OnInterrupt;
                        hold = false;
                    }
                }
            }
        }

        static OutputPort RTS;
        static InputPort  CTS;
        static bool serialPortFailure = false;
        private static void openPort()
        {

            try
            {
                serialPort1 = new SerialPort("COM3", (int)BaudRate.Baudrate115200);
                serialPort1.Open();
                serialPort1.DataReceived += serialPort1_DataReceived;

                serialPort2 = new SerialPort("COM4", (int)BaudRate.Baudrate115200);
                serialPort2.Open();
                serialPort2.DataReceived += serialPort2_DataReceived;

                serialPort3 = new SerialPort("COM5", (int)BaudRate.Baudrate115200);
                serialPort3.Open();
                serialPort3.DataReceived += serialPort3_DataReceived;
                //
                Cpu.Pin cts;
                Cpu.Pin rts;
                Cpu.Pin filler;
                HardwareProvider.HwProvider.GetSerialPins("COM3", out filler, out filler, out cts, out rts);
                Green = new OutputPort(rts, true);
                CTS = new InputPort(cts, false, Port.ResistorMode.Disabled);
            }
            catch (Exception ex)
            {
                serialPortFailure = true; 
                Debug.Print(ex.Message);
            }
        }

        static void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Green.Write(true);          
            Thread.Sleep(50);            
            try
            {
                byte[] buffer = new byte[serialPort1.BytesToRead];
                serialPort1.Read(buffer, 0, buffer.Length);
                Debug.Print(serialPort1.PortName + ": " + convertToString(buffer));     
            }
            catch (Exception)
            {
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
            }
            Green.Write(false);
        }
        static void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Green.Write(true);
            Thread.Sleep(50);
            try
            {
                byte[] buffer = new byte[serialPort2.BytesToRead];
                serialPort2.Read(buffer, 0, buffer.Length);
                // string message = "";
                // foreach (char c in buffer)
                // {
                //     message += c.ToString();
                // }
                // Debug.Print(serialPort2.PortName + " Data:" + message);
                Debug.Print(serialPort2.PortName + ": " + convertToString(buffer));     
            }
            catch (Exception)
            {
                serialPort2.DiscardInBuffer();
                serialPort2.DiscardOutBuffer();
            }
            Green.Write(false);
        }
        static void serialPort3_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Blue.Write(true);
            Thread.Sleep(50);
            try
            {
                byte[] buffer = new byte[serialPort3.BytesToRead];
                serialPort3.Read(buffer, 0, buffer.Length);
                Debug.Print(serialPort3.PortName + ": " + convertToString(buffer));
            }
            catch (Exception)
            {
                serialPort3.DiscardInBuffer();
                serialPort3.DiscardOutBuffer();
            }
            Blue.Write(false);
        }

        static string readFromSerialPort(SerialPort port)
        {
            Green.Write(true);
            Thread.Sleep(50);
            try
            {
                byte[] buffer = new byte[port.BytesToRead];
                port.Read(buffer, 0, buffer.Length);
                Green.Write(false);
                return port.PortName + ":" + convertToString(buffer);
            }
            catch (Exception)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                throw new Exception("this port cannot be read");
            }

        }

        static string convertToString(byte[] buffer)
        {
            string message = "";
            foreach (char c in buffer)
            {
                message += c.ToString();
            }
            return message;
        }

        static bool hold        { get; set; }
        static int  inside      { get; set; }
        static void pushBtn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            pushBtn.DisableInterrupt();
            // hold = (data2 == 1) ? true : false;
            inside += 1;
            hold    = !hold;
            pushBtn.EnableInterrupt();
        }

        static void orangeTask(object state)
        {
            if (hold) return;
            Orange.Write(!Orange.Read());
        }

        static void redTask(object state)
        {
            if (hold) return;
            Red.Write(!Red.Read());
        }

        static int firstOrSecond;
        static void blueTask(object state)
        {
            if (!hold || reset) { Blue.Write(!Blue.Read()); return; }
            // Blue.Write(!Blue.Read());
            firstOrSecond += 1;
            switch (firstOrSecond)
            {
                case 1:
                    Red.Write(true);
                    serialPort1.Write(buffer, 0, buffer.Length);
                    Red.Write(false);
                    break;
                case 2:
                    Orange.Write(true);
                    serialPort2.Write(buffer, 0, buffer.Length);
                    Orange.Write(false);
                    break;
                case 3:
                    Blue.Write(true);
                    serialPort3.Write(buffer, 0, buffer.Length);
                    Blue.Write(false);
                    firstOrSecond = 0;
                    break;
                default:
                    firstOrSecond = 0;
                    break;
            }
        }
    }
    /// <summary>
    /// Define ports
    /// </summary>
    static class STM32F4_Port
    { 
        public const int A = 0 ;
        public const int B = 16;
        public const int C = 32;
        public const int D = 48;
        public const int E = 64;
    }
    /// <summary>
    /// Define General Pourpose pin out
    /// </summary>
    static class GP
    {
        public const int IO0 = 0;
        public const int IO1 = 1;
        public const int IO2 = 2;
        public const int IO3 = 3;
        public const int IO4 = 4;
        public const int IO5 = 5;
        public const int IO6 = 6;
        public const int IO7 = 7;
        public const int IO8 = 8;
        public const int IO9 = 9;
        public const int IO10= 10;
        public const int IO11= 11;
        public const int IO12= 12;
        public const int IO13= 13;
        public const int IO14= 14;
        public const int IO15= 15;
    }
}


#region code Garbage
// byte[] buffer = Encoding.UTF8.GetBytes("@" + inside.ToString() + "RD0400001053*\n");                   
// if (hold)
// {
//     serialPort1.Write(buffer, 0, buffer.Length);
// }
// else
// {
//     serialPort2.Write(buffer, 0, buffer.Length);
// }
// Thread.Sleep(Timeout.Infinite);
#endregion