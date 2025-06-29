using AODB;

namespace aogltf
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AOExportTest();
        }

        private static void AOExportTest()
        {
            string aoPath = "D:\\Funcom\\Anarchy Online";
            AbiffExporter testExport = new AbiffExporter(new RdbController(aoPath));
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string exportDir = Path.Combine(desktopPath, "AbiffDump");
            Directory.CreateDirectory(exportDir);
            testExport.ExportGlb(exportDir, 283378);
        }
    }
}
