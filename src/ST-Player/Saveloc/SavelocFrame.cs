namespace SurfTimer;

public class SavelocFrame
{
        public Vector_t Pos { get; set; } = new Vector_t(0, 0, 0);
        public QAngle_t Ang { get; set; } = new QAngle_t(0, 0, 0);
        public Vector_t Vel { get; set; } = new Vector_t(0, 0, 0);
        public int Tick { get; set; } = 0;
}
