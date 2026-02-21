using AODB;
using gltf;

public abstract class PlayfieldExporterBase<TData>
{
    protected RdbController _rdbController;
    protected int PlayfieldId;

    protected PlayfieldExporterBase(RdbController rdbController)
    {
        _rdbController = rdbController;
    }

    public bool Export(int playfieldId, string outputFolder, FileFormat fileFormat)
    {
        PlayfieldId = playfieldId;
        var data = ParseData();

        return fileFormat switch
        {
            FileFormat.Gltf => ExportGltf(outputFolder, data),
            FileFormat.Glb => ExportGlb(outputFolder, data),
            _ => false,
        };
    }

    protected abstract TData ParseData();

    public abstract bool ExportGlb(string outputFolder, TData data);

    public abstract bool ExportGltf(string outputFolder, TData data);
}