namespace SurfTimer;

internal class PlayerStats
{
    // To-Do: Each stat should be a class of its own, with its own methods and properties - easier to work with. 
    //        Temporarily, we store ticks + basic info so we can experiment

    // These account for future style support and a relevant index.
    public int[,] PB {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: map/bonus (0 = map, 1+ = bonus index)
    public int[,] Rank {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: map/bonus (0 = map, 1+ = bonus index)
    public int[,] Checkpoints {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: checkpoint index
    public int[,] StagePB {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: stage index
    public int[,] StageRank {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: stage index
}