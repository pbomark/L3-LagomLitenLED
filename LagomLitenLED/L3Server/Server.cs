using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Xml;
using L3;
using Ini;

namespace L3Server
{
    public struct KeyFrame
    {
        public int duration;
        public byte[] intensity;

        public KeyFrame(int dur, string intensityCSV, string stateCSV)
        {
            duration = dur;
            string[] intensityArray = intensityCSV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] valueArray = stateCSV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            intensity = new byte[intensityArray.Length];

            double scale = 255.0 / 48.0; // values are between 0 and 48, we want 0 to 255.
            for (int i = 0; i < intensityArray.Length; i++)
            {
                intensity[i] = byte.Parse(valueArray[i]) == (byte)1 ? (byte)Math.Min((int)Math.Round(byte.Parse(intensityArray[i]) * scale), 255) : (byte)0; // cap at 255
            }
        }
    }
    class Server
    {
        static LagomLitenLed trinket = new LagomLitenLed();
        static int numberOfDiodes = trinket.getNumberOfDiodes();
        static bool stop = false;
        static public void setDiodeColorBuffers(IniReader tagFile, string section, IniReader buttonFile, IniReader colorFile)
        {
            foreach (string key in tagFile.GetKeys(section))
            {
                string[] diodes = buttonFile.GetValue(key, "Diodes").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string diode in diodes)
                {
                    int index = int.Parse(diode);
                    string[] rgb = colorFile.GetValue(tagFile.GetValue(key, section), "Colors").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    trinket.setRed(index, byte.Parse(rgb[0]));
                    trinket.setGreen(index, byte.Parse(rgb[1]));
                    trinket.setBlue(index, byte.Parse(rgb[2]));
                }
            }
        }
        static void Main(string[] args)
        {
            // handle closing of the window correctly (see #region cleanup)
            HandlerRoutine hr = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(hr, true); 

            using (var server = new MailslotServer("LagomLitenLedMailSlot"))
            {
                try
                {
                    trinket.open();
                    while (!stop)
                    {
                        var msg = server.GetNextMessage();
                        if (msg != null) // incoming message
                        {
                            var arguments = msg.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            if (arguments.Length == 1) // one argument means either a game file or an animation
                            {
                                if (arguments[0].Equals("stop")) // time to end this
                                {
                                    stop = true;
                                }
                                else if (arguments[0].EndsWith(".lwax", true, null)) // animation, ignore case, default culture
                                {
                                    XmlDocument anim = new XmlDocument();
                                    anim.Load(arguments[0]);

                                    XmlNodeList intensityList, stateList;
                                    intensityList = anim.SelectNodes(".//Intensity[@LedHwType='6']"); // there are more premade animations for LEDWiz

                                    stateList = anim.SelectNodes(".//State[@LedHwType='6']");

                                    KeyFrame[] animation = new KeyFrame[stateList.Count];

                                    String currentIntensity = new String("".ToCharArray());
                                    String currentState = new String("".ToCharArray());
                                    for (int i = 0; i < stateList.Count; i++)
                                    {
                                        XmlElement frameparent = (XmlElement)stateList[i].ParentNode;
                                        // find out current intensity setting
                                        for (int j = 0; j < intensityList.Count; j++) // index from one, because thats what the animation files do
                                        {
                                            XmlElement parent = (XmlElement)intensityList[j].ParentNode;
                                            if (parent.GetAttribute("Number") == frameparent.GetAttribute("Number"))
                                            {
                                                XmlElement intensityElement = (XmlElement)intensityList[j];
                                                currentIntensity = intensityElement.GetAttribute("Value");
                                            }
                                        }
                                        XmlElement stateElement = (XmlElement)stateList[i];
                                        currentState = stateElement.GetAttribute("Value");
                                        // build frame
                                        animation[i] = new KeyFrame(int.Parse(frameparent.GetAttribute("Duration")), currentIntensity, currentState);
                                    }


                                    foreach (KeyFrame frame in animation)
                                    {
                                        for (int i = 0; i < numberOfDiodes; i++)
                                        {
                                            trinket.setRed(i, frame.intensity[i * 3]);
                                            trinket.setGreen(i, frame.intensity[i * 3 + 1]);
                                            trinket.setBlue(i, frame.intensity[i * 3 + 2]);
                                        }
                                        Thread.Sleep(frame.duration);
                                        //trinket.update();
                                        Console.WriteLine(trinket.update().ToString());
                                    }
                                }
                                else //gamefile
                                {
                                    // Most games don't use all buttons, so lets paint it black
                                    trinket.setToBlack();

                                    // read game ini file
                                    IniReader games = new IniReader("../../data/Colors.ini");

                                    // supersede allows defining new games or redefining games already in colors.ini to new colors.
                                    IniReader supersede = new IniReader("../../data/supersede.ini");

                                    // buttons.ini define which diode goes to which button
                                    IniReader buttons = new IniReader("../../data/Buttons.ini");

                                    // colordefinitions.ini have name definitions for different colors.
                                    IniReader colors = new IniReader("../../data/ColorDefinitions.ini");

                                    // check if the argument matches any of the game files 
                                    if (games.ContainsSection(arguments[0]) && !supersede.ContainsSection(arguments[0])) // allow supersede to override the colors.ini settings.
                                    {
                                        setDiodeColorBuffers(games, arguments[0], buttons, colors);
                                    }
                                    else if (supersede.ContainsSection(arguments[0]))
                                    {
                                        setDiodeColorBuffers(supersede, arguments[0], buttons, colors);
                                    }
                                    else
                                    {
                                        Console.Write("Game file " + arguments[0] + " not found in colors.ini nor in supersede.ini");
                                    }
                                    // set system button colors
                                    setDiodeColorBuffers(buttons, "SystemButtonColors", buttons, colors);
                                    Console.Write(trinket.update().ToString());
                                }
                            }
                            else if (arguments.Length == numberOfDiodes) // set colors directly
                            {
                                for (int i = 0; i < numberOfDiodes; i++)
                                {
                                    String trimmed = arguments[i];
                                    if (arguments[i].Substring(0, 2) == "0x")
                                        trimmed = arguments[i].Substring(2, 6);
                                    trinket.setRed(i, byte.Parse(trimmed.Substring(0, 2), System.Globalization.NumberStyles.HexNumber));
                                    trinket.setGreen(i, byte.Parse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber));
                                    trinket.setBlue(i, byte.Parse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                                }
                                Console.Write(trinket.update().ToString());
                            }
                        }
                        Thread.Sleep(1);
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
                    trinket.close();
                }
            }
        }
        #region Cleanup
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine 
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            stop = true;
            return true;
        }
        #endregion
    }
}
