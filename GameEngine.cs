/*
 * Copyright 2011-2012 Antonin Lenfant (Aweb)
 * 
 * This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2 of the License, or (at your option) any later version.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MTV3D65; //TV6.5
using System.Windows.Forms; //Application

namespace WorldEngine
{
    public class GameEngine
    {
        public TVEngine TV;                        // We declare TrueVision8.
        public TVGlobals Globals;                  // We declare the Globals , usefull functions there
        public TVLandscape Land;                   // We declare the landscape
        public TVTextureFactory TextureFactory;    // The texture factory. Will hold all the textures needed in our project.
        public TVAtmosphere Atmos;                 // New : to enable fog in our project, we have to use the TVAtmosphere object like the sky.
        public TVGraphicEffect GraphicFX;          // The TVGraphicEffect class let us to make some interesting screen effects like fade in and fade out.
        public TVRenderSurface RenderSurf1;
        public TVRenderSurface RenderSurf2;
        public TVMesh WaterMesh;
        public TV_PLANE WPlane;
        public float sngWaterHeight;
        public TVScene Scene;                      // We the declare the scene
        //public TVSceneManager.SceneObject Scene;
        public TVInputEngine InputEngine;          // We declare the input engine.
        public bool DoLoop = true;                 // The loop.

        private IntPtr GameHandle;                       //Form where the game will be displayed

        // The real hero here : the class managing the world map
        public WorldMap WMap;

        // We are going to use camera (point of view) angles, as well as the camera position and look at vectors.
        float sngPositionX;
        float sngPositionY;
        float sngPositionZ;
        float snglookatX;
        float snglookatY;
        float snglookatZ;
        float sngAngleX;
        float sngAngleY;
        // We could have done this in many ways, but we added some smoothing to the movement se we need to declare two additional variables.
        float sngWalk;
        float sngStrafe;

        // Mouse input variables
        int tmpMouseX;
        int tmpMouseY;
        bool tmpMouseB1;
        bool tmpMouseB2;
        bool tmpMouseB3;
        bool tmpMouseB4;
        int tmpMouseScrollOld;
        int tmpMouseScrollNew;

        public GameEngine(IntPtr GameHandle)
        {
            this.GameHandle = GameHandle;
        }

        public void Init()
        {
            //bool DebugToConsole = true;

            // We have to create the TV object before anything else.
            TV = new TVEngine();

            // Set the search directory of the objects, textures, ...
            TV.SetSearchDirectory(Application.StartupPath);

            // We put the debug file in the app directory
            //TV.SetDebugFile(Application.StartupPath + "\\WorldEngine-Debug.txt");
            //TV.SetDebugMode(true, true, DebugToConsole);
            TV.SetDebugMode(false, false, false, false);

            // We initialize TV in the picture box of the form.
            TV.Init3DWindowed(GameHandle);

            // We want to see the FPS.
            TV.DisplayFPS(true);

            // We create the TVGlobals object.
            Globals = new TVGlobals();

            // We create the input object.
            InputEngine = new TVInputEngine();
            InputEngine.Initialize();

            // New : we create the graphic object so we can add some fog.
            GraphicFX = new TVGraphicEffect();

            // We create the scene (the world).
            Scene = new TVScene();
            //Scene.SetViewFrustum(45, 20000);
            //Scene.SetViewFrustum(45, 20);

            // We create the atmosphere class
            Atmos = new TVAtmosphere();

            // As said above, we need to create a new object which will
            // hold all the textures needed for our land.
            TextureFactory = new TVTextureFactory();

            // We load the sky texture.
            TextureFactory.LoadTexture("Media\\sky\\sunset\\up.jpg", "SkyTop", -1, -1);
            TextureFactory.LoadTexture("Media\\sky\\sunset\\down.jpg", "SkyBottom", -1, -1);
            TextureFactory.LoadTexture("Media\\sky\\sunset\\left.jpg", "SkyLeft", -1, -1);
            TextureFactory.LoadTexture("Media\\sky\\sunset\\right.jpg", "SkyRight", -1, -1);
            TextureFactory.LoadTexture("Media\\sky\\sunset\\front.jpg", "SkyFront", -1, -1);
            TextureFactory.LoadTexture("Media\\sky\\sunset\\back.jpg", "SkyBack", -1, -1);

            // We set the sky textures.
            Atmos.SkyBox_SetTexture(Globals.GetTex("SkyFront"), Globals.GetTex("SkyBack"), Globals.GetTex("SkyLeft"), Globals.GetTex("SkyRight"), Globals.GetTex("SkyTop"), Globals.GetTex("SkyBottom"));
            Atmos.SkyBox_Enable(true);

            // New : the land generation. This is so much fun because it's
            // so simple! You load a texture as a height map, the engine
            // does the rest. But before this, we create the land object.
            Land = new TVLandscape();
            //Land = Scene.CreateLandscape("Land");

            // Generate the height of the land from the grayscale of the image.
            //Land.GenerateTerrain("Media\\heightmap.jpg", CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_LOW, 8, 8, -1024, 0, -1024, true);
            //Land.CreateEmptyTerrain(CONST_TV_LANDSCAPE_PRECISION.TV_PRECISION_LOW, 1, 1, -128, 0, -128);

            // Because we don't want to have mountains with height that get to
            // the clouds, we adjust the height (Y) factor.
            //Land.SetScale(1, 0.7f, 1);

            // Then, we load the land texture.
            TextureFactory.LoadTexture("Media\\dirtandgrass.jpg", "LandTexture", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);
            TextureFactory.LoadTexture("Media\\dirtandgrass2.jpg", "LandTexture2", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);

            //...The splatting texture...
            TextureFactory.LoadTexture("Media\\grass.dds", "SplattingTexture", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);
            TextureFactory.LoadTexture("Media\\grassa.dds", "SplattingAlphaTexture", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);

            // We assign a texture to that land.
            //Land.SetTexture(Globals.GetTex("LandTexture"), -1);
            //Land.SetTextureScale(3, 3, -1);

            // New : the sun. We have to place the sun in the world. Just like the
            // sky box, the sun is attached to the camera position vector. You will
            // never notice it until you start playing really badly with the
            // properties of the sun. Let's start by loading a texture for it.
            TextureFactory.LoadTexture("Media\\sun.jpg", "Sun", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);

            // Then, initialize it by placing it via a vector.
            Atmos.Sun_SetTexture(Globals.GetTex("Sun"));
            Atmos.Sun_SetBillboardSize(1);
            Atmos.Sun_SetPosition(-1000f, 570f, 0f);
            Atmos.Sun_Enable(true);

            // New : To add extra visual effects, we add a lens flare effect. For
            // this, we have to load some cirles that will be used to simulate
            // the flare effect.
            TextureFactory.LoadTexture("Media\\flare1.jpg", "Flare1", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);
            TextureFactory.LoadTexture("Media\\flare2.jpg", "Flare2", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);
            TextureFactory.LoadTexture("Media\\flare3.jpg", "Flare3", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);
            TextureFactory.LoadTexture("Media\\flare4.jpg", "Flare4", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_NO, true);

            // Initialize the lens flares.
            Atmos.LensFlare_SetLensNumber(4);
            Atmos.LensFlare_Enable(true);
            Atmos.LensFlare_SetLensParams(0, Globals.GetTex("Flare1"), 2 * 5f, 40, Globals.RGBA(1f, 1f, 1f, 0.5f), Globals.RGBA(1f, 1f, 1f, 0.5f));
            Atmos.LensFlare_SetLensParams(1, Globals.GetTex("Flare2"), 2 * 1f, 18, Globals.RGBA(1f, 1f, 1f, 0.5f), Globals.RGBA(1f, 1f, 1f, 0.5f));
            Atmos.LensFlare_SetLensParams(2, Globals.GetTex("Flare3"), 2 * 1.8f, 15, Globals.RGBA(1f, 1f, 1f, 0.5f), Globals.RGBA(0.7f, 1f, 1f, 0.5f));
            Atmos.LensFlare_SetLensParams(3, Globals.GetTex("Flare4"), 2 * 1f, 6, Globals.RGBA(1f, 0.1f, 0f, 0.5f), Globals.RGBA(0.5f, 1f, 1f, 0.5f));

            // New : also for fun, we add water. We start by loading the
            // water tetxure...

            sngWaterHeight = -1; //60
            TextureFactory.LoadTexture("Media\\water.bmp", "Water");

            WaterMesh = Scene.CreateMeshBuilder();
            WaterMesh.AddFloor(Globals.GetTex("Water"), -700, 0, (8 * 256) - 700, 8 * 256, sngWaterHeight, 1, 1, false);
            RenderSurf1 = Scene.CreateRenderSurface(256, 256, true);
            RenderSurf2 = Scene.CreateRenderSurface(256, 256, true);
            WPlane.Dist = -sngWaterHeight;
            WPlane.Normal = new TV_3DVECTOR(0, 1, 0);
            RenderSurf1.SetBackgroundColor(355);
            GraphicFX.SetWaterReflection(WaterMesh, RenderSurf1, RenderSurf2, 0, WPlane);

            // New : for fun, we will also add some clouds, just over the water
            // to give a creepy fog effect. Let's start by loading the clouds textures.
            TextureFactory.LoadTexture("Media\\cloud1.dds", "Clouds", -1, -1, CONST_TV_COLORKEY.TV_COLORKEY_BLACK, true);

            // Then, set the land's clouds.
            Atmos.Clouds_Create(1, 1024, 1024);
            Atmos.Clouds_SetLayerParameters(0, 300, Globals.GetTex("Clouds"));
            Atmos.Clouds_SetLayerAnimation(0, true, 0.01f, 0.01f);


            // We set the camera vectors (position and look at) and angles.
            sngPositionX = 0f;
            sngPositionY = 20f;
            sngPositionZ = 0f;
            snglookatX = 0f;
            snglookatY = 20f;
            snglookatZ = 50f;
            sngAngleX = 0f;
            sngAngleY = 0f;

            // We set the initial values of movement
            sngWalk = 0f;
            sngStrafe = 0f;

            // Set the viewing distance
            Scene.SetViewFrustum(60f, 900f); //"random" values, TODO : Check for better ones ?
            //http://www.truevision3d.com/forums/tv3d_sdk_63/about_tilemap-t3865.0.html;prev_next=prev

            // We pop the form over everything else.

            //We create the world map
            WMap = new WorldMap(Scene);

            // We start the main loop. You can't create the MainLoop by using the
            // toolbox buttons, nor by clicking on the form "Form1" : you have to
            // create it by yourself. It's not as hard as it may sound...
            DoLoop = true;

            TV.EnableProfiler(true, false);

            Main_Loop();
        }

        private void Main_Loop()
        {

            // Okay, we start the main loop here. We are going to loop over
            // and over until the user click on the "Quit" button and by this,
            // change the value of DoLoop to false.

            // We loop all of this until the DoLoop isn't True.
            while (DoLoop == true)
            {
                // Let us the capacity to use buttons of the form.
                System.Windows.Forms.Application.DoEvents();

                // New : We moved the movement code in an other sub to make
                // the code clearer.
                Check_Input();

                if (InputEngine.IsKeyPressed(CONST_TV_KEY.TV_KEY_Z))
                {
                    Scene.SetRenderMode(CONST_TV_RENDERMODE.TV_LINE);
                }
                else
                {
                    Scene.SetRenderMode(CONST_TV_RENDERMODE.TV_SOLID);
                }

                // New : We moved the checking of maximum camera "look at" and
                // also the camera movement smoothing in an other sub too.
                Check_Movement();

                //render surfaces before tv3d.clear or you get funkey results
                RenderSurf1.StartRender(false);
                Atmos.Atmosphere_Render();
                //Land.Render();
                RenderSurf1.EndRender();
                RenderSurf2.StartRender(false);
                Atmos.Atmosphere_Render();
                //Land.Render();
                RenderSurf2.EndRender();

                // Clear the the last frame.
                TV.Clear(false);

                // New : if we are below the waterheight, this means the we are
                // underwater. To give a cool underwater effect, we will add fog.
                // If we are over the ground, then don't add the fog but render
                // the lens flare.
                /*
                if (sngPositionY < sngWaterHeight)
                {
                    //' Render a blue fog to simulate under water.
                    Atmos.Fog_Enable(true);
                    Atmos.Fog_SetColor(0f, 0.4f, 0.5f);
                    Atmos.Fog_SetParameters(0f, 0f, 0.01f);
                    Atmos.Fog_SetType(CONST_TV_FOG.TV_FOG_EXP, CONST_TV_FOGTYPE.TV_FOGTYPE_RANGE);
                    Atmos.LensFlare_Enable(false);
                }
                else
                {*/
                // New : we have to render the lens flare.
                Atmos.LensFlare_Enable(true);
                Atmos.Fog_Enable(false);
                /*Atmos.Fog_Enable(true);
                Atmos.Fog_SetColor(1, 1, 1);
                Atmos.Fog_SetParameters(50f, 100f, 0.01f);
                Atmos.Fog_SetType(CONST_TV_FOG.TV_FOG_LINEAR, CONST_TV_FOGTYPE.TV_FOGTYPE_VERTEX);*/
                //}

                // New have to render the sky, the sun and lens flares
                Atmos.Atmosphere_Render();

                // New : we have to render the landscape.
                //Land.Render();
                WMap.Render();
                //Scene.RenderAll(false);

                // We render all the 3D objects contained in the scene.
                //Scene.RenderAllMeshes(true);

                // We display everything that we have rendered
                TV.RenderToScreen();

                WorldPosition PlayerPos = WMap.GetPlayerPosition();
                GlobalVars.GameForm.Text = "Pos:" + sngPositionX + " (" + PlayerPos.TileX + ");" + sngPositionY + ";" + sngPositionZ + " (" + PlayerPos.TileZ + ") + (" + PlayerPos.TilePosX + ";" + PlayerPos.TilePosY + ";" + PlayerPos.TilePosZ + ")";
                //GlobalVars.GameForm.Text = Scene.GetTriangleNumber().ToString();
            }

            // We ask to quit.
            this.Quit();


        }
        private void Check_Input()
        {
            // Check if we pressed the UP arrow key, if so, then we are
            // walking forward.
            if (InputEngine.IsKeyPressed(CONST_TV_KEY.TV_KEY_UP))
            {
                sngWalk = 1f;
            }
            // If we are not walking forward, maybe we are walking backward
            //' by using the DOWN arrow? If so, set walk speed to negative.
            else if (InputEngine.IsKeyPressed(CONST_TV_KEY.TV_KEY_DOWN))
            {
                sngWalk = -1;
            }

            // Check if we pressed the LEFT arrow key, if so, then strafe
            // on the left.
            if (InputEngine.IsKeyPressed(CONST_TV_KEY.TV_KEY_LEFT))
            {
                sngStrafe = 1f;
            }
            // If we are not strafing left, maybe we want to strafe to the
            // right, using the RIGHT arrow? If so, set strafe to negative.
            else if (InputEngine.IsKeyPressed(CONST_TV_KEY.TV_KEY_RIGHT))
            {
                sngStrafe = -1f;
            }

            // Actual value to old mouse scroller value.
            tmpMouseScrollOld = tmpMouseScrollNew;

            // Get the movement of the mouse.
            InputEngine.GetMouseState(ref tmpMouseX, ref tmpMouseY, ref tmpMouseB1, ref tmpMouseB2, ref tmpMouseB3, ref tmpMouseB4, ref tmpMouseScrollNew);

            // Update the camera angles.
            if (tmpMouseB2 == true)
            {
                sngAngleX = sngAngleX - ((float)tmpMouseY / 100);
                sngAngleY = sngAngleY - ((float)tmpMouseX / 100);
            }

        }
        private void Check_Movement()
        {

            // Simple check of the mouse.
            if (sngAngleX > 1.3)
                sngAngleX = 1.3F;
            if (sngAngleX < -1.3)
                sngAngleX = -1.3F;
            // Okay, now for the smothing of the movement... Update
            // the forward and backward (walk) movement.
            if (sngWalk > 0)
            {
                sngWalk = sngWalk - 0.005F * (float)TV.TimeElapsed();
                if (sngWalk < 0)
                    sngWalk = 0;
            }
            else
            {
                sngWalk = sngWalk + 0.005F * (float)TV.TimeElapsed();
                if (sngWalk > 0)
                    sngWalk = 0;
            }

            // Now, we update the left and right (strafe) movement.
            if (sngStrafe > 0)
            {
                sngStrafe = sngStrafe - 0.005F * (float)TV.TimeElapsed();
                if (sngStrafe < 0)
                    sngStrafe = 0;
            }
            else
            {
                sngStrafe = sngStrafe + 0.005F * (float)TV.TimeElapsed();
                if (sngStrafe > 0)
                    sngStrafe = 0;
            }

            // Update the vectors using the angles and positions.
            sngPositionX = sngPositionX + (float)(System.Math.Cos((double)sngAngleY) * sngWalk / 5 * TV.TimeElapsed()) + (float)(System.Math.Cos((double)sngAngleY + 3.141596 / 2) * sngStrafe / 5 * TV.TimeElapsed());
            sngPositionZ = sngPositionZ + (float)(System.Math.Sin((double)sngAngleY) * sngWalk / 5 * TV.TimeElapsed()) + (float)(System.Math.Sin((double)sngAngleY + 3.141596 / 2) * sngStrafe / 5 * TV.TimeElapsed());

            // New : because we are using a landscape with up and down, we
            // can't let the camera at the same height. We want the camera to
            // follow the height of the map, so we use the "get height". Also,
            // because we want to have the effect that we are not a mouse,
            // we will add some height to the height returned...
            //sngPositionY = Land.GetHeight(sngPositionX, sngPositionZ) + 10;
            sngPositionY = WMap.GetPositionHeight(WMap.GetWorldPos(sngPositionX, 0, sngPositionZ)) + 10;

            // We update the look at position.
            snglookatX = sngPositionX + (float)System.Math.Cos((double)sngAngleY);
            snglookatY = sngPositionY + (float)System.Math.Tan((double)sngAngleX);
            snglookatZ = sngPositionZ + (float)System.Math.Sin((double)sngAngleY);

            // With the new values of the camera vectors (position and
            // look at), we update the scene's camera.
            Scene.SetCamera(sngPositionX, sngPositionY, sngPositionZ, snglookatX, snglookatY, snglookatZ);

            //We set it in the engine
            GlobalVars.GameEngine.WMap.SetPlayerPosition(sngPositionX, sngPositionY, sngPositionZ);
        }

        public void Dispose()
        {
            TV = null;
            DoLoop = false;
        }

        public void Quit()
        {
            //first of all, we close the world map manager
            WMap.Quit();

            // We want to quit the project, so we start by desroyng
            // the texture factory.
            TextureFactory = null;

            // We destroy the land and Atmos object.
            Land = null;
            Atmos = null;
            // Don't forget to destroy the inputengine object...
            InputEngine = null;

            // Dispose of the GFX effect class
            GraphicFX = null;

            // Then, we destroy the scene object.
            Scene = null;

            // Dispose the Globals Object
            Globals = null;
            // We finish the frenetic destroy with the TV object.
            TV = null;

        }
    }
}
