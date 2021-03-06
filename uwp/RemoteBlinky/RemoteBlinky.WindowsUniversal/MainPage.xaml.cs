﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker.Serial;
using Microsoft.Maker.RemoteWiring;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteBlinky.WindowsUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //Usb is not supported on Win8.1. To see the USB connection steps, refer to the win10 solution instead.
        const bool USE_VBB_CONNECTION = true;
        const bool USE_CUSTOM_PROTOCOL = false;

        IStream connection;
        RemoteDevice arduino;
        CustomRemoteProtocol myRemoteProtocol;

        public MainPage()
        {
            this.InitializeComponent();


            if (USE_VBB_CONNECTION)
            {
                /*
                 * VirtualBreadboard Virtual Firmata device (www.virtualbreadboard.com)
                 * - is the server, so you must run VBB first
                 * - connection port 5000 as set in the VBB firmata port property
                 * - connects using the local IP address, 127.0.0.1
                 * - You must enable Private Networks ( Cient & Server ) in thePackage : Capabilities manifest
                 */
                LocalSerialSocket localSocket = new LocalSerialSocket(5000);
                if (USE_CUSTOM_PROTOCOL)
                {
                    myRemoteProtocol = new CustomRemoteProtocol(localSocket);
                }
                else
                {
                    arduino = new RemoteDevice(localSocket);
                }

                arduino.DeviceReady += OnConnectionEstablished;
 
                connection = localSocket;
            }
            else
            {
                /*
               * I've written my bluetooth device name as a parameter to the BluetoothSerial constructor. You should change this to your previously-paired
               * device name if using Bluetooth. You can also use the BluetoothSerial.listAvailableDevicesAsync() function to list
               * available devices, but that is not covered in this sample.
               */
                BluetoothSerial bluetooth = new BluetoothSerial("RNBT-E072");

                if (USE_CUSTOM_PROTOCOL)
                {
                    myRemoteProtocol = new CustomRemoteProtocol(bluetooth);
                }
                else
                {
                    arduino = new RemoteDevice(bluetooth);
                }

                bluetooth.ConnectionEstablished += OnConnectionEstablished;
                connection = bluetooth;
            }


            //these parameters don't matter for localSocket/bluetooth
            connection.begin(115200, SerialConfig.SERIAL_8N1);

        }

        private void OnConnectionEstablished()
        {
            //enable the buttons on the UI thread!
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                OnButton.IsEnabled = true;
                OffButton.IsEnabled = true;
                arduino.pinMode ( 5, PinMode.OUTPUT );

            }));
        }

        private void OnButton_Click(object sender, RoutedEventArgs e)
        {
            if (USE_CUSTOM_PROTOCOL)
            {
                myRemoteProtocol.LEDOn = true;
            }
            else
            {
                //turn the LED connected to pin 5 ON
                arduino.digitalWrite(5, PinState.HIGH);
            }

        }

        private void OffButton_Click(object sender, RoutedEventArgs e)
        {
            if (USE_CUSTOM_PROTOCOL)
            {
                myRemoteProtocol.LEDOn = false;
            }
            else
            {
                //turn the LED connected to pin 5 OFF
                arduino.digitalWrite(5, PinState.LOW);
            }

        }
    }
}
