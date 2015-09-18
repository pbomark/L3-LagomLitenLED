using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LowLevelHooks.Keyboard;

namespace L3KeyboardSender
{
    public partial class Form1 : Form
    {
        private readonly KeyboardHook kHook;
        public Form1()
        {
            InitializeComponent();
            kHook = new KeyboardHook();
            kHook.KeyEvent += KHookKeyEvent;
            FormClosing += Form1FormClosing;
            kHook.Hook(); // dispose() unhooks
        }
        void Form1FormClosing(object sender, FormClosingEventArgs e)
        {
            kHook.Dispose();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        void KHookKeyEvent(object sender, KeyboardHookEventArgs e)
        {
           HandleKey(e);
        }

        private void HandleKey(KeyboardHookEventArgs e)
        {
            if (e.KeyboardEventName == KeyboardEventNames.KeyUp || e.KeyboardEventName == KeyboardEventNames.KeyDown)
            {
                using (var client = new MailslotClient("LagomLitenLedMailSlot"))
                {
                    // change so it tries to send, if server is down it will fail as it is now
                    client.SendMessage( ((WindowsVirtualKey)e.VirtualKeyCode).ToString() + " " + e.KeyboardEventName.ToString() );
                }
            }

            // KeyPressed is not interesting
           /* if (e.Char == '\0')
            {
                //KeyboardWriter.Write(e.KeyString);
            }
            else
            {
                if (e.KeyboardEventName == KeyboardEventNames.KeyUp)
                {
                }
            }*/
        }
    }
}
