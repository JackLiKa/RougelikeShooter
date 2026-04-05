using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{


    void Start()
    {
        // 每次场景加载时自动生成地图
        GenerateMap();
    }
    void OnDestroy()
    {
        // 场景销毁时清理地图
        CleanMap();
    }

    public Tilemap groundTilemap;
    public int width;//地图宽度
    public int height;//地图高度

    [Range(0, 1f)]
    public float waterProbability;//水概率

    public TileBase waterTile;//水瓷砖
    public TileBase groundTile;//地面瓷砖

    public int seed;//种子值
    
    public bool useRandomSeed;//是否使用随机种子值

    public float lacunarity;//分形噪声的分形因子


    private float[,] mapData;//地图数组True：ground False：water
    public void GenerateMap()
    {
        Debug.Log("GenerateMap - 生成地图");
        GeneratorMapData();
        //TODO:地图处理
        GeneratorTileMap();
    }
    public void CleanMap()
    {
        Debug.Log("CleanMap - 清理地图");
        groundTilemap.ClearAllTiles();
    }

    private void GeneratorMapData()
    {
        //对于种子的应用
        if (!useRandomSeed)//如果不使用随机种子值
        {
            seed = Time.time.GetHashCode();
            // Debug.Log(seed);
        }
        UnityEngine.Random.InitState(seed);

        mapData=new float[width,height];


        float randomOffset =UnityEngine.Random.Range(-1000,1000);
        
        float minValue= float.MaxValue;
        float maxValue= float.MinValue;
        
        for(int x=0;x<width;x++)
        {
            for(int y=0;y<height;y++)
            {
                float noiseValue = Mathf.PerlinNoise(x*lacunarity+randomOffset,y*lacunarity+randomOffset);
                mapData[x,y]=noiseValue;
                
                if(noiseValue<minValue)
                {
                    minValue=noiseValue;
                }
                if(noiseValue>maxValue)
                {
                    maxValue=noiseValue;
                }
            }
        }

        //将噪声值映射到0-1之间
         for(int x=0;x<width;x++)
        {
            for(int y=0;y<height;y++)
            {
                mapData[x,y]=Mathf.InverseLerp(minValue,maxValue,mapData[x,y]);
            }
        }
    }
    private void GeneratorTileMap()
    {
        CleanMap();
        for(int x=0;x<width;x++)
        {
            for(int y=0;y<height;y++)
            {
              TileBase tile=mapData[x,y]>waterProbability?groundTile:waterTile;
              groundTilemap.SetTile(new Vector3Int(x,y,0),tile);
            }
        }
    }
}
