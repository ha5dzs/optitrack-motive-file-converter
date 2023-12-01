using System;
using System.IO;
using NMotive;

namespace OptiTrack_NMotive_Converter
{
    class Converter
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
            if(args.Length < 2 || args.Length > 3 )
            {
                print_usage();
                return -1;
            }
            // Get the input arguments.
            var take_file_name = args[0];
            //Console.WriteLine("Input file name is: {0}", take_file_name);
            var csv_file_name = args[1];
            //Console.WriteLine("Output file name is: {0}", csv_file_name);

            Rotation rotation_format = 0;

            if(args.Length == 3)
            {
                // If we got here, update the format code with the third input argument
                switch(Convert.ToInt32(args[2]))
                {
                    // I have to do it this way, because I can't seem to cast the input argument string into NMotive.Rotation
                    // Steve Ballmer will probably know why.

                    case 1:
                        rotation_format = (NMotive.Rotation)1;
                        break;

                    case 2:
                        rotation_format = (NMotive.Rotation)2;
                        break;
                    case 3:
                        rotation_format = (NMotive.Rotation)3;
                        break;
                    case 4:
                        rotation_format = (NMotive.Rotation)4;
                        break;
                    case 5:
                        rotation_format = (NMotive.Rotation)5;
                        break;
                    case 6:
                        rotation_format = (NMotive.Rotation)6;
                        break;

                    // Any other weird value will be interpreted as 0.
                    default:
                        rotation_format = 0;
                        break;
                }
            }


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

            // Reconstruct and auto-label. Motive 3 has some quick rigid body solver, which doesn't always work with complicated objects.
            Trajectorizer trajectoriser = new Trajectorizer(); // Input argument is a progress bar, but we won't care about this.

            /* I am trying without this file
            // I don't know why, but the example reads in the reconstruction settings.
            string reconstruction_settings_file = "ReconstructionSettings.motive";
            NMotive.Result import_result = Settings.ImportMotiveProfile( reconstruction_settings_file );

            if ( !import_result.Success )
            {
                Console.WriteLine("Couldn't import the reconstruction settings file {0}", reconstruction_settings_file);
                return -1;
            }

            */


            trajectoriser.Process( input_take, TrajectorizerOption.ReconstructAndAutoLabel ); // Do the job
            input_take.Save(); // Just in case.


            // Solve
            input_take.Solve();
            input_take.Save();


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
            csv_exporter.RotationType = rotation_format; // As specified by the input argument.


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
            Console.WriteLine("converter <input TAK file path> <output CSV file path> <OPTIONAL: rotation_format>");
            Console.WriteLine("The paths can be absolute or relative to the executable file.");
            Console.WriteLine("WARNING: Rotation format is non-obvious, please specify the format as follows:");
            Console.WriteLine("Number\tFormat and rotation order");
            Console.WriteLine("0\tQuaternion, wxyz (wijk)");
            Console.WriteLine("1\tEuler, XYZ");
            Console.WriteLine("2\tEuler, XZY");
            Console.WriteLine("3\tEuler, YXZ");
            Console.WriteLine("4\tEuler, YZX");
            Console.WriteLine("5\tEuler, ZXY");
            Console.WriteLine("6\tEuler, ZYX");
        }
    }
}
