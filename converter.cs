using System;
using System.IO;
using NMotive;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

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
            /*
             * Objects we need to play with
             */

            Trajectorizer trajectoriser = new Trajectorizer(); // This is for processing the .tak file
            CSVExporter csv_exporter = new CSVExporter(); // This one creates the csv_file from the .tak file.
            XmlDocument config_file = new XmlDocument(); // This is the object where I hold all the 'global' settings.

            /*
             * Check config files
             */


                /*
                * Trajectoriser settings.
                * Got this from: https://forums.naturalpoint.com/viewtopic.php?t=26411
                *
                * Unfortunately, we have no option, but to load it in from a file.
                */

            // This file is in the executable's path. C# doesn't take this into account, so I need to create an absolute path

            // Thanks to: https://stackoverflow.com/questions/3991933/get-path-for-my-exe
            var executable_path = AppDomain.CurrentDomain.BaseDirectory;

            //Console.WriteLine(executable_path);

            var reconstruction_settings_file_name = "ReconstructionSettings.motive";

            var reconstruction_settings_file_path = executable_path + reconstruction_settings_file_name;


            NMotive.Result import_result = Settings.ImportMotiveProfile( reconstruction_settings_file_path );

            if ( !import_result.Success )
            {
                Console.WriteLine("Couldn't import the reconstruction settings file {0}", reconstruction_settings_file_path);
                return -1; // No point in continuing, if the reconstruction settings file couldn't be loaded.
            }



                /*
                * CSVExporter settings.
                * These are created manually here.
                * If there is a problem with the csv exporter config file, this will create it with these defaults.
                */

            var csv_exporter_settings_file_name = "CSVExporterSettings.motive";

            var csv_exporter_settings_file_path = executable_path + csv_exporter_settings_file_name;

            csv_exporter.RotationType = Rotation.QuaternionFormat; // Rotation is quaternion. More complicated, but less messy than Euler angles.
            csv_exporter.Units = LengthUnits.Units_Millimeters; // We use mm in the lab.
            csv_exporter.UseWorldSapceCoordinates = true; // This will matter when VR is used.
            csv_exporter.WriteBoneMarkers = false; // We don't use skeletons here. Some people might do.
            csv_exporter.WriteHeader = true; // Add header to the csv file. This is a bit of a headache because of the stuff inside it, but, meh.
            csv_exporter.WriteMarkers = false; // We principally use rigid bodies only. Some people don't.
            csv_exporter.WriteRigidBodies = true; // We use a ton of rigid bodies, we need these.
            csv_exporter.WriteRigidBodyMarkers = false; // Just in case.

            if(!File.Exists(csv_exporter_settings_file_path))
            {
                // If we got here, we need to export the CSV settings into an xml file
                // 'inspired' by: https://stackoverflow.com/questions/4123590/serialize-an-object-to-xml
                // ...and: https://stackoverflow.com/questions/4363886/how-to-add-a-line-break-when-using-xmlserializer

                var XmlWriterSettings = new XmlWriterSettings() {Indent = true}; // Add intentation, so the file is human-readable
                XmlSerializer csv_exporter_serialiser = new XmlSerializer(typeof(CSVExporter));
                StringWriter string_writer = new StringWriter();

                // Probably there are simpler ways of doing it. Developers, developers, developers.
                using( var writer = XmlWriter.Create(string_writer, XmlWriterSettings))
                {
                    csv_exporter_serialiser.Serialize(writer, csv_exporter);
                    Console.WriteLine("No CSV Exporter configuration file is found, creating a default one.");
                    //Console.WriteLine(string_writer.ToString());
                    string output_string = string_writer.ToString();
                    File.WriteAllTextAsync(csv_exporter_settings_file_path, output_string);
                }


            }
            else
            {
                // We have a file, we read it, deserialise it, and assign it to csv_exporter.
                //Console.WriteLine("CSV exporter config file found.");
                try
                {
                    using (var reader = new StreamReader(csv_exporter_settings_file_path))
                    {
                        csv_exporter = (CSVExporter) new XmlSerializer(typeof(CSVExporter)).Deserialize(reader);
                    }

                }
                catch(Exception e)
                {
                    // Might as well.
                    Console.WriteLine("Reading the config file failed.\n The computer says: {0}", e.Message);
                    Console.WriteLine("Try deleting CSVExporterSettings.motive, and starting configuring it again.");
                    return -1; // If we use bad config file, no point in continuing

                }

                //Console.WriteLine("Loading the config file was successful.");


            }


            /*
             * Basic sanity checks
             * (nothing too fancy)
             */

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
                // Update the csv_exporter setting here, if we have an optional input argument:
                csv_exporter.RotationType = rotation_format; // As specified by the input argument.
            }


            // Some sanity check on the input arguments. Paths to files should be either absolute, or relative to the executable.
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


            trajectoriser.Process( input_take, TrajectorizerOption.ReconstructAndAutoLabel ); // Do the job
            input_take.Save(); // Just in case.


            // Solve and save
            input_take.Solve();
            input_take.Save();


            // Now do the actual exporting with the configured csv_exporter
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
            Console.WriteLine("This option will override whatever you specify in CSVExporterSettings.motive.");
        }
    }
}
