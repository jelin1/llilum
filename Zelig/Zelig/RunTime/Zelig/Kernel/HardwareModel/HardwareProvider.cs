﻿//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Zelig.Runtime
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using Windows.Devices.Spi.Provider;
    using Windows.Devices.Gpio.Provider;
    using Windows.Devices.I2c.Provider;

    //--//

    [ImplicitInstance]
    [ForceDevirtualization]
    public abstract class HardwareProvider
    {
        private static object pinLock = new object();

        //
        // See below in default ctor: the singleton factory currently does not yet support creating parameterized objects
        // we need to use a ceiling value until we invoke the defautl ctor that will ask the actual number of pins to the 
        // concrete HW Provider (e.g. mBed HW provider)
        // Consider that LPC1768 board has 32 exposed pins and K64F has 64 pins only
        //
        static BitArray m_reservedPins = new BitArray( 256 );
        
        //--//

        //protected HardwareProvider( )
        //{
        //    // BUGBUGBUG: fix singleton factory to call the default ctor when available
        //    m_used = new BitArray( this.PinCount );
        //}
        
        //
        // Spi discovery 
        //

        public abstract int GetSpiChannelIndexFromString( string busId );

        public abstract bool GetSpiPinsFromBusId( int id, out int mosi, out int miso, out int sclk, out int chipSelect );

        public abstract bool GetSpiChannelInfo( int id, out int csLineCount, out int maxFreq, out int minFreq, out bool supports16 );

        public abstract bool GetSpiChannelTimingInfo(int id, out int setupTime, out int holdTime);

        public abstract bool GetSpiChannelActiveLow(int id, out bool activeLow);

        public abstract string[] GetSpiChannels();

        //
        // Spi creation
        //

        public abstract SpiChannel CreateSpiChannel( );

        //
        // I2C Discovery
        //

        public abstract string[] GetI2CChannels();

        public abstract int GetI2cChannelIndexFromString(string busId);

        public abstract bool GetI2CPinsFromChannelIndex(int index, out int sdaPin, out int sclPin);

        //
        // I2C Creation
        //

        public abstract I2cChannel CreateI2cChannel();

        //
        // Gpio discovery and reservation service
        //

        /// <summary>
        /// Returns how many pins on the board are accessible to the user.  This 
        /// includes also pins tied to peripherals, such as SPI, etc.
        /// </summary>
        public abstract int PinCount
        {
            get;
        }

        /// <summary>
        /// Returns the sequential pin index used by the reservation service from the pin number provides by the 
        /// OEM, usually through the OEM specific board provider assembly
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        public abstract int PinToIndex( int pin ); 

        public abstract bool IsPinNC(int pin);
        
        //
        // Gpio reservation
        //

        internal bool TryReservePins(params int[] pins)
        {
            int failIndex = -1;
            
            lock (pinLock)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    // Do not try to release NC pins
                    if(!IsPinNC(pins[i]))
                    {
                        int index = PinToIndex(pins[i]);
                        if (m_reservedPins[index] == true)
                        {
                            failIndex = i;
                            break;
                        }
                        m_reservedPins[index] = true;
                    }
                }

                if (failIndex > 0)
                {
                    for (int i = 0; i < failIndex; i++)
                    {
                        // Do not touch NC pins
                        if (!IsPinNC(pins[i]))
                        {
                            int index = PinToIndex(pins[i]);
                            m_reservedPins[index] = false;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        internal void ReleasePins(params int[] pins)
        {
            lock (pinLock)
            {
                foreach (int pin in pins)
                {
                    // Don't touch NC pins
                    if (!IsPinNC(pin))
                    {
                        int index = PinToIndex(pin);

                        if (m_reservedPins[index] == true)
                        {
                            m_reservedPins[index] = false;
                        }
                    }
                }
            }
        }

        //
        // Gpio creation
        //

        public abstract GpioPinProvider CreateGpioPin();

        //
        // Factory methods
        //

        public static extern HardwareProvider Instance
        {
            [SingletonFactory()]
            [MethodImpl( MethodImplOptions.InternalCall )]
            get;
        }
    }
}