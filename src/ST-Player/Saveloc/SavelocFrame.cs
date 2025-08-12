namespace SurfTimer;

public class SavelocFrame
{
    public VectorT Pos { get; set; } = new VectorT(0, 0, 0);
    public QAngleT Ang { get; set; } = new QAngleT(0, 0, 0);
    public VectorT Vel { get; set; } = new VectorT(0, 0, 0);
    public int Tick { get; set; } = 0;
}
