using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

[Serializable]
public class ItemSpawnData
{
    public int weight;
    // public int x;
    // public int y;
    public TileBase tile;
}

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
    public Tilemap itemTilemap;

    public int width;//地图宽度
    public int height;//地图高度

    [Range(0, 1f)]
    public float waterProbability;//水概率

    public List<ItemSpawnData> itemSpawnDatas;//物品生成数据

//移孤岛Tile的次数
    public int removeSeparateTileNumberOfTimes=2;//移除单独瓷砖的次数



    public TileBase waterTile;//水瓷砖
    public TileBase groundTile;//地面瓷砖
    public TileBase itemTile;//物品瓷砖

    public int seed;//种子值
    
    public bool useRandomSeed;//是否使用随机种子值

    public float lacunarity;//分形噪声的分形因子


    private float[,] mapData;//地图数组True：ground False：water
    public void GenerateMap()
    {
        itemSpawnDatas.Sort((data1,data2)=>{
            return data1.weight.CompareTo(data2.weight);
        });
        Debug.Log("GenerateMap - 生成地图");
        GeneratorMapData();
        //TODO:地图处理
        for(int i=0;i<removeSeparateTileNumberOfTimes;i++)
        {
            if(RemoveSeparateTile())//如果没有移除单独瓷砖
            {
                break;
            }
        }



        GeneratorTileMap();
    }
    public void CleanMap()
    {
        Debug.Log("CleanMap - 清理地图");
        groundTilemap.ClearAllTiles();
        itemTilemap.ClearAllTiles();
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


        float randomOffset =UnityEngine.Random.Range(-10000,10000);
        
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

    private bool RemoveSeparateTile()
    {
        bool res=false;//是否移除单独瓷砖
        for(int x=0;x<width;x++){
            for(int y=0;y<height;y++){

                //如果当前瓷砖是地面，且只有1个邻居是地面
               if(IsGround(x,y)&&GetFourNeighborsGroundCount(x,y)==1)
               {
                    mapData[x,y]=0;//将当前瓷砖设置为水
                    res=true;//设置移除单独瓷砖为true
               }
            }
        }
        return res;
    }
    private int GetFourNeighborsGroundCount(int x,int y){
        int count=0;
        if(IsInMapRange(x-1,y)&&IsGround(x-1,y))
        {
            count++;
        }
        if(IsInMapRange(x+1,y)&&IsGround(x+1,y))
        {
            count++;
        }
        if(IsInMapRange(x,y-1)&&IsGround(x,y-1))
        {
            count++;
        }
        if(IsInMapRange(x,y+1)&&IsGround(x,y+1))
        {
            count++;
        }
        return count;
    }
    private int GetEightNeighborsGroundCount(int x,int y){
        int count=0;
        if(IsInMapRange(x-1,y)&&IsGround(x-1,y))
        {
            count++;
        }
        if(IsInMapRange(x+1,y)&&IsGround(x+1,y))
        {
            count++;
        }
        if(IsInMapRange(x,y-1)&&IsGround(x,y-1))
        {
            count++;
        }
        if(IsInMapRange(x,y+1)&&IsGround(x,y+1))
        {
            count++;
        }
        if(IsInMapRange(x-1,y-1)&&IsGround(x-1,y-1))
        {
            count++;
        }
        if(IsInMapRange(x+1,y-1)&&IsGround(x+1,y-1))
        {
            count++;
        }
        if(IsInMapRange(x-1,y+1)&&IsGround(x-1,y+1))
        {
            count++;
        }
        if(IsInMapRange(x+1,y+1)&&IsGround(x+1,y+1))
        {
            count++;
        }
        return count;
    }



    public bool IsInMapRange(int x,int y){
        return x>=0&&x<width&&y>=0&&y<height;
    }

    public bool IsGround(int x,int y){
        return mapData[x,y]>waterProbability;
    }


    private void GeneratorTileMap()
    {
        CleanMap();


        //生成地面
        for(int x=0;x<width;x++)
        {
            for(int y=0;y<height;y++)
            {
              TileBase tile=mapData[x,y]>waterProbability?groundTile:waterTile;
              groundTilemap.SetTile(new Vector3Int(x,y,0),tile);
            }
        }


        //生成物品
        int weightTotal=0;
        for(int i=0;i<itemSpawnDatas.Count;i++){
            weightTotal+=itemSpawnDatas[i].weight;
        }

        for(int x=0;x<width;x++){
            for(int y=0;y<height;y++){
                if(GetEightNeighborsGroundCount(x,y)>7&&IsGround(x,y))//如果当前瓷砖是地面
                {
                    float randValue=UnityEngine.Random.Range(1,weightTotal);
                    float temp=0;
                    
                    for(int i=0;i<itemSpawnDatas.Count;i++){
                        temp+=itemSpawnDatas[i].weight;
                        if(randValue<temp){
                            //如果随机值小于当前物品的权重，说明当前物品被选中
                            itemTilemap.SetTile(new Vector3Int(x,y,0),itemSpawnDatas[i].tile);
                            break;   
                        }   
                    }
                    continue;
                }
            }
        }
    }
}
