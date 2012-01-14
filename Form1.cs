/*
 * Copyright 2011-2012 Antonin Lenfant (Aweb)
 * 
 * This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2 of the License, or (at your option) any later version.
 */

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using WorldEngine;
using System.Threading;

namespace WorldEngine
{
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;

		private System.ComponentModel.Container components = null;

        public Form1()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{


			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(8, 8);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1128, 619);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1040, 633);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(96, 40);
            this.button1.TabIndex = 1;
            this.button1.Text = "Quit";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(1148, 685);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TrueVision3D - A08 A complete Landscape";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
            GlobalVars.GameForm = this;
            GlobalVars.GameEngine = new GameEngine(this.pictureBox1.Handle);

            this.Show();
            this.Focus();

            GlobalVars.GameEngine.Init();
            pictureBox1.Focus();
        }

		private void Main_Quit()
		{
			this.Close();
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			
			// if the user clicks on Quit, just unload all and go away...
			GlobalVars.GameEngine.DoLoop = false;
            this.Close(); //We close the window
		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            GlobalVars.GameEngine.DoLoop = false;
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GlobalVars.GameEngine.WMap.CheckLoadTiles();
        }
	}
}
