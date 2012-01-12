using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

using MTV3D65;

namespace WorldEngine
{
    //Position of a map tile
    public class TilePosition
    {
        public int TileX = 0;
        public int TileZ = 0;

        public TilePosition(int TileX = 0, int TileZ = 0)
        {
            this.TileX = TileX;
            this.TileZ = TileZ;
        }

        public TilePosition(TilePosition Position)
        {
            this.TileX = Position.TileX;
            this.TileZ = Position.TileZ;
        }

        public int TileDistanceTo(TilePosition Position)
        {
            return (int)Math.Sqrt(Math.Pow(Position.TileX - this.TileX, 2) + Math.Pow(Position.TileZ - this.TileZ, 2));
        }
    }
    public class WorldPosition : TilePosition
    {
        public float TilePosX = 0f;
        public float TilePosY = 0f;
        public float TilePosZ = 0f;

        public WorldPosition(int TileX = 0, int TileZ = 0, float TilePosX = 0f, float TilePosY = 0f, float TilePosZ = 0f)
            : base(TileX, TileZ) //We call the TilePosition constructor
        {
            this.TilePosX = TilePosX;
            this.TilePosY = TilePosY;
            this.TilePosZ = TilePosZ;
        }

        public WorldPosition(TilePosition Position)
            : base(Position)
        {
            this.TileX = Position.TileX;
            this.TileZ = Position.TileZ;
        }
    }

    public class WorldMap
    {
        //Constants
        public const int RenderedTilesDistance = 1;

        //Position
        WorldPosition PlayerPos = new WorldPosition();
        WorldPosition WorldPos = new WorldPosition();

        TVScene Scene;

        MapTile[][] MapTiles; //We use jagged arrays because it's much faster than multidimentionnal arrays in c# //TODO : Flattened array?
        bool LoadingMapTiles = true;

        Thread LoadThread;
        public bool LoadThreadWork = true;

        Thread LoadHeightmapsThread;
        public bool LoadHeightmapsThreadWork = true;

        LinkedList<MapTile> TilesList = new LinkedList<MapTile>();

        //Queue<TilePosition> TilesHeightmapToLoad = new Queue<TilePosition>();
        NGenerics.DataStructures.Queues.PriorityQueue<TilePosition, int> TilesHeightmapToLoad = new NGenerics.DataStructures.Queues.PriorityQueue<TilePosition, int>(NGenerics.DataStructures.Queues.PriorityQueueType.Minimum);
        //LinkedList<TilePosition> TilesHeightmapToLoad = new LinkedList<TilePosition>();

        //Constructor
        public WorldMap(TVScene Scene)
        {
            this.Scene = Scene;

            //Load every tile
            this.CheckLoadTiles(true);

            //Start the loader thread
            LoadThread = new Thread(new ThreadStart(CheckTilesToLoad));
            LoadThread.Start();

            //Start the heightmap loader thread
            LoadHeightmapsThread = new Thread(new ThreadStart(LoadHeightmaps));
            LoadHeightmapsThread.Start();

            System.Diagnostics.Debug.WriteLine("WorldMap initialized");
        }

        //Render the different tiles
        public void Render()
        {
            //It's faster to begin by j and then iterate over i!
            /*for (short j = 0; j < (2 * RenderedTilesDistance + 1); j++)
            {
                for (short i = 0; i < (2 * RenderedTilesDistance + 1); i++)
                {
                    MapTiles[i][j].Render(); //We render the tile's landscape and meshes
                }
            }*/
            //TimeSpan RenderingBegin = Process.GetCurrentProcess().TotalProcessorTime;
            foreach (MapTile Tile in TilesList)
            {
                Tile.Render();
            }
            //System.IO.File.AppendAllText("C:\\debugtv.txt", (Process.GetCurrentProcess().TotalProcessorTime - RenderingBegin).TotalMilliseconds+"ms\r\n");
        }

