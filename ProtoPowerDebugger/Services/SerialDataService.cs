using DynamicData;
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
        readonly int bufferLength = 0;

        static List<IObserver<RawAdcData>>? observers;

        public SerialDataService(int size)
        {
            observers = new List<IObserver<RawAdcData>>();
            bufferLength = size;
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
            {
                observers.Add(observer);
            }

            return new Unsubscriber(observers, observer);
        }

        readonly RawAdcData rawAdcData = new();

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
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

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
    // 1. Impliment try/catch to the ReadLine function and send exception error to observer.OnError
        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
                
            while (sp.BytesToRead > 0 && readEnabled)       // The entire string needs to be read in a single itterartion of this loop            {
            {
                try
                {
                    byte[] bytes = new byte[9];
                    int bytesToRead = 8;

                    int bytesRead = sp.Read(bytes, 0, 1);

                    if ((bytesRead == 1) && (bytes[0] == '<'))
                    {
                        do
                        {
                            bytesRead += sp.Read(bytes, bytesRead, bytesToRead);
                            bytesToRead -= bytesRead;
                        } while (bytesToRead > 0);


                        rawAdcData.AuxVolt = BitConverter.ToUInt16(bytes, 1);
                        rawAdcData.AuxMicroAmp = BitConverter.ToUInt16(bytes, 3);
                        rawAdcData.PrimaryVolt = BitConverter.ToUInt16(bytes, 5);
                        rawAdcData.PrimaryMicroAmp = BitConverter.ToUInt16(bytes, 7);
                        if (observers != null)
                        {
                            foreach (var observer in observers)
                                observer.OnNext(rawAdcData);
                        }
                    }
                }
                catch (Exception err)
                {
                    if (observers != null)
                    {
                        foreach (var observer in observers)
                            observer.OnError(err);
                    }
                    break;
                }
            }
        }
    }
}
    