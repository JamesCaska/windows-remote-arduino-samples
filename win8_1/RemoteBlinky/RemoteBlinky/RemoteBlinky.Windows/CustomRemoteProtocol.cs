using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maker.Serial;
namespace RemoteBlinky
{
    public class CustomRemoteProtocol
    {
        IStream connection;
        public CustomRemoteProtocol(IStream connection)
        {
            this.connection = connection;
        }

        public bool LEDOn {
            set
            {
                if (value)
                {
                    connection.write((byte)'H');
                }
                else
                {
                    connection.write((byte)'L');
                }

               
            }
        }
 
    }   
  
}
