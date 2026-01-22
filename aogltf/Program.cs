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
            var rdbController = new RdbController(aoPath);
            //AbiffExport(rdbController);
            CirExport(rdbController);
        }

        private static void CirExport(RdbController rdbController)
        {
            int modelId = 5927; //soli female
            CirExporter testExport = new CirExporter(rdbController);
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CirDump");
            Directory.CreateDirectory(exportDir);
            testExport.ExportGlb(exportDir, modelId);
        }

        private static void AbiffExport(RdbController rdbController)
        {
            int modelId = 289814; //Rollerrat in a Cage
            AbiffExporter testExport = new AbiffExporter(rdbController);
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AbiffDump");
            Directory.CreateDirectory(exportDir);
            testExport.ExportGlb(exportDir, modelId);
        }
    }
}