        //Change the player's possition
        public void ChangePlayerPos(float PosX, float PosY, float PosZ)
        {
            PlayerPos = GetWorldPos(PosX, PosY, PosZ);
        }

        //Get the WorldPosition's equivalent of absolute coordinates
        public WorldPosition GetWorldPos(float PosX, float PosY, float PosZ)
        {
            WorldPosition WorldPos = new WorldPosition();
            WorldPos.TilePosY = PosY; //Easiest : done !
            WorldPos.TileX = (int)PosX / (256 * MapTile.TileSize) - ((PosX < 0) ? 1 : 0);
            WorldPos.TileZ = (int)PosZ / (256 * MapTile.TileSize) - ((PosZ < 0) ? 1 : 0);
            WorldPos.TilePosX = PosX % (256 * MapTile.TileSize);
            WorldPos.TilePosZ = PosZ % (256 * MapTile.TileSize);

            return WorldPos;
        }

        //Check if there are tiles to load and load them
        public bool CheckLoadTiles(bool force = false) //Force : force the reload of all the tiles
        {
            System.Diagnostics.Debug.WriteLine("Start loadtiles (" + WorldPos.TileX + "," + WorldPos.TileZ + ")");
            if (force || ((PlayerPos.TileX != WorldPos.TileX) || (PlayerPos.TileZ != WorldPos.TileZ)))
            {
                //The player moved too much
                LoadingMapTiles = true;
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

                int TilesMoveX = PlayerPos.TileX - WorldPos.TileX;
                int TilesMoveZ = PlayerPos.TileZ - WorldPos.TileZ;

                TimeSpan CheckLandscapeBegin = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;

                //We create the new MapTile which will contain the new terrains
                MapTile[][] MapTiles_New = new MapTile[2 * RenderedTilesDistance + 1][];// = MapTiles = new MapTile[2 * RenderedTilesDistance + 1, 2 * RenderedTilesDistance + 1];
                LinkedList<MapTile> TilesList_New = new LinkedList<MapTile>();

                //We check all the new tiles to see if we have to create a new one or copy an old one
                for (short i = 0; i < (2 * RenderedTilesDistance + 1); i++)
                {
                    MapTiles_New[i] = new MapTile[2 * RenderedTilesDistance + 1];
                    for (short j = 0; j < (2 * RenderedTilesDistance + 1); j++)
                    {
                        int oldIndexX = i + TilesMoveX; //Correspondance to the old MapTiles
                        int oldIndexZ = j + TilesMoveZ; //Correspondance to the old MapTiles
                        if (!force && (((oldIndexX >= 0) && (oldIndexX <= 2 * RenderedTilesDistance)) && ((oldIndexZ >= 0) && (oldIndexZ <= 2 * RenderedTilesDistance))))
                        {
                            //The old index is valid, we just move the tile
                            MapTiles_New[i][j] = MapTiles[oldIndexX][oldIndexZ];
                        }
                        else
                        {
                            //New landscape, we create it
                            TimeSpan NewLandscapeBegin = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                            MapTiles_New[i][j] = new MapTile(new TilePosition(PlayerPos.TileX + i - RenderedTilesDistance, PlayerPos.TileZ + j - RenderedTilesDistance));
                            TimeSpan NewLandscapeEnd = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                            System.Diagnostics.Debug.WriteLine("New tile i:" + i + "(" + (PlayerPos.TileX + i - RenderedTilesDistance) + ");j:" + j + "(" + (PlayerPos.TileZ + j - RenderedTilesDistance) + ") - " + (NewLandscapeEnd - NewLandscapeBegin).TotalMilliseconds + " ms.");
                        }

                        if ((i - RenderedTilesDistance == 0) && (j - RenderedTilesDistance == 0))
                        {
                            //Player's tile
                            MapTiles_New[i][j].Landscape.SetTexture(GlobalVars.GameEngine.Globals.GetTex("LandTexture"), -1);
                            MapTiles_New[i][j].Landscape.SetTextureScale(3, 3, -1);
                        }
                        else
                        {
                            //Another tile
                            MapTiles_New[i][j].Landscape.SetTexture(GlobalVars.GameEngine.Globals.GetTex("LandTexture2"), -1);
                            MapTiles_New[i][j].Landscape.SetTextureScale(3, 3, -1);
                        }

                        TilePosition Position = new TilePosition(i - RenderedTilesDistance + PlayerPos.TileX, j - RenderedTilesDistance + PlayerPos.TileZ);

                        TilesHeightmapToLoad.Enqueue(Position, Position.TileDistanceTo(PlayerPos));

                        //LoadTileHeightmap(new TilePosition(i - RenderedTilesDistance, j - RenderedTilesDistance));
                        //LoadTileHeightmap2(MapTiles_New[i][j]);

                        WorldPosition SplatPosition = new WorldPosition(Position);
                        /*MapTiles_New[i][j].Landscape.AddSplattingTexture(GlobalVars.GameEngine.Globals.GetTex("SplattingTexture"), 1, 1, 1, 0, 0);
                        MapTiles_New[i][j].Landscape.ExpandSplattingTexture(GlobalVars.GameEngine.Globals.GetTex("SplattingAlphaTexture"), GlobalVars.GameEngine.Globals.GetTex("SplattingTexture"), 0, 0, 4, 4);
                        MapTiles_New[i][j].Landscape.SetSplattingEnable(true);*/
                        //AddSplattingToTile(SplatPosition, GlobalVars.GameEngine.Globals.GetTex("SplattingTexture"));

                        TilesList_New.AddLast(MapTiles_New[i][j]);
                    }
                }
                TimeSpan CheckLandscapeEnd = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;

                //We replace the old tiles with the new ones
                MapTiles = MapTiles_New;
                TilesList = TilesList_New;
                TimeSpan CopyMapTilesEnd = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;

                WorldPos.TileX = PlayerPos.TileX;
                WorldPos.TileZ = PlayerPos.TileZ;

                //DEBUG : add a mesh to each tile
                /*for (short i = 0; i < (2 * RenderedTilesDistance + 1); i++)
                {
                    for (short j = 0; j < (2 * RenderedTilesDistance + 1); j++)
                    {
                        if (!MapTiles[i][j].MeshesLoaded) //DEBUG
                        {
                            MapTiles[i][j].MeshesLoaded = true; //DEBUG

                            //Debug : we add a mesh
                            TVMesh newmesh = new TVMesh();
                            newmesh = GlobalVars.GameEngine.Scene.CreateMeshBuilder();
                            newmesh.CreateTeapot();
                            newmesh.SetScale(50, 50, 50);
                            newmesh.SetCullMode(CONST_TV_CULLING.TV_BACK_CULL);
                            AddMeshToTile(new WorldPosition(i - RenderedTilesDistance + PlayerPos.TileX, i - RenderedTilesDistance + PlayerPos.TileZ, 0, 0, 0), newmesh);
                        }
                    }
                }*/

                LoadingMapTiles = false;
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
                System.Diagnostics.Debug.WriteLine("Finished load new MapTiles (" + WorldPos.TileX + "," + WorldPos.TileZ + ") - " + (CheckLandscapeEnd - CheckLandscapeBegin).TotalMilliseconds + " ms|" + (CopyMapTilesEnd - CheckLandscapeEnd).TotalMilliseconds + " ms");
                return true;
            }
            System.Diagnostics.Debug.WriteLine("Nothing loaded");
            return false;
        }

