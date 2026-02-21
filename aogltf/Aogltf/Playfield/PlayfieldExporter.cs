
using AODB;
using gltf;

namespace aogltf
{
    public class PlayfieldExporter
    {
        private RdbController _rdbController;

        public PlayfieldExporter (RdbController rdbController)
        {
            _rdbController = rdbController;
        }

        public bool Export(int playfieldId, string outputPath, FileFormat fileFormat)
        {
            try
            {
                new TerrainExporter(_rdbController).Export(playfieldId, outputPath, fileFormat);
                new StatelExporter(_rdbController).Export(playfieldId, outputPath, fileFormat);
                new CollisionExporter(_rdbController).Export(playfieldId, outputPath, fileFormat);
                new WaterExporter(_rdbController).Export(playfieldId, outputPath, fileFormat);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}