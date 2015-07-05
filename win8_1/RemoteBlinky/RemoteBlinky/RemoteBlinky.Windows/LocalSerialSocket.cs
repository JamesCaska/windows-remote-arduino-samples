using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Microsoft.Maker.Serial;
using System.Runtime.InteropServices.WindowsRuntime;
namespace RemoteBlinky
{

    /**
       Implements the Firmata IStream with a local socket 
       Firmata requires synchronous so provides a synchronous buffer to asynchronous background socket threads

       https://github.com/ms-iot/remote-wiring

       Serial
       Serial is the transport layer, which provides the physical communication between applications and the Arduino device. IStream is the interface which defines the requirements of a communication stream between the Arduino and the application itself. Currently, this is implemented in the default library with the  BluetoothSerial  class as well as  USBSerial  for wired connections on Windows 10 (USB is not supported on Windows 8.1). There are five functions which need to be implemented should you choose to extend the capabilities of the library with other communication methods. These functions MUST be guaranteed to be synchronous operations in order to be consumed by the Firmata layer.
       • begin(int, SerialConfig)  ->  void  -- initializes the stream, the SerialConfig is important when using USB. Default for Arduino is SERIAL_8N1
       • end(void)  ->  void  -- finalizes the stream
       • available(void)  ->  int  -- gets the number of bytes in the stream
       • read(void)  ->  short  -- reads a single character from the incoming stream
       • write(char)  ->  void  -- writes a single character to the outgoing stream

       @author James Caska , www.virtualbreadboard.com
       */
    public class LocalSerialSocket : IStream
    {
        StreamSocket tcpClient;
        int port_;
        Queue<byte> inputBuffer;
        Queue<byte> outputBuffer;
        bool taskRunning;

        public event RemoteWiringConnectionCallback ConnectionEstablished;
        public event RemoteWiringConnectionCallback ConnectionFailed;
        public event RemoteWiringConnectionCallback ConnectionLost;

        public LocalSerialSocket(int port)
        {
            this.port_ = port;
            inputBuffer = new Queue<byte>();
            outputBuffer = new Queue<byte>();

        }

        private void RaiseConnectionFailed()
        {
            if (ConnectionFailed != null) ConnectionFailed();
        }

        private void RaiseConnectionEstablished()
        {
            if (ConnectionEstablished != null) ConnectionEstablished();
        }

        private void RaiseConnectionLost()
        {
            if (ConnectionLost != null && taskRunning)
            {
                RaiseConnectionLost();
            }
            taskRunning = false;
        }


        ushort IStream.available()
        {
            return (ushort)inputBuffer.Count;
        }

        void IStream.begin(uint baud_, SerialConfig config_)
        {

            Task.Factory.StartNew(Connect);

        }

        private async void Connect()
        {
            try
            {
                tcpClient = new Windows.Networking.Sockets.StreamSocket();
                await tcpClient.ConnectAsync(
                    new Windows.Networking.HostName("127.0.0.1"),
                    port_.ToString(),
                    Windows.Networking.Sockets.SocketProtectionLevel.PlainSocket);
            }
            catch (Exception e)
            {
                RaiseConnectionFailed();
                return;
            }

            RaiseConnectionEstablished();

            taskRunning = true;

            Task.Factory.StartNew(SendTask);

            Task.Factory.StartNew(ReceiveTask);

        }

        private async void SendTask()
        {
            try
            {
                while (taskRunning)
                {
                    if (outputBuffer.Count > 0)
                    {
                        byte[] txBuffer;
                        lock (outputBuffer)
                        {
                            txBuffer = outputBuffer.ToArray();
                            outputBuffer.Clear();
                        }

                        await tcpClient.OutputStream.WriteAsync(txBuffer.AsBuffer());

                    }
                }

            }
            catch (Exception e)
            {
                RaiseConnectionLost();
            }

        }

        private async void ReceiveTask()
        {

            IBuffer buffer = new byte[1024].AsBuffer();
            try
            {
                while (taskRunning)
                {
                    IBuffer bufferResult = await tcpClient.InputStream.ReadAsync(buffer, buffer.Length, Windows.Storage.Streams.InputStreamOptions.Partial);
                    if (bufferResult.Length > 0)
                    {
                        byte[] b = buffer.ToArray();
                        for (int i = 0, l = (int)bufferResult.Length; i < l; i++)
                        {
                            lock (inputBuffer)
                            {
                                inputBuffer.Enqueue(b[i]);
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                RaiseConnectionLost();
            }

        }


        void IStream.end()
        {
            taskRunning = false;
            tcpClient.Dispose();

        }

        ushort IStream.read()
        {
            lock (inputBuffer)
            {
                if (inputBuffer.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return inputBuffer.Dequeue();
                }
            }

        }

        uint IStream.write(byte c_)
        {
            lock (outputBuffer)
            {
                outputBuffer.Enqueue(c_);
            }
            return 0;
        }
    }

}
