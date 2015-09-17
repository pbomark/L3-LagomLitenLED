#LagomLitenLED
Allows using a Trinket (from Adafruit) to control adressable diodes(e.g. neopixels) from a PC using USB.

Consists of four parts:
* L3, An arduino project for the Adafruit Trinket.
* LagomLitenUSB, an arduino library that L3 uses(more of a wrapper for V-USB).
* LagomLitenLed, a c# class that handles the PC side of communications.
* L3CLI, the host software which uses the LagomLitenLed class. The intended purpose is to control diodes in the buttons of an arcade cabinet control panel. Has functionality for parsing color definition files for MAME games, and defining custom color mappings.
