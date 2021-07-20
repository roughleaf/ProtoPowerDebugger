using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using ProtoPowerDebugger.Models;
using ProtoPowerDebugger.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoPowerDebugger.ViewModels
{
    public class AdcDebugViewModel : ViewModelBase, IObserver<RawAdcData>
    {
        SerialDataService serialDataService = new SerialDataService();

        private string portOpenCloseText = "Open";
        public string PortOpenCloseText
        {
            get { return portOpenCloseText; }
            private set => this.RaiseAndSetIfChanged(ref portOpenCloseText, value);
        }

        private bool commsStarted = false;
        public bool CommsStarted
        {
            get { return commsStarted; }
            private set => this.RaiseAndSetIfChanged(ref commsStarted, value);
        }

        private bool primaryOutputEnabled = false;
        public bool PrimaryOutputEnabled
        {
            get { return primaryOutputEnabled; }
            private set => this.RaiseAndSetIfChanged(ref primaryOutputEnabled, value);
        }

        private bool auxOutputEnabled = false;
        public bool AuxOutputEnabled
        {
            get { return auxOutputEnabled; }
            private set => this.RaiseAndSetIfChanged(ref auxOutputEnabled, value);
        }

        private int portNum;
        public int PortNum
        {
            get { return portNum; }
            set { portNum = value; }
        }

        private string[]? serialPortList;
        public string[]? SerialPortList
        {
            get { return serialPortList; }
            private set => this.RaiseAndSetIfChanged(ref serialPortList, value);
        }

        private RawAdcData? adcData;
        public RawAdcData? AdcData
        {
            get { return adcData; }
            private set => this.RaiseAndSetIfChanged(ref adcData, value);
        }

        public AdcDebugViewModel()
        {
            SerialPortList = SerialPort.GetPortNames();
            AdcData = new RawAdcData();
            serialDataService.Subscribe(this);
        }

        public void OnCompleted()
        {
            // Do Nothing
        }

        public void OnError(Exception error)
        {
            // Do Nothing
        }

        public void OnNext(RawAdcData value)
        {
            AdcData = new RawAdcData
            {
                PrimaryVolt = value.PrimaryVolt,
                PrimaryMicroAmp = value.PrimaryMicroAmp,
                AuxMicroAmp = value.AuxMicroAmp,
                AuxVolt = value.AuxVolt
            };
        }

        public void SerialStartStop()
        {
            if (!CommsStarted)
            {
                //serialComms.ReadRawDataStop();
                serialDataService.Stop();
                serialDataService.Close();
                PortOpenCloseText = "Open";
            }
            else
            {
                //serialComms.ReadRawDataStart(SerialPortList[PortNum]);
                if (SerialPortList?[PortNum] != null)
                {
                    if (serialDataService.Open(SerialPortList[PortNum]) >= 0)
                    {
                        serialDataService.Start();
                        PortOpenCloseText = "Close";
                    }
                    else
                    {
                        string errorMessage = "Could not open " + SerialPortList[PortNum].ToString();

                        var msBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                        {
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentTitle = "Error",
                            ContentMessage = errorMessage,
                            Icon = Icon.Error,
                            Style = Style.None
                        });
                        msBoxStandardWindow.Show();

                        CommsStarted = false;
                    }
                }
            }
        }
    }
}