        //Load a tile's heightmap (here uses the sin function)
        private void LoadTileHeightmap2(MapTile TileToLoad)
        {
            //return;
            //We don't use the debug sinus code
            if (TileToLoad != null)
            {
                //TimeSpan Start1 = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                MTV3D65.CONST_TV_LANDSCAPE_PRECISION precision = TileToLoad.Landscape.GetPrecision();
                int Vertices = (256 / GlobalVars.getTVPrecisionDivider(precision)) * TileToLoad.Landscape.GetLandWidth();
                float[] Height_Array = new float[Vertices * Vertices];
                for (int i = 0; i < Vertices; i++)
                {
                    for (int j = 0; j < Vertices; j++)
                    {
                        Height_Array[i * Vertices + j] = (float)Math.Sin(Math.Sqrt(Math.Pow(i * 2, 2) + Math.Pow(j * 2, 2)) / 25) * 10;
                    }
                }
                TileToLoad.Landscape.SetHeightArray(0, 0, Vertices, Vertices, Height_Array);
                //double Total1 = (System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime - Start1).TotalMilliseconds;

                //TimeSpan Start2 = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                /*for (int i = TileToLoad.TilePosition.TileX * 256 * MapTile.TileSize; i <= TileToLoad.TilePosition.TileX * 256 * MapTile.TileSize + 256 * MapTile.TileSize; i += 2)
                {
                    for (int j = TileToLoad.TilePosition.TileZ * 256 * MapTile.TileSize; j <= TileToLoad.TilePosition.TileZ * 256 * MapTile.TileSize + 256 * MapTile.TileSize; j = j + 1)
                    {
                        TileToLoad.Landscape.SetHeight(i, j, (float)Math.Sin(Math.Sqrt(i * i + j * j) / 25) * 10);
                    }
                }*/
                //double Total2 = (System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime - Start2).TotalMilliseconds;

                //Debug.WriteLine("Method 1 " + Total1 + "ms; Method 2 " + Total2 + "ms");
                //Method 1 31,2002ms; Method 2 3120,02ms

                TileToLoad.Landscape.FlushHeightChanges();
            }
        }

