namespace SurfTimer;

public class Cfg : Attribute
{

    public string Path { get; private set; }

    public Cfg(string path)
    {
        this.Path = path;
    }

}