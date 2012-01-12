using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using MTV3D65;

namespace WorldEngine
{
    class MapTile
    {
        //Contants
        public const int TileSize = 4;

        public TVLandscape Landscape = new TVLandscape();
        public TilePosition TilePosition;

        public List<TVMesh> Meshes = new List<TVMesh>();

        public bool HeightmapLoaded = false;
        public bool MeshesLoaded = false;

        public MapTile(TilePosition TilePosition)
        {
            this.TilePosition = TilePosition;
            // We create the tile
            Landscape = GlobalVars.GameEngine.Scene.CreateLandscape("Land" + TilePosition.TileX + ";" + TilePosition.TileZ);
            Landscape.GenerateTerrain(null, CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_HIGH, TileSize, TileSize, TilePosition.TileX * 256 * TileSize, 0, TilePosition.TileZ * 256 * TileSize);
            //Landscape.CreateEmptyTerrain(CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_LOW, TileSize, TileSize, TilePosition.TileX * 256 * TileSize, 0, TilePosition.TileZ * 256 * TileSize);
            //Landscape.CreateEmptyTerrain(CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_LOW, TileSize, TileSize, TilePosition.TileX * 256 + (TileSize - 1) * 256, 0, TilePosition.TileZ * 256 + (TileSize - 1) * 256);
            
            //Latest :
            //Landscape.EnableLOD(true, 100, CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_ULTRA_LOW, 0, true);
            Landscape.SetCullMode(CONST_TV_CULLING.TV_BACK_CULL);
            Landscape.SetCollisionEnable(false);
            System.Diagnostics.Debug.WriteLine("  Create landscape X:" + TilePosition.TileX * TileSize * 256 + "; Y:0; Z:" + TilePosition.TileZ * TileSize * 256 + "!");
        }

        //Render the tile : Landscape and meshes
        public void Render()
        {
            TimeSpan RenderingBegin = Process.GetCurrentProcess().TotalProcessorTime;
            Landscape.Render();
            //System.IO.File.AppendAllText("C:\\debugtv.txt", "LandRender:"+ (Process.GetCurrentProcess().TotalProcessorTime - RenderingBegin).TotalMilliseconds + "ms\r\n");
            //Debug.WriteLine("LandRender:" + (Process.GetCurrentProcess().TotalProcessorTime - RenderingBegin).TotalMilliseconds + "ms");
            Meshes.ForEach(delegate(TVMesh mesh) { mesh.Render(); });
            //System.IO.File.AppendAllText("C:\\debugtv.txt", "TotalTileRender:"+(Process.GetCurrentProcess().TotalProcessorTime - RenderingBegin).TotalMilliseconds + "ms\r\n");
        }

        ~MapTile() //Destructor
        {
            if (Landscape != null)
            {
                    //Landscape.DeleteAll();
                    //Landscape.Destroy();
                    Landscape = null; //We destroy the landscape
            }
        }
    }
}
