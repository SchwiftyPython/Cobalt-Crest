using System.Collections.Generic; using UnityEngine;
public class SpatialHash2D<T> {
  private readonly float _cellSize;
  private readonly Dictionary<Vector2Int, List<T>> _cells = new();
  public SpatialHash2D(float cellSize){ _cellSize=Mathf.Max(0.1f,cellSize); }
  public void Clear()=>_cells.Clear();
  public void Add(Vector2 pos, T item){
    var key=new Vector2Int(Mathf.FloorToInt(pos.x/_cellSize), Mathf.FloorToInt(pos.y/_cellSize));
    if(!_cells.TryGetValue(key, out var list)){ list=new List<T>(8); _cells[key]=list; }
    list.Add(item);
  }
  public List<T> Query(Vector2 pos, float radius, List<T> results){
    results.Clear();
    int minX=Mathf.FloorToInt((pos.x-radius)/_cellSize), maxX=Mathf.FloorToInt((pos.x+radius)/_cellSize);
    int minY=Mathf.FloorToInt((pos.y-radius)/_cellSize), maxY=Mathf.FloorToInt((pos.y+radius)/_cellSize);
    for(var y=minY;y<=maxY;y++) for(var x=minX;x<=maxX;x++){ var key=new Vector2Int(x,y); if(_cells.TryGetValue(key, out var list))
      {
        results.AddRange(list);
      }
    }
    return results;
  }
}
