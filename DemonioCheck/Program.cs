using DemonioCheck.Models;
using System.Management;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        List<Discos> discos = new List<Discos>();
        string url = "https://localhost:7067/Calculos";
        Maquina maquina = new Maquina();
        maquina.Discos = new List<Discos>();
        // Obtener IP local
        string ipLocal = ObtenerIPLocal();
        maquina.IP = ipLocal;

        string nombrePC = Environment.MachineName;
        maquina.Nombre = nombrePC;

        // Dominio o grupo de trabajo
        string dominio = Environment.UserDomainName;
        maquina.Dominio = dominio;

        // Obtener uso de memoria RAM
        maquina.Discos.AddRange(ObtenerMemoriaRAM());
        // Obtener uso del disco duro
        maquina.Discos.AddRange(ObtenerUsoDisco());

        var json = System.Text.Json.JsonSerializer.Serialize(maquina);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = client.PostAsync(url, content).Result;
        }
    }

    static string ObtenerIPLocal()
    {
        string ip = "No disponible";
        foreach (var ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                ip = ipAddress.ToString();
                break;
            }
        }
        return ip;
    }

    static List<Discos> ObtenerMemoriaRAM()
    {
        List<Discos> discosRam = new List<Discos>();

        int count = 1;
        try
        {
            var wmi = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in wmi.Get())
            {
                Discos discoRam = new Discos();
                double total = Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024; // En MB
                double libre = Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024;     // En MB
                double usado = total - libre;
                discoRam.Nombre = $"Ram-{count}";
                discoRam.Total = $"{total:N2}";
                discoRam.EnUso = $"{usado:N2}";
                discoRam.Libre = $"{libre:N2}";
                discoRam.Tipo = "RAM";
                discosRam.Add(discoRam);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la memoria RAM: {ex.Message}");
        }
        return discosRam;
    }

    static List<Discos> ObtenerUsoDisco()
    {
        List<Discos> discosRam = new List<Discos>();
        int count = 1;
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    Discos discoRom = new Discos();
                    double totalGB = drive.TotalSize / Math.Pow(1024, 3);
                    double libreGB = drive.TotalFreeSpace / Math.Pow(1024, 3);
                    double usadoGB = totalGB - libreGB;
                    discoRom.Nombre = drive.Name;
                    discoRom.Total = $"{totalGB:N2}";
                    discoRom.EnUso = $"{usadoGB:N2}";
                    discoRom.Libre = $"{libreGB:N2}";
                    discoRom.Tipo = "ROM";
                    discosRam.Add(discoRom);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener información del disco: {ex.Message}");
        }
        return discosRam;
    }
}
