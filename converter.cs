using System;
using System.IO;
using NMotive;

namespace OptiTrack_NMotive_Converter
{
    class converter
    {
        /*
         * This is here to suppress this warning:
         * QWindowsContext: OleInitialize() failed:  "COM error 0xffffffff80010106 RPC_E_CHANGED_MODE (Unknown error 0x080010106)"
         * Not sure why this happens, never did anything with Qt before, found this here:
         * https://github.com/qmlnet/qmlnet-examples/issues/17
         * Since I won't do anything with Qt GUIs in this code, I can afford not to care.
         */
        [STAThread]


        static int Main(string[] args)
        {
            // This is a simple tool, so I just display the usage when something is not OK.
            if(args.Length != 2)
            {
                print_usage();
                return -1;
            }
            // Get the input arguments.
            var take_file_name = args[0];
            //Console.WriteLine("Input file name is: {0}", take_file_name);
            var csv_file_name = args[1];
            //Console.WriteLine("Output file name is: {0}", csv_file_name);

            // No sanity check on the input arguments. Paths to files should be either absolute, or relative to the executable.
            // I know it's like giving a hand grenade to a monkey, but since this thing is to be called programmatically, I can live with it.

            // Load our take, and let the user know if the take failed to load
            Take input_take = null; // Just in case, clear this poor thing.
            try
            {
                try
                {
                    input_take = new Take (take_file_name); // This should be full path.
                }
                catch
                {
                    Console.WriteLine("Input .TAK file {0} could not be opened.", take_file_name);
                }
            }
            catch (NMotiveException nmotive_exception)
            {
                // Straight from TakeProcessingExample.cs
                Console.WriteLine("Couldn't load input take file.");
                Console.WriteLine("NMotive API says: {0}", nmotive_exception.Message);
                return -1;
            }

            //Console.WriteLine("Loading done.");


            // This is our CSVExporter object, as per the API reference manual.
            var csv_exporter = new CSVExporter();


            /*
             * Here, we set our CSV exporter's preferences.
             * I adjusted this for our lab. Your preferences might be different, adjust them as needed.
             */

            csv_exporter.Units = LengthUnits.Units_Millimeters; // We use milimeters in our stuff.
            csv_exporter.WriteHeader = true; // Add header to the csv file. This is a bit of a headache because of the stuff inside it, but, meh.
            csv_exporter.WriteMarkers = false; // Add marker data to the csv file.
            csv_exporter.WriteRigidBodies = true; // We use a ton of rigid bodies, we need these.
            csv_exporter.WriteRigidBodyMarkers = false; // Just in case.


            // Now do the actual exporting
            Result export_result = csv_exporter.Export(input_take, csv_file_name, true);
            if(!export_result.Success)
            {
                // I like how these people throught of error management.
                Console.WriteLine("Error exporting the take.");
                Console.WriteLine("NMotive API says: {0}", export_result.Message);
                return -1;
            }

            //Console.WriteLine("Done!");

            return 0;

        }

        static void print_usage()
        {
            // Print basic usage.
            Console.WriteLine("Usage:");
            Console.WriteLine("converter <input TAK file path> <output CSV file path>");
            Console.WriteLine("The paths can be absolute or relative to the executable file. That's pretty much it.");
        }
    }
}
