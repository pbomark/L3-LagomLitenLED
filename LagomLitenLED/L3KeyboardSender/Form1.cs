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
using System.Xml;

namespace L3KeyboardSender
{
    public partial class Form1 : Form
    {
        private readonly KeyboardHook kHook;
        string[] KeyCodes;
        public Form1()
        {
            InitializeComponent();
            kHook = new KeyboardHook();
            kHook.KeyEvent += KHookKeyEvent;
            FormClosing += Form1FormClosing;
            kHook.Hook(); // dispose() unhooks

            // Keycode lists:
            //TODO: add error handling
            XmlDocument inputmap = new XmlDocument();
            inputmap.Load("../../../configuration/LEDBlinkyInputMap.xml");

            XmlNodeList keycodeList;
            keycodeList = inputmap.SelectNodes(".//port[@type='R']"); // Grabbing the red ports to get a count
            KeyCodes = new String[keycodeList.Count];

            int j = 1;
            for (int i = 0; i < keycodeList.Count; i++)
            {
                XmlElement keycode = (XmlElement)inputmap.SelectSingleNode(".//port[@number='" + j + "']");
                j = j + 3;

                //String is a bit wrong, fix it here to avoid another loop
                String kc_string = keycode.GetAttribute("inputCodes").Substring(8); // trim "KEYCODE_"
                // check if length is 1, if so, add "K_" to the start, else add "VK_" to match virtual keycodes enum.
                if (kc_string.Length == 1)
                {
                    kc_string = "K_" + kc_string;
                }
                else
                {
                    kc_string = "VK_" + kc_string;
                }

                // fix alt keys (called VK_LMENU and VK_RMENU in windows virtual keycodes)
                if (kc_string == "VK_LALT" || kc_string == "VK_RALT")
                {
                    kc_string = kc_string.Substring(0, 4) + "MENU";
                }
                // fix escape key
                if (kc_string == "VK_ESC")
                {
                    kc_string = "VK_ESCAPE";
                }
                // expect more fixes here

                // assign
                KeyCodes[i] = kc_string;
            }
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
                // check if the key is mapped ( LEDBlinkyInputMap.xml is parsed into KeyCodes[] already )
               int index = Array.IndexOf(KeyCodes, ((WindowsVirtualKey)e.VirtualKeyCode).ToString());
               if ( index >= 0)
               {
                   using (var client = new MailslotClient("LagomLitenLedMailSlot"))
                   {
                       // change so it tries to send, if server is down it will fail as it is now
                       client.SendMessage(index + "," + e.KeyboardEventName.ToString() );
                   }
               }
            }
        }
    }
}
