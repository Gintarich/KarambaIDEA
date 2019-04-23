﻿using KarambaIDEA.Core;
using KarambaIDEA.IDEA;
using IdeaRS.Connections.Data;
using IdeaRS.OpenModel.Connection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using KarambaIDEA;


namespace TestCON
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            KarambaIDEA.MainWindow mainWindow = new MainWindow();
            KarambaIDEA.TestFrameworkJoint fj = new TestFrameworkJoint();

            //hieronder testjoint definieren
            Joint joint = fj.Testjoint7();
            //Joint joint = fj.Testjoint5();
            joint.project.CreateFolder();
            //min lasafmeting uitzetten bij Grasshopper
            joint.project.minthroat = 1.0;

            mainWindow.Test(joint);
        }
        
    }
}
