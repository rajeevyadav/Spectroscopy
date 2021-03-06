﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spectroscopy_Controller
{
    // Methods for DDS. Designed for communication with an Arduino Mega 2560 to control an AD9910 DDS.
    class DDS
    {
        public static int CalculateFTW(int fo)
        {
            double fc = Math.Pow(10, 9); // clock frequency
            double FTW = fo * Math.Pow(2, 32) / fc; // calculate FTW
            FTW = Math.Round(FTW); // round to closest integer
            int FTWRounded = (int)FTW; // transforms the double into an int
            return FTWRounded;
        }

        public static int CalculatePOW(double phase)
        {
            double POW = phase * Math.Pow(2, 16) / 360;
            POW = Math.Round(POW);
            int POWRounded = (int)POW;
            return POWRounded;
        }

        public static string CalculateFTWBinary(int FTW)
        {
            string FTWBinary = Convert.ToString(FTW, 2); // converts a integer into a string of binary digits
            if (FTWBinary.Length < 32)
            {
                int diff = 32 - FTWBinary.Length;
                for (int i = 0; i < diff; i++)
                {
                    FTWBinary = 0 + FTWBinary;  // adds 0s if the lenght of the string is smaller than 32
                }
            }
            else
            { }

            return FTWBinary;
        }

        public static string Calculate16Binary(int value)
        {
            string binary = Convert.ToString(value, 2);
            if(binary.Length < 16)
            {
                int diff = 16 - binary.Length;
                for(int i = 0; i < diff; i++)
                {
                    binary = 0 + binary;
                }
            }
            else { }

            return binary;
        }

        static string CalculateByte(string binaryString, int start) // function to extract 8 characters out of a string of binary digits and convert them into a byte
        {
            int end = start + 8;

            string binaryStringByte = "";

            for (int i = start; i < end; i++)
            {
                binaryStringByte = binaryStringByte + Convert.ToString(binaryString[i]);
            }

            byte decimalByte = Convert.ToByte(binaryStringByte, 2);

            string FTWByteString = Convert.ToString(decimalByte);

            return FTWByteString;
        }

        public static void GetFTW(decimal value, out string FTWbyte0, out string FTWbyte1, out string FTWbyte2, out string FTWbyte3)
        {
            FTWbyte0 = "0";
            FTWbyte1 = "0";
            FTWbyte2 = "0";
            FTWbyte3 = "0";

            int FTW = CalculateFTW(Convert.ToInt32(value)); // calculates FTW
            string FTWBinary = CalculateFTWBinary(FTW); // converts in binary string

            FTWbyte0 = CalculateByte(FTWBinary, 0);
            FTWbyte1 = CalculateByte(FTWBinary, 8);
            FTWbyte2 = CalculateByte(FTWBinary, 16);
            FTWbyte3 = CalculateByte(FTWBinary, 24);
        }

        public static void GetASF(decimal value, decimal amp, out string ASFbyte0, out string ASFbyte1, bool normalisation) // This method is rather more complex than GetFTW because it includes normalistaions to maintain a constant output amplitude over the whole frequency range.
        {
           ASFbyte0 = "63"; // first byte of the ASF in decimal, full scale value
           ASFbyte1 = "255"; // second byte of the ASF in decimal, full scale value
           
           double frequency = Convert.ToDouble(value);
           double ampD = Convert.ToDouble(amp);
               
           if(frequency >= 100000000 && frequency <= 119999999) // checks the value of the frequency is in the first range. The different ranges correspond to different fitting regions.
           {

                // OLD BIT OF CODE //////////////////////////////////////////////////////////////////////////////////////////////////////////
                /*double rank = Math.Round(frequency / 100000) - 2000; // converts the frequency into the number of a line in the LUT
                int line = (int)rank;
                string[] amplitudeScaleFactor = System.IO.File.ReadAllLines(@"C:\Users\localadmin\Desktop\ASF_200_300MHz_33dBm.txt"); // open and read the LUT (text file)
                int ASF = Convert.ToInt32(amplitudeScaleFactor[line]); // converts the ASF into an int*/
                //int ASF = (int)Math.Round(194*0.826*Math.Pow(2,14)/(-8.59478*Math.Pow(10,-7)*Math.Pow(freqMHz,4) + 8.37290*Math.Pow(10,-4)*Math.Pow(freqMHz,3) - 0.302463*Math.Pow(freqMHz,2) + 47.6572*freqMHz - 2526.30)); // normalisation
                // END OF THE OLD BIT ///////////////////////////////////////////////////////////////////////////////////////////////////////

                double freqMHz = frequency / Math.Pow(10, 6);
                double RMS = 0.02 * Math.Pow(freqMHz, 2) - 4.5 * freqMHz + 595; // this is a fit of the RMS of the driver output as a function of frequency. Measured with ASF = 8181 and 30 dB attenuation on a Tektronix MSO3034 scope. Measurements done on 25/10/2016. See Vince's lab book for details.
                int ASF = (int)Math.Round(0.7 * 217 * Math.Pow(2, 14) / RMS); // Normalisation: 217 is in mV RMS the minimum measured on the 100 - 300 MHz range. The factor 0.7 is to avoid the saturation of the AOM.
                //ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                if (normalisation == true) // laser power normalisation so that x % on the GUI corresponds to x % of the max laser power
                {                          // The normalisations below correspond to fits of the ASF versus laser power. They can be seen on the excel file "New calibration 729 AOM driver".
                    if (ampD >= 5 && ampD <= 91.56) // Measurements were made at 178 MHz which is close to the carrier frequency to this date. The two different ranges correspond to different fitting regions.
                    {
                        ASF = (int)Math.Round(ASF * (8.78114279 * Math.Pow(10, -7) * Math.Pow(ampD, 3) - 1.323317 * Math.Pow(10, -4) * Math.Pow(ampD, 2) + 0.0126421389 * ampD + 0.0775117));
                    }
                    else if (ampD >= 91.57 && ampD <= 100)
                    {
                        ASF = (int)Math.Round(ASF * (4.6113 * Math.Pow(10, -4) * Math.Pow(ampD, 3) - 0.1305 * Math.Pow(ampD, 2) + 12.323 * ampD - 387.44));
                    }
                    else ASF = (int)Math.Round(ASF * amp / 100); // No normalistaion below 5%
                }
                else ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                string ASFBinary = Calculate16Binary(ASF); // converts ASF in binary string

                ASFbyte0 = CalculateByte(ASFBinary, 0);
                ASFbyte1 = CalculateByte(ASFBinary, 8);
            }
            else if(frequency >= 120000000 && frequency <= 179999999)
            {
                double freqMHz = frequency / Math.Pow(10, 6);
                double RMS = 2.3543 * Math.Pow(10, -4) * Math.Pow(freqMHz, 3) - 0.10083 * Math.Pow(freqMHz, 2) + 13.333 * freqMHz - 211.91;
                int ASF = (int)Math.Round(0.7 * 217 * Math.Pow(2, 14) / RMS);
                //ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                if (normalisation == true) // laser power normalisation so that x % on the GUI corresponds to x % of the max laser power
                {
                    if (ampD >= 5 && ampD <= 91.56)
                    {
                        ASF = (int)Math.Round(ASF * (8.78114279 * Math.Pow(10, -7) * Math.Pow(ampD, 3) - 1.323317 * Math.Pow(10, -4) * Math.Pow(ampD, 2) + 0.0126421389 * ampD + 0.0775117));
                    }
                    else if (ampD >= 91.57 && ampD <= 100)
                    {
                        ASF = (int)Math.Round(ASF * (4.6113 * Math.Pow(10, -4) * Math.Pow(ampD, 3) - 0.1305 * Math.Pow(ampD, 2) + 12.323 * ampD - 387.44));
                    }
                    else ASF = (int)Math.Round(ASF * amp / 100);
                }
                else ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                string ASFBinary = Calculate16Binary(ASF); // converts ASF in binary string

                ASFbyte0 = CalculateByte(ASFBinary, 0);
                ASFbyte1 = CalculateByte(ASFBinary, 8);
            }
            else if (frequency >= 180000000 && frequency <= 229999999)
            {
                double freqMHz = frequency / Math.Pow(10, 6);
                double RMS = 1.7716 * Math.Pow(10, -4) * Math.Pow(freqMHz, 3) - 0.10867 * Math.Pow(freqMHz, 2) + 21.385 * freqMHz - 1067.2;
                int ASF = (int)Math.Round(0.7 * 217 * Math.Pow(2, 14) / RMS);
                //ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                if (normalisation == true) // laser power normalisation so that x % on the GUI corresponds to x % of the max laser power
                {
                    if (ampD >= 5 && ampD <= 91.56)
                    {
                        ASF = (int)Math.Round(ASF * (8.78114279 * Math.Pow(10, -7) * Math.Pow(ampD, 3) - 1.323317 * Math.Pow(10, -4) * Math.Pow(ampD, 2) + 0.0126421389 * ampD + 0.0775117));
                    }
                    else if (ampD >= 91.57 && ampD <= 100)
                    {
                        ASF = (int)Math.Round(ASF * (4.6113 * Math.Pow(10, -4) * Math.Pow(ampD, 3) - 0.1305 * Math.Pow(ampD, 2) + 12.323 * ampD - 387.44));
                    }
                    else ASF = (int)Math.Round(ASF * amp / 100);
                }
                else ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                string ASFBinary = Calculate16Binary(ASF); // converts ASF in binary string

                ASFbyte0 = CalculateByte(ASFBinary, 0);
                ASFbyte1 = CalculateByte(ASFBinary, 8);
            }
            else if (frequency >= 230000000 && frequency <= 300000000)
            {
                double freqMHz = frequency / Math.Pow(10, 6);
                double RMS = -6.1763 * Math.Pow(10, -5) * Math.Pow(freqMHz, 3) + 0.037394 * Math.Pow(freqMHz, 2) - 7.3432 * freqMHz + 718.86;
                int ASF = (int)Math.Round(0.7 * 217 * Math.Pow(2, 14) / RMS);
                //ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                if (normalisation == true) // laser power normalisation so that x % on the GUI corresponds to x % of the max laser power
                {
                    if (ampD >= 5 && ampD <= 91.56)
                    {
                        ASF = (int)Math.Round(ASF * (8.78114279 * Math.Pow(10, -7) * Math.Pow(ampD, 3) - 1.323317 * Math.Pow(10, -4) * Math.Pow(ampD, 2) + 0.0126421389 * ampD + 0.0775117));
                    }
                    else if (ampD >= 91.57 && ampD <= 100)
                    {
                        ASF = (int)Math.Round(ASF * (4.6113 * Math.Pow(10, -4) * Math.Pow(ampD, 3) - 0.1305 * Math.Pow(ampD, 2) + 12.323 * ampD - 387.44));
                    }
                    else ASF = (int)Math.Round(ASF * amp / 100);
                }
                else ASF = (int)Math.Round(ASF * amp / 100); // multiplies the ASF by the amp percentage

                string ASFBinary = Calculate16Binary(ASF); // converts ASF in binary string

                ASFbyte0 = CalculateByte(ASFBinary, 0);
                ASFbyte1 = CalculateByte(ASFBinary, 8);
            }
            else // If out of range. Max ASF. Should not happen in normal operation.
            {
                   ASFbyte0 = "63";
                   ASFbyte1 = "255";
            }    
        }

        public static void GetPOW(decimal value, out string POWbyte0, out string POWbyte1)
        {
            POWbyte0 = "0";
            POWbyte1 = "0";

            double phase = Convert.ToDouble(value%360);

            int POW = CalculatePOW(phase); // calculates POW
            string POWBinary = Calculate16Binary(POW); // converts in binary string 

            POWbyte0 = CalculateByte(POWBinary, 0);
            POWbyte1 = CalculateByte(POWBinary, 8);
        }
    }
}
