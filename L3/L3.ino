
#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
  #include <avr/power.h>
#endif
#define LED_OUTPUT_PIN 1
#define NUMBER_OF_LEDS 28

uint8_t diodes[NUMBER_OF_LEDS*3];
bool doRefresh;

#include "LagomLitenUSB.h"

Adafruit_NeoPixel strip = Adafruit_NeoPixel(NUMBER_OF_LEDS, LED_OUTPUT_PIN, NEO_RGB + NEO_KHZ800);
uint8_t *pixels = strip.getPixels();
#ifdef __cplusplus
extern "C" {
#endif
static uchar replyBuffer[4];
 
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
    doRefresh = true;
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
    //return USB_NO_MSG;
  }else if(data[1] == 3){ // refresh all diodes
    doRefresh = true;
    //echo:
    len = 4;
    replyBuffer[0] = data[2];
    replyBuffer[1] = data[3];
    replyBuffer[2] = data[4];
    replyBuffer[3] = data[5];
    //return USB_NO_MSG;
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

void setup()
{
  doRefresh=false;
  usbBegin();
  strip.begin();
  strip.show(); // Initialize all pixels to 'off'
}
void loop()
{
  usbPoll();
  if (doRefresh)
  {
    for(int i=0;i<NUMBER_OF_LEDS*3;i++)
    {
      pixels[i] = diodes[i];
    }
    doRefresh=false;
    strip.show();
  }
}
