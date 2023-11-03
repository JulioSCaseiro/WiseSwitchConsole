using System.IO.Ports;
using System.Management;

class SwitchControlApp
{
    static SerialPort serialPort;

    static void Main(string[] args)
    {
        string portName = FindComPort();
        
        if (portName == null)
        {
            Console.WriteLine("No compatible COM port found.");
            // Handle the situation where no suitable COM port is detected
            return;
        }
        Console.WriteLine($"Using {portName}.");

        InitializeSerialPort(portName);


        // Main loop for user interaction
        while (true)
        {
            Console.WriteLine("1. Reset Switch");
            Console.WriteLine("2. Delete VLAN config");
            Console.WriteLine("3. Configure Ports");
            Console.WriteLine("4. Delete Port Configurations");
            Console.WriteLine("5. Exit");

            Console.Write("Select an option: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ResetSwitch();
                    break;
                case "2":
                    VlanDeleteSwitch();
                    break;
                case "3":
                    ConfigurePortsForTesting();
                    break;
                case "4":
                    DeletePortConfigurations();
                    break;
                case "5":
                    CloseSerialPort();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private static string FindComPort()
    {
        string[] ports = SerialPort.GetPortNames();

        foreach (string port in ports)
        {
            if (IsSwitchPort(port))
            {
                return port;
            }
        }
        return null; // No compatible COM port found
    }

    private static bool IsSwitchPort(string portName)
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM" + portName.Substring(3) + "%'");
        foreach (ManagementObject queryObj in searcher.Get())
        {
            if (queryObj["Caption"].ToString().Contains("USB Serial Port"))
            {
                return true;
            }
        }
        return false;
    }

    static void InitializeSerialPort(string portName)
    {
        serialPort = new SerialPort(portName, 9600); // Change baud rate as needed
        serialPort.Open();
    }

    static void CloseSerialPort()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }

    static void SendCommand(string command)
    {
        serialPort.WriteLine(command);
        Thread.Sleep(1000); // Adjust delay based on your switch's response time
    }

    static void ResetSwitch()
    {
        //********************************ENABLE SWITCH*******************************************

        EnableSwitch();

        //********************************DELETE STARTUP CONFIG*******************************************

        DeleteStartupConfig();

        //********************************DELETE CONFIG.TEXT*******************************************

        DeleteConfigText();

        //********************************RESET SWITCH*******************************************

        Reset();
    }

    static void VlanDeleteSwitch()
    {
        EnableSwitch();
        Console.WriteLine("Checking if Vlan configuration exists.");

        var filenames = new string[]
        {
            "vlan.dat.renamed", "vlan.dat"
        };
        foreach (string filename in filenames)
        {
            SendCommand($"more flash:{filename}");
            Thread.Sleep(500);
            // Read the response from the switch to confirm the status
            string response = serialPort.ReadExisting();

            if (response.Contains("No such file"))
            {
                Console.WriteLine($"{filename} not found.");
            }

            else
            {
                // Send commands to delete startup file
                SendCommand($"delete flash:{filename}");
                Thread.Sleep(1000);
                SendCommand($"{filename}");
                Thread.Sleep(1000);
                SendCommand("y");
                Thread.Sleep(1500); // Wait for 1.5 secs
                SendCommand($"more flash:{filename}"); // Example command to retrieve the switch's version
                Thread.Sleep(500);
                response = serialPort.ReadExisting();
                if (response.Contains("No such file"))
                {
                    Console.WriteLine($"{filename} erased successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to delete {filename}");
                }
            }
        }
    }

    static void ConfigurePortsForTesting()
    {
        // Send commands to configure ports for testing
        SendCommand("configure_ports_for_testing");
        Console.WriteLine("Ports configured for testing.");
    }

    static void DeletePortConfigurations()
    {
        EnableSwitch();
        DeleteStartupConfig();
        DeleteConfigText();
    }

    static void EnableSwitch()
    {
        Console.WriteLine("Enabling switch.");
        SendCommand("enable");
        Thread.Sleep(500); // Wait for 1 second
        string response = serialPort.ReadExisting();
        if(response.Contains("password"))
        {
        SendCommand("cisco");
        Thread.Sleep(500); // Wait for 1 second
        }
    }

    static void DeleteStartupConfig()
    {
        SendCommand("show startup-config"); // Example command to retrieve the switch's version
        Thread.Sleep(100);
        // Read the response from the switch to confirm the status
        string response = serialPort.ReadExisting();

        if (response.Contains("not present"))
        {
            Console.WriteLine("startup-config is missing.");
        }

        else
        {
            SendCommand("y"); // Example command to retrieve the switch's version
            Thread.Sleep(300); // Wait for 2 seconds
            // Send commands to delete startup file
            SendCommand("erase startup-config");
            Thread.Sleep(500); // Wait for 0.5 second
            SendCommand("y");
            Thread.Sleep(2000); // Wait for 9 secs
            SendCommand("show startup-config"); // Example command to retrieve the switch's version
            Thread.Sleep(500);
            response = serialPort.ReadExisting();
            if (response.Contains("not present"))
            {
                Console.WriteLine("startup-config erased successfully.");
            }
            else
            {
                Console.WriteLine("Failed to delete startup-config");
            }
        }
    }

    static void DeleteConfigText()
    {
        var filenames = new string[]
        {
            "private-config.text.renamed", "private-config.text", "config.text.renamed", "config.text"
        };
        foreach (string filename in filenames)
        {
            SendCommand($"more flash:{filename}");
            Thread.Sleep(500);
            // Read the response from the switch to confirm the status
            string response = serialPort.ReadExisting();

            if (response.Contains("No such file or directory"))
            {
                Console.WriteLine($"{filename} is missing.");
            }

            else
            {
                // Send commands to delete startup file
                SendCommand($"delete flash:{filename}");
                Thread.Sleep(1000);
                SendCommand($"{filename}");
                Thread.Sleep(1000);
                SendCommand("y");
                Thread.Sleep(1500); // Wait for 1.5 secs
                SendCommand($"more flash:{filename}"); // Example command to retrieve the switch's version
                Thread.Sleep(500);
                response = serialPort.ReadExisting();
                if (response.Contains("No such file or directory"))
                {
                    Console.WriteLine($"{filename} erased successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to delete {filename}");
                }
            }
        }
    }

    static void Reset()
    {
        SendCommand("reload");
        // Wait for the switch to reset (adjust the time according to the switch's response time)
        Thread.Sleep(1000); // Wait for 1 second
        SendCommand("y");
        Thread.Sleep(200000); // Wait for 4 mins
        // Send a command to check if the switch is responsive after reset
        SendCommand("show version"); // Example command to retrieve the switch's version

        // Read the response from the switch to confirm the status
        string response = serialPort.ReadExisting();

        if (response.Contains("Cisco IOS Software"))
        {
            Console.WriteLine("Switch reset successfully.");
            SendCommand("n");
            Thread.Sleep(500); // Wait for 0.5 second
            SendCommand("n");
            Thread.Sleep(500); // Wait for 0.5 second
            SendCommand("n");
        }
        else
        {
            Console.WriteLine("Failed to reset the switch.");
        }
    }
}
