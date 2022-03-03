﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Waveplus.DaqSys;
using Waveplus.DaqSysInterface;
using WaveplusLab.Shared.Definitions;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            /*
             * 1. [Server] Start TCP server, wait for connections
             * 2. [Matlab] Start matlab program that connects
             *  a.[Matlab] Send integer denoting sensor count
             * 3. [Server] When receiving connection, start capture with requested sensor count
             * 4. [Server] Send data to socket as soon as it's received from sensors
             * 5. [Matlab] Read data from socket, GOTO 4
             */

            new Program();

        }
        bool FAKEDAQ = true;
        DaqSystem daqSystem;
        DateTime starttime = DateTime.UtcNow;

        UDPSocket c = new UDPSocket();
        //string[] imu_names;

        Dictionary<int, string> imu_dict =
               new Dictionary<int, string>();

        int numframes = 0;

        public DataAvailableEventArgs DistrDaq(string[] trow)
        {
            DataAvailableEventArgs e = new DataAvailableEventArgs();
            e.Samples = new float[32, 32000];
            e.ImuSamples = new float[32, 4, 32000];
            e.AccelerometerSamples = new float[32, 3, 32000];
            e.GyroscopeSamples = new float[32, 3, 32000];
            e.MagnetometerSamples = new float[32, 3, 32000];
            string[] row = trow.Skip(1).ToArray();
            float time = float.Parse(trow[0]); /// not sure how to save it in e yet.
            //for (int i =0; i< imu_names.Length; i++)
            int j = 0;
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
                {
                int i = ele1.Key;

                string imu = ele1.Value;
                //string imu = imu_names[i];
                Console.WriteLine("Evaling imu( {0} ): {1}", i.ToString(), imu);
                // now there is a fixed sequence which i must follow
                //q1,q2,q3,q4
                //ax,ay,az
                //gx,gy,gz
                //mx,my,mz
                //barometer
                //linAcc(x,y,z)
                //altitude
                int I = j * 18;
                Console.WriteLine("I: {0}, j: {1}",I.ToString(), j.ToString() );
                j++;
                e.ImuSamples[i, 0, 0] = float.Parse(row[I + 0]);
                e.ImuSamples[i, 1, 0] = float.Parse(row[I + 1]);
                e.ImuSamples[i, 2, 0] = float.Parse(row[I + 2]);
                e.ImuSamples[i, 3, 0] = float.Parse(row[I + 3]);
                e.AccelerometerSamples[i, 0, 0] = float.Parse(row[I + 4]);
                e.AccelerometerSamples[i, 1, 0] = float.Parse(row[I + 5]);
                e.AccelerometerSamples[i, 2, 0] = float.Parse(row[I + 6]);
                e.GyroscopeSamples[i, 0, 0] = float.Parse(row[I + 7]);
                e.GyroscopeSamples[i, 1, 0] = float.Parse(row[I + 8]);
                e.GyroscopeSamples[i, 2, 0] = float.Parse(row[I + 9]);
                e.MagnetometerSamples[i, 0, 0] = float.Parse(row [I + 10]);
                e.MagnetometerSamples[i, 1, 0] = float.Parse(row [I + 11]);
                e.MagnetometerSamples[i, 2, 0] = float.Parse(row [I + 12]);
                                                      //e.Barometer, linAcc, altitude // no existe!

                //e.ImuSamples = new float[32, 4, 32000];
                //e.ImuSamples[i, 0, 0] = 0.0F;//float.Parse(row[I + 0]);
                //e.ImuSamples[i, 1, 0] = 0.0F;//float.Parse(row[I + 1]);
                //e.ImuSamples[i, 2, 0] = 0.0F;//float.Parse(row[I + 2]);
                //e.ImuSamples[i, 3, 0] = 0.0F;//float.Parse(row[I + 3]);
                //e.AccelerometerSamples = new float[32, 3, 32000];
                //e.AccelerometerSamples[i, 0, 0] = 0.0F;//float.Parse(row[I + 4]);
                //e.AccelerometerSamples[i, 1, 0] = 0.0F;//float.Parse(row[I + 5]);
                //e.AccelerometerSamples[i, 2, 0] = 0.0F;//float.Parse(row[I + 6]);
                //e.GyroscopeSamples = new float[32, 3, 32000];
                //e.GyroscopeSamples[i, 0, 0] = 0.0F;//float.Parse(row[I + 7]);
                //e.GyroscopeSamples[i, 1, 0] = 0.0F;//float.Parse(row[I + 8]);
                //e.GyroscopeSamples[i, 2, 0] = 0.0F;//float.Parse(row[I + 9]);
                //e.MagnetometerSamples = new float[32, 3, 32000];
                //e.MagnetometerSamples[i, 0, 0] = 0.0F;//float.Parse(row [I + 10]);
                //e.MagnetometerSamples[i, 1, 0] = 0.0F;//float.Parse(row [I + 11]);
                //e.MagnetometerSamples[i, 2, 0] = 0.0F;//float.Parse(row [I + 12]);
                //                                      //e.Barometer, linAcc, altitude // no existe!

            }
            return e;
        }
        public Quaternion convertQuaternion(Quaternion q, double angle, bool LEFTHANDEDSYSTEM = false)
        {

            //see https://stackoverflow.com/questions/28673777/convert-quaternion-from-right-handed-to-left-handed-coordinate-system
            //and https://gamedev.stackexchange.com/questions/129204/switch-axes-and-handedness-of-a-quaternion
            /*Quaternion Q1 = new Quaternion((float)Math.Cos(angle / 2) * q.X, 
                                           (float)Math.Sin(angle / 2) * q.Y,
                                           (float)Math.Sin(angle / 2) * q.Z,
                                           (float)Math.Sin(angle / 2) * q.W);*/
            Quaternion q0 = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f); //this is the angle 120 around the vector 1,1,1; for this angle to have any meaning this is what needs to be done, but I won't do it now.
            Quaternion Q1 = Quaternion.Concatenate(q,q0);
            if (LEFTHANDEDSYSTEM)
            {
                Q1 = new Quaternion(Q1.X, -Q1.Y, -Q1.Z, -Q1.W);
            }
            return Q1;
        }

        public string eEeParser(DataAvailableEventArgs e, int sampleNumber)
        {            
            TimeSpan t = DateTime.UtcNow - starttime;
            //TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string output =  t.TotalSeconds.ToString() + " ";
            //for (int i = 0; i < imu_names.Length; i++)
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
            {
                int i = ele1.Key;

                string imu = ele1.Value;
                Console.WriteLine("imu ({1}): {0}", imu, i);
                float q0, q1, q2, q3;
                q0 = e.ImuSamples[i, 0, sampleNumber];
                q1 = e.ImuSamples[i, 1, sampleNumber];
                q2 = e.ImuSamples[i, 2, sampleNumber];
                q3 = e.ImuSamples[i, 3, sampleNumber];
                Quaternion Q = new Quaternion(q0, q1, q2, q3);
                
                Console.WriteLine(e.ImuSamples);
                Console.WriteLine(Q);

                //foreach (int j in e.ImuSamples)
                //{
                //    Console.Write("{0} ", j);
                //}
                //
                Quaternion QQ = convertQuaternion(Q, 0.0);
                Console.WriteLine(QQ);
                float qq0, qq1, qq2, qq3;
                qq0 = QQ.X;
                qq1 = QQ.Y;
                qq2 = QQ.Z;
                qq3 = QQ.W;
                ///this is wrong!
                Console.WriteLine("quarternions             : {0}, {1}, {2}, {3}", q0,q1,q2,q3);
                Console.WriteLine("quarternions converted   : {0}, {1}, {2}, {3}", qq0,qq1,qq2, qq3);
                output += q0.ToString() + " ";
                output += q1.ToString() + " ";
                output += q2.ToString() + " ";
                output += q3.ToString() + " ";                
                output += e.AccelerometerSamples[i,0, sampleNumber].ToString() + " ";
                output += e.AccelerometerSamples[i,1, sampleNumber].ToString() + " ";
                output += e.AccelerometerSamples[i,2, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,0, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,1, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,2, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,0, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,1, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,2, sampleNumber].ToString() + " ";
                output += "0.0 "; //barometer
                output += "0.0 "; //linAccx
                output += "0.0 "; //linAccy
                output += "0.0 "; //linAccz
                output += "0.0 "; //altitude ??? this should be here, but then i have one extra column
            }
            //Console.WriteLine("Parsed output: "+ output);
            return output;       
        
        }

        public Program()
        {
            ConfigureDaq(); /// THIS IS RUBBISH, 

            imu_dict.Add(11, "TORAX");
            imu_dict.Add(12, "HUMERUS");
            imu_dict.Add(13, "RADIUS");
            //imu_names = new string[] {"a","b","c","d",
            //                          "e","f","g","h"  };  //lower body
            //imu_names = new string[] {"a","b","c","d",
            //                          "e","f","g","h"  }; //upper body

            StartServer();
            


            bool UPPER = true;
            var lowbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\gait1992_imu.csv";
            var uppbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\mobl2016_imu.csv";
            string dasfile;
            if (UPPER)
                dasfile = uppbodyfile;
            else
                dasfile = lowbodyfile;

            if (FAKEDAQ)
            {
                Console.WriteLine("Starting capture");
                using (var reader = new StreamReader(dasfile))
                {
                    DataAvailableEventArgs e = new DataAvailableEventArgs();
                    //float[,] datatable = new float[17 * 8, 32000];
                    reader.ReadLine();
                    reader.ReadLine();
                    reader.ReadLine();
                    reader.ReadLine();

                    reader.ReadLine(); // labels
                    while (FAKEDAQ && !reader.EndOfStream)
                    {
                        Console.WriteLine("LOOP");
                        var line = reader.ReadLine();
                        Console.WriteLine("rowread:" + line);
                        var values = line.Split(',');

                        e = DistrDaq(values);

                        e.ScanNumber = 1;
                        Capture_DataAvailable(null, e);

                        System.Threading.Thread.Sleep(1);
                    }
                }
            }
            else
            {
                Console.WriteLine("Starting capture");
                daqSystem.StartCapturing(DataAvailableEventPeriod.ms_10); // Available: 100, 50, 25, 10            
            }
            Console.WriteLine("Finished!");
            c.Send("BYE!");
               Console.ReadKey();
        }

        private void StartServer()
        {
            string client_ip = "127.0.0.1";
            int client_port = 8080;
            Console.WriteLine("Will spit data as udp in {0}, {1}", client_ip, client_port.ToString());
            // now the third horriblest serverino:
            c.Client(client_ip, client_port);

        }

        private void ConfigureDaq()
        {
            // Create daqSystem object and assign the event handlers
            daqSystem = new DaqSystem();
            daqSystem.StateChanged += Device_StateChanged;
            daqSystem.DataAvailable += Capture_DataAvailable;

            // Configure sensors
            // .InstalledSensors = 16, not the number of sensed sensors
            // Configure sensors from channel 1-8 as EMG sensors, 9-16 as IMU sensors
            for (int EMGsensorNumber = 0; EMGsensorNumber < daqSystem.InstalledSensors - 8; EMGsensorNumber++)
            {
                Console.WriteLine("Configuring EMG sensor #" + EMGsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.EMG_SENSOR },
                    EMGsensorNumber
                );
            }

            for (int IMUsensorNumber = 8; IMUsensorNumber < daqSystem.InstalledSensors; IMUsensorNumber++)
            {
                Console.WriteLine("Configuring IMU sensor #" + IMUsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.INERTIAL_SENSOR },
                    IMUsensorNumber
                );
            }

            Console.WriteLine("Configuring capture");
            var new_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.Fused9xData_142Hz };
            var old_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.RawData };
            daqSystem.ConfigureCapture(old_config);
            //daqSystem.ConfigureCapture(new_config);
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            //int samplesPerChannel = 1; // somewhere this is defined. sending 20 samples sounds interesting, but right now, i don't know how to deal with this
            int samplesPerChannel = e.ScanNumber; // what's this?
            Console.WriteLine("scan number ???" + e.ScanNumber);
            //int channelsNumber = 16; // Number of output channels
            //double[] values = new double[samplesPerChannel * channelsNumber]; // Change to add more sensors
            //string output = "";
            for (int sampleNumber = 0; sampleNumber < samplesPerChannel; sampleNumber = sampleNumber + 1) // This loops captures data from sensor # sampleNumber+1
            {
                Console.WriteLine("samplenumber: "+ sampleNumber.ToString());
                //Console.WriteLine("EMGSensor #" + 1 + ": " + e.Samples[0, sampleNumber]);
                //Console.WriteLine("EMGSensor #" + 2 + ": " + e.Samples[1, sampleNumber]);
                //Console.WriteLine("EMGSensor #" + 3 + ": " + e.Samples[2, sampleNumber]);
                //Console.WriteLine("EMGSensor #" + 4 + ": " + e.Samples[3, sampleNumber]);

                //values[sampleNumber * 4 + 0] = e.Samples[0, sampleNumber];
                //values[sampleNumber * 4 + 1] = e.Samples[1, sampleNumber];
                //values[sampleNumber * 4 + 2] = e.Samples[2, sampleNumber];
                //values[sampleNumber * 4 + 3] = e.Samples[3, sampleNumber];
                //Console.WriteLine("IMUSensor #" + 13 + "Gyroscope X: " + e.GyroscopeSamples[12, 0, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 13 + "Gyroscope Y: " + e.GyroscopeSamples[12, 1, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 13 + "Gyroscope Z: " + e.GyroscopeSamples[12, 2, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 13 + "Acceleration X: " + e.AccelerometerSamples[12, 0, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 13 + "Acceleration Y: " + e.AccelerometerSamples[12, 1, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 13 + "Acceleration Z: " + e.AccelerometerSamples[12, 2, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Gyroscope X: " + e.GyroscopeSamples[13, 0, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Gyroscope Y: " + e.GyroscopeSamples[13, 1, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Gyroscope Z: " + e.GyroscopeSamples[13, 2, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Acceleration X: " + e.AccelerometerSamples[13, 0, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Acceleration Y: " + e.AccelerometerSamples[13, 1, sampleNumber]);
                //Console.WriteLine("IMUSensor #" + 14 + "Acceleration Z: " + e.AccelerometerSamples[13, 2, sampleNumber]);
                //values[sampleNumber * 22 + 0] = e.Samples[0, sampleNumber];
                //values[sampleNumber * 22 + 1] = e.Samples[1, sampleNumber];
                //values[sampleNumber * 22 + 2] = e.Samples[2, sampleNumber];
                //values[sampleNumber * 22 + 3] = e.Samples[3, sampleNumber];
                //values[sampleNumber * 22 + 4] = e.GyroscopeSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 5] = e.GyroscopeSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 6] = e.GyroscopeSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 7] = e.AccelerometerSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 8] = e.AccelerometerSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 9] = e.AccelerometerSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 10] = e.ImuSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 11] = e.ImuSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 12] = e.ImuSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 13] = e.GyroscopeSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 14] = e.GyroscopeSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 15] = e.GyroscopeSamples[9, 2, sampleNumber];
                //values[sampleNumber * 22 + 16] = e.AccelerometerSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 17] = e.AccelerometerSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 18] = e.AccelerometerSamples[9, 2, sampleNumber];
                //values[sampleNumber * 22 + 19] = e.ImuSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 20] = e.ImuSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 21] = e.ImuSamples[9, 2, sampleNumber];
                //values[sampleNumber * 16 + 0] = e.Samples[0, sampleNumber];
                //values[sampleNumber * 16 + 1] = e.Samples[1, sampleNumber];
                //values[sampleNumber * 16 + 2] = e.Samples[2, sampleNumber];
                //values[sampleNumber * 16 + 3] = e.Samples[3, sampleNumber];
                //values[sampleNumber * 16 + 4] = e.GyroscopeSamples[12, 0, sampleNumber];
                //values[sampleNumber * 16 + 5] = e.GyroscopeSamples[12, 1, sampleNumber];
                //values[sampleNumber * 16 + 6] = e.GyroscopeSamples[12, 2, sampleNumber];
                //values[sampleNumber * 16 + 7] = e.AccelerometerSamples[12, 0, sampleNumber];
                //values[sampleNumber * 16 + 8] = e.AccelerometerSamples[12, 1, sampleNumber];
                //values[sampleNumber * 16 + 9] = e.AccelerometerSamples[12, 2, sampleNumber];
                //values[sampleNumber * 16 + 10] = e.GyroscopeSamples[13, 0, sampleNumber];
                //values[sampleNumber * 16 + 11] = e.GyroscopeSamples[13, 1, sampleNumber];
                //values[sampleNumber * 16 + 12] = e.GyroscopeSamples[13, 2, sampleNumber];
                //values[sampleNumber * 16 + 13] = e.AccelerometerSamples[13, 0, sampleNumber];
                //values[sampleNumber * 16 + 14] = e.AccelerometerSamples[13, 1, sampleNumber];
                //values[sampleNumber * 16 + 15] = e.AccelerometerSamples[13, 2, sampleNumber];
                //Console.WriteLine("values.Length:" + values.Length);
                Console.WriteLine("ScanNumber:" + e.ScanNumber);
                //DisplayArray(values, "wo");

                //values[sampleNumber * 8 + 4] = e.Samples[4, sampleNumber];
                //values[sampleNumber * 8 + 5] = e.Samples[5, sampleNumber];
                //values[sampleNumber * 8 + 6] = e.Samples[6, sampleNumber];
                //values[sampleNumber * 8 + 7] = e.Samples[7, sampleNumber];

                //
                //output += eEeParser(e, sampleNumber);
                eEeParser(e, sampleNumber);

            }
            numframes++;

            //servidor numbero 3!
            //c.Send(output);
            Console.WriteLine("this goes to the socket:");
            if (numframes > 0)
            {
                c.Send(eEeParser(e, 0));
            }
            else
            {
                Console.WriteLine("skipping initial frames!");
            }
                //foreach (int value in values)
            //    Console.Write("{0}  ", value);
            Console.WriteLine("Values has been sent");

        }

        private void Device_StateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            Console.WriteLine(e);
        }
    }

}
