# gba-usb-multiboot

This is a port of [this arduino-based multiboot project](https://github.com/jojolebarjos/gba-multiboot) to the cheap and ubiquitous RP2040-mini controller

## Hardware used

* [A GBA link cable](https://s.click.aliexpress.com/e/_c4c6UNrx)
* [A RP2040-Zero](https://s.click.aliexpress.com/e/_c4Tl1wnn)

## Build instructions

First, build and flash the firmware with PlatformIO to the board.

Wire up the cable based on the original repo linked above:

| Cable side pin | RP2040-Zero pin | Signal         |
| --- | --- | --- |
| 2 | 4 | SO -> SPI0 RX |
| 3 | 3 | SI -> SPI0 TX |
| 5 | 5 | CLK -> SPI0 CS |
| 4 | GND | GND |

My version of the adapter will output "Ready" on boot, make sure your software knows how to deal with this.

## Usage

Use the `upload.py` from the original repo, or use my vibe coded mess of a GUI tool.
