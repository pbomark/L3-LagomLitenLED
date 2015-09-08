
#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
  #include <avr/power.h>
#endif
#define LED_OUTPUT_PIN 1
#define NUMBER_OF_LEDS 29

uint8_t diodes[NUMBER_OF_LEDS*3];
bool needsRefresh;

#include "LagomLitenUSB.h"

Adafruit_NeoPixel strip = Adafruit_NeoPixel(NUMBER_OF_LEDS, LED_OUTPUT_PIN, NEO_RGB + NEO_KHZ800);

#ifdef __cplusplus
extern "C" {
#endif
static uchar replyBuffer[8];
 
uchar usbFunctionSetup(uchar data[8])
{
uchar len = 0;
  if(data[1] == 0){ // echo without doing anyting
    len = 4;
    replyBuffer[0] = data[2];
    replyBuffer[1] = data[3];
    replyBuffer[2] = data[4];
    replyBuffer[3] = data[5];
  }else if(data[1] == 1){ // set and refresh
    diodes[data[2]*3] = data[3];
    diodes[data[2]*3+1] = data[4];
    diodes[data[2]*3+2] = data[5];
    needsRefresh = true;
    // echo:
    len = 4;
    replyBuffer[0] = data[2];
    replyBuffer[1] = data[3];
    replyBuffer[2] = data[4];
    replyBuffer[3] = data[5];
  }else if(data[1] == 2){ // set without refreshing (use 3 to refresh)
    diodes[data[2]*3] = data[3]; //red
    diodes[data[2]*3+1] = data[4]; // green
    diodes[data[2]*3+2] = data[5]; // blue
    // echo:
    len = 4;
    replyBuffer[0] = data[2];
    replyBuffer[1] = data[3];
    replyBuffer[2] = data[4];
    replyBuffer[3] = data[5];
  }else if(data[1] == 3){ // refresh all diodes
    needsRefresh = true;
    //echo:
    len = 4;
    replyBuffer[0] = data[2];
    replyBuffer[1] = data[3];
    replyBuffer[2] = data[4];
    replyBuffer[3] = data[5];
  }
  usbMsgPtr = (int)replyBuffer;

    return len;
}
#ifdef __cplusplus
}
#endif
void usbBegin()
{
  cli();

  // run at full speed, because Trinket defaults to 8MHz for low voltage compatibility reasons
  clock_prescale_set(clock_div_1);

  // fake a disconnect to force the computer to re-enumerate
  PORTB &= ~(_BV(USB_CFG_DMINUS_BIT) | _BV(USB_CFG_DPLUS_BIT));
  usbDeviceDisconnect();
  _delay_ms(250);
  usbDeviceConnect();

  // start the USB driver
  usbInit();
  sei();
}

uint8_t * pixels = strip.getPixels();

void setup()
{
  needsRefresh=false;
  usbBegin();
  strip.begin();
  strip.show(); // Initialize all pixels to 'off'
}
void loop()
{
  usbPoll();
  if (needsRefresh)
  {
    for(int i=0;i<NUMBER_OF_LEDS*3;i++)
    {
      pixels[i] = diodes[i];
    }
    needsRefresh=false;
    strip.show();
  }
}
