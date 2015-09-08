#LagomLitenLED
Allows using a Trinket (from Adafruit) to control adressable diodes(e.g. neopixels) from a PC using USB.

Consists of four parts:
* L3, An arduino project for the Adafruit Trinket.
* LagomLitenUSB, an arduino library that L3 uses(more of a wrapper for V-USB).
* LagomLitenLed, a c# class that handles the PC side of communications.
* L3CLI, the host software which uses the LagomLitenLed class. The intended purpose is to control diodes in the buttons of an arcade cabinet control panel. Has functionality for parsing color definition files for MAME games, and defining custom color mappings.

Based on:
TrinketFakeUsbSerial, by Frank Zhao ( http://learn.adafruit.com/trinket-fake-usb-serial )
and
RemoteSensor ( https://www.obdev.at/products/vusb/remotesensor.html )

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
