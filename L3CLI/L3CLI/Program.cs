
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using System.Xml;

using LibUsbDotNet;
using LibUsbDotNet.Main;

using Ini;
using L3;

namespace L3
{
    public struct KeyFrame
    {
        public int duration;
        public byte[] intensity;

        public KeyFrame(int dur, string intensityCSV, string stateCSV)
        {
            duration = dur;
            string[] intensityArray = intensityCSV.Split(",".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
            string[] valueArray = stateCSV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            intensity = new byte[intensityArray.Length];

            double scale = 255.0 /48.0; // values are between 0 and 48, we want 0 to 255.
            for(int i = 0; i <intensityArray.Length; i++)
            {
                intensity[i] = byte.Parse(valueArray[i])==(byte)1?(byte)Math.Min((int)Math.Round(byte.Parse(intensityArray[i])*scale),255):(byte)0; // cap at 255
            }
        }
    }

    internal class L3Cli
    {
        static LagomLitenLed trinket = new LagomLitenLed();
        static int numberOfDiodes = trinket.getNumberOfDiodes();
        
        public static void Main(string[] args)
        {
            ErrorCode ec = ErrorCode.None;
            try
            {
                trinket.open();

                if (args.Length != 0)
                {
                    // open usb communications

                    if (args.Length == numberOfDiodes) 
                    {
                        for (int i = 0; i < numberOfDiodes; i++)
                        {
                            String trimmed = args[i];
                            if (args[i].Substring(0, 2) == "0x")
                                trimmed = args[i].Substring(2, 6);
                            trinket.setRed(i, byte.Parse(trimmed.Substring(0, 2), System.Globalization.NumberStyles.HexNumber));
                            trinket.setGreen(i, byte.Parse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber));
                            trinket.setBlue(i, byte.Parse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                        }
                        Console.Write(trinket.update().ToString());
                    }
                    else if (args.Length == 1) // one argument means either a game file or an animation
                    {
                        if (args[0].EndsWith(".lwax", true, null)) // animation, ignore case, default culture
                        {
                            XmlDocument anim = new XmlDocument();
                            anim.Load(args[0]);

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
                                   if(parent.GetAttribute("Number")==frameparent.GetAttribute("Number"))
                                   {
                                       XmlElement intensityElement = (XmlElement)intensityList[j];
                                       currentIntensity = intensityElement.GetAttribute("Value");
                                   }
                                }
                                XmlElement stateElement = (XmlElement)stateList[i];
                                currentState = stateElement.GetAttribute("Value");
                                // build frame
                                animation[i] = new KeyFrame(int.Parse(frameparent.GetAttribute("Duration")), currentIntensity,currentState);
                             }


                            foreach (KeyFrame frame in animation)
                            {
                                for (int i = 0; i < numberOfDiodes; i++)
                                {
                                    trinket.setRed(i,frame.intensity[i*3]);
                                    trinket.setGreen(i,frame.intensity[i*3 + 1]);
                                    trinket.setBlue(i, frame.intensity[i*3 + 2]);
                                }
                                Thread.Sleep(frame.duration);
                                //trinket.update();
                                Console.WriteLine(trinket.update().ToString());
                            }

                            Console.ReadKey();
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
                            if (games.ContainsSection(args[0]) && !supersede.ContainsSection(args[0])) // allow supersede to override the colors.ini settings.
                            {
                                setDiodeColorBuffers(games, args[0], buttons, colors);
                            }
                            else if (supersede.ContainsSection(args[0]))
                            {
                                setDiodeColorBuffers(supersede, args[0], buttons, colors);
                            }
                            else
                            {
                                throw new Exception("Game file " + args[0] + " not found in colors.ini nor in supersede.ini");
                            }
                            // set system button colors
                            setDiodeColorBuffers(buttons, "SystemButtonColors", buttons, colors);
                            Console.Write(trinket.update().ToString());
                        }
                     
                    }
                    else
                    {
                        throw new Exception("Need exactly 28 arguments in hex (0xffffff) format or 1 game file argument");
                    }

                   
                }
                else
                {
                    throw new Exception("Usage: " + Environment.NewLine +
                                        "   L3CLI 0xffffff 0xffffff 0xffffff 0xffffff ..." + Environment.NewLine +
                                        "       - Set all 28 diode colors using hex values, 0xRRGGBB" + Environment.NewLine +
                                        "   L3CLI [gametag]" + Environment.NewLine +
                                        "       - Set diode colors according to the colors.ini for that game tag. (diode to button mapping in buttons.ini)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
            }
            finally
            {
                // close down usb connection
                trinket.close();
            }
        }
 
        static public void setDiodeColorBuffers(IniReader tagFile, string section, IniReader buttonFile, IniReader colorFile)
        {
            foreach (String key in tagFile.GetKeys(section))
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
       
    }
}