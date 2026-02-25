using AODB;
using gltf;

namespace aogltf
{
    public class PlayfieldExporter
    {
        private RdbController _rdbController;

        public PlayfieldExporter(RdbController rdbController)
        {
            _rdbController = rdbController;
        }

        public bool Export(int playfieldId, string outputPath, FileFormat fileFormat, ExportMirror transforms = ExportMirror.NoMirror)
        {
            try
            {
                new TerrainExporter(_rdbController).Export(playfieldId, outputPath, fileFormat, transforms);
                new StatelExporter(_rdbController).Export(playfieldId, outputPath, fileFormat, transforms);
                new CollisionExporter(_rdbController).Export(playfieldId, outputPath, fileFormat, transforms);
                new WaterExporter(_rdbController).Export(playfieldId, outputPath, fileFormat, transforms);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}