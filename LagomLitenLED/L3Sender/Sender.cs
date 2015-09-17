using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L3Sender
{
    class Sender
    {
        static void Main(string[] args)
        {
            using (var client = new MailslotClient("LagomLitenLedMailSlot"))
            {
                try
                {  
                    if (args.Length != 0)
                    {
                        client.SendMessage(String.Join(",",args));
                    }
                    else // no arguments
                    {
                        throw new Exception("Nothing to do.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.Message);
                    Console.ReadKey();
                }
                finally
                {

                }
            }
        }
    }
}
