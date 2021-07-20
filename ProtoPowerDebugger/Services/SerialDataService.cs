using ProtoPowerDebugger.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoPowerDebugger.Services
{
    class SerialDataService : IObservable<RawAdcData>
    {
        bool readEnabled = false;

        static List<IObserver<RawAdcData>>? observers;

        public SerialDataService()
        {
            observers = new List<IObserver<RawAdcData>>();
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<RawAdcData>> _observers;
            private IObserver<RawAdcData> _observer;

            public Unsubscriber(List<IObserver<RawAdcData>> observers, IObserver<RawAdcData> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }

        public IDisposable Subscribe(IObserver<RawAdcData> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber(observers, observer);
        }

        RawAdcData rawData = new();

        SerialPort? serialPort;

        public int Open(string serialPortName)
        {
            serialPort = new(serialPortName);

            serialPort.BaudRate = 230400;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = Handshake.None;
            serialPort.RtsEnable = false;

            //_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            serialPort.DataReceived += SerialDataReceived;

            try
            {
                serialPort.Open();
            }
            catch
            {
                return -1;
            }
            return 0;
        }

        public void Start()
        {
            readEnabled = true;
        }

        public void Stop()
        {
            readEnabled = false;
        }

        public void Write(string toWrite)
        {
            if (serialPort != null)
            {
                serialPort.Write(toWrite);
            }
        }

        public void Close()
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    readEnabled = false;
                    Thread.Sleep(10);   // Give pending read events time to trigger
                    serialPort.DiscardInBuffer();
                    while (serialPort.BytesToRead > 0 | serialPort.BytesToWrite > 0) { }     // Wait for any read/write operations to finish
                    serialPort.Close();
                }
            }
        }


    // TODO:
    // 1. Add start character to string
    // 2. Check string length when newline escape character is received. If less then required, add ASCII code 10 character to string and continue
    // 3. String length indicated complete, check for start and end character at start and end of received string to verify that a valid stream was received.
        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            while (sp.BytesToRead > 0 && readEnabled)
            {
                string indata = sp.ReadLine();
                //string indata = sp.ReadExisting();
                byte[] bytes = Encoding.ASCII.GetBytes(indata);

                if (indata.Length == 8)     // Discards data in edge case where new line escape character appears sooner than expected.
                {
                    rawData.AuxVolt = BitConverter.ToUInt16(bytes, 0);
                    rawData.AuxMicroAmp = BitConverter.ToUInt16(bytes, 2);
                    rawData.PrimaryVolt = BitConverter.ToUInt16(bytes, 4);
                    rawData.PrimaryMicroAmp = BitConverter.ToUInt16(bytes, 6);

                    if (observers != null)
                    {
                        foreach (var observer in observers)
                            observer.OnNext(rawData);
                    }
                }
            }
        }

    }
}