        //Second way of loading height
        private void LoadTileHeightmap(TilePosition Tilepos)
        {
            int tilei = Tilepos.TileX - WorldPos.TileX + RenderedTilesDistance;
            int tilej = Tilepos.TileX - WorldPos.TileX + RenderedTilesDistance;
            if ((tilei >= 0) && (tilei <= 2 * RenderedTilesDistance) && (tilej >= 0) && (tilej <= 2 * RenderedTilesDistance))
            {
                //If the tile is still used
                MapTiles[tilei][tilej].Landscape.SetHeight(1, 1, 50);
            }
        }

        //Background check made regularly to check if there are tiles to load
        private void CheckTilesToLoad()
        {
            while (LoadThreadWork)
            {
                if (!CheckLoadTiles())
                {
                    //If no tile loaded
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        //Get a position's height
        public float GetPositionHeight(WorldPosition Position)
        {
            int tilei = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            int tilej = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            if ((tilei >= 0) && (tilei <= 2 * RenderedTilesDistance) && (tilej >= 0) && (tilej <= 2 * RenderedTilesDistance))
            {
                return MapTiles[tilei][tilej].Landscape.GetHeight(Position.TilePosX, Position.TilePosZ);
            }
            else
            {
                return 0;
            }
        }

        //Add a mesh to a tile
        public void AddMeshToTile(WorldPosition Position, TVMesh Mesh)
        {
            int tilei = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            int tilej = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            if ((tilei >= 0) && (tilei <= 2 * RenderedTilesDistance) && (tilej >= 0) && (tilej <= 2 * RenderedTilesDistance))
            {
                Mesh.SetPosition(Position.TileX * MapTile.TileSize * 256 + Position.TilePosX, Position.TilePosY, Position.TileZ * MapTile.TileSize * 256 + Position.TilePosZ);
                MapTiles[tilei][tilej].Meshes.Add(Mesh);
            }
        }

        //Add splatting to a tile
        public void AddSplattingToTile(WorldPosition Position, int SplattingTexture)
        {
            int tilei = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            int tilej = Position.TileX - WorldPos.TileX + RenderedTilesDistance;
            if ((tilei >= 0) && (tilei <= 2 * RenderedTilesDistance) && (tilej >= 0) && (tilej <= 2 * RenderedTilesDistance))
            {
                MapTiles[tilei][tilej].Landscape.AddSplattingTexture(SplattingTexture, 1, 15, 15, 0, 0);
                //MapTiles[tilei][tilej].Landscape.ExpandSplattingTexture(IDAlpha, IDGrass, 0, 0, 4, 4);
                MapTiles[tilei][tilej].Landscape.SetSplattingEnable(true, -1, 1);
            }
        }

        /* Load tile heightmap */
        void LoadHeightmaps()
        {
            while (LoadHeightmapsThreadWork)
            {
                if (!LoadingMapTiles && (TilesHeightmapToLoad.Count != 0))
                {
                    //Heightmaps to load!
                    TilePosition CurrentPosition = TilesHeightmapToLoad.Dequeue();
                    int tilei = CurrentPosition.TileX - WorldPos.TileX + RenderedTilesDistance;
                    int tilej = CurrentPosition.TileZ - WorldPos.TileZ + RenderedTilesDistance;
                    if ((tilei >= 0) && (tilei <= 2 * RenderedTilesDistance) && (tilej >= 0) && (tilej <= 2 * RenderedTilesDistance) && !MapTiles[tilei][tilej].HeightmapLoaded)
                    {
                        //Valid position (tile still loaded)
                        LoadTileHeightmap2(MapTiles[tilei][tilej]);
                        MapTiles[tilei][tilej].HeightmapLoaded = true;

                        //Left
                        if (tilei > 0)
                        {
                            MapTiles[tilei][tilej].Landscape.FixSeams(MapTiles[tilei - 1][tilej].Landscape);
                            MapTiles[tilei - 1][tilej].Landscape.FixSeams(MapTiles[tilei][tilej].Landscape);
                        }
                        //Right
                        if (tilei < 2 * RenderedTilesDistance)
                        {
                            MapTiles[tilei][tilej].Landscape.FixSeams(MapTiles[tilei + 1][tilej].Landscape);
                            MapTiles[tilei + 1][tilej].Landscape.FixSeams(MapTiles[tilei][tilej].Landscape);
                        }
                        //Top
                        if (tilej > 0)
                        {
                            MapTiles[tilei][tilej].Landscape.FixSeams(MapTiles[tilei][tilej - 1].Landscape);
                            MapTiles[tilei][tilej - 1].Landscape.FixSeams(MapTiles[tilei][tilej].Landscape);
                        }
                        //Bottom
                        if (tilej < 2 * RenderedTilesDistance)
                        {
                            MapTiles[tilei][tilej].Landscape.FixSeams(MapTiles[tilei][tilej + 1].Landscape);
                            MapTiles[tilei][tilej + 1].Landscape.FixSeams(MapTiles[tilei][tilej].Landscape);
                        }

                        //DEBUG : splatting
                        MapTiles[tilei][tilej].Landscape.AddSplattingTexture(GlobalVars.GameEngine.Globals.GetTex("SplattingTexture"), 1, 1, 1, 0, 0);
                        MapTiles[tilei][tilej].Landscape.ExpandSplattingTexture(GlobalVars.GameEngine.Globals.GetTex("SplattingAlphaTexture"), GlobalVars.GameEngine.Globals.GetTex("SplattingTexture"), 0, 0, 4, 4);
                        MapTiles[tilei][tilej].Landscape.SetSplattingEnable(true);

                        Debug.WriteLine("Heightmap loaded (" + CurrentPosition.TileX + ";" + CurrentPosition.TileZ + ")");
                    }
                }
                else
                {
                    //Nothing to load, we sleep a bit
                    Thread.Sleep(1000);
                }
            }
        }

        /* Load tile meshes */


        public int GetTilePosX(int i)
        {
            return WorldPos.TileX + (i - RenderedTilesDistance);
        }

        public int GetTilePosZ(int j)
        {
            return WorldPos.TileZ + (j - RenderedTilesDistance);
        }
    }

}
