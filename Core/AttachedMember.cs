﻿// Copyright (c) 2019 Rayaan Ajouz, Bouwen met Staal. Please see the LICENSE file	
// for details. All rights reserved. Use of this source code is governed by a	
// Apache-2.0 license that can be found in the LICENSE file.	
using System;
using System.Collections.Generic;
using System.Linq;

namespace KarambaIDEA.Core
{
    public class AttachedMember
    {
        public Element element;
        public bool isStartPoint;
                
        public Line ideaLine;
        public int ideaOperationID;
        public Vector distanceVector;
        public bool platefailure = true;

        

        public double MaxAxialLoad()
        {
            List<double> Nloads = new List<double>();
            foreach (LoadCase lc in this.element.project.loadcases)
            {
                foreach (LoadsPerLine loadsPerLine in lc.loadsPerLines)
                {
                    if (loadsPerLine.element == this.element)
                    {
                        if (this.isStartPoint == true)
                        {
                            double N = loadsPerLine.startLoad.N;
                            N = Math.Abs(N);
                            Nloads.Add(N);
                        }
                        else
                        {
                            double N = loadsPerLine.endLoad.N;
                            N = Math.Abs(N);
                            Nloads.Add(N);
                        }
                    }
                }
            }
            return Nloads.Max();
        }
        /// <summary>
        /// Max stress in local z direction
        /// TODO extend to bidirectional stress determination
        /// </summary>
        /// <returns>maxiumum stress in N/mm2</returns>
        public double Maxstress()
        {
            List<double> stresses = new List<double>();
            foreach (LoadCase lc in this.element.project.loadcases)
            {
                foreach (LoadsPerLine loadsPerLine in lc.loadsPerLines)
                {
                    if (loadsPerLine.element == this.element)
                    {
                        double N;
                        double My;
                        if (this.isStartPoint == true)
                        {
                            N = loadsPerLine.startLoad.N*1000;//kN to N
                            My = loadsPerLine.startLoad.My*1000000;//kNm to Nmm
                        }
                        else
                        {
                            N = loadsPerLine.endLoad.N*1000;//kN to N
                            My = loadsPerLine.endLoad.My*1000000;//kNm to Nmm
                        }
                        CrossSection c = this.element.crossSection;
                        double sigma1 = N / c.Area() + My / c.MomentOfResistance();
                        double sigma2 = N / c.Area() - My / c.MomentOfResistance();
                        double sigma = Math.Max(Math.Abs(sigma1), Math.Abs(sigma2));
                        stresses.Add(sigma);
                    }
                }
            }
            return stresses.Max();
        }
    }

    public class BearingMember : AttachedMember
    {
        public Nullable<bool> isSingle;
        public BearingMember()
        {

        }
        public BearingMember(Element _element, Vector _distancevector, bool _isStartPoint, Line _idealine, Nullable<bool> _isSingle = null)
        {
            this.element = _element;
            this.distanceVector = _distancevector;
            this.isStartPoint = _isStartPoint;
            this.ideaLine = _idealine;
            this.isSingle = _isSingle;
        }
    }

    public class ConnectingMember : AttachedMember
    {
        public Weld flangeWeld = new Weld();
        public Weld webWeld = new Weld();
        public double localEccentricity;
        public double angleWithBear = new double();

        public ConnectingMember(Element _element, Vector _distancevector, bool _isStartPoint, Line _idealine, double _localEccentricity)
        {
            this.element = _element;
            this.distanceVector = _distancevector;
            this.isStartPoint = _isStartPoint;
            this.ideaLine = _idealine;

            this.localEccentricity = _localEccentricity;

            SetDefaultWeldType();
        }
        /// <summary>
        /// Fillet welds are assigned to Hollow sections, double fillet welds are assigned to Isections
        /// </summary>
        public void SetDefaultWeldType()
        {
            if (this.element.crossSection.shape == CrossSection.Shape.ISection)
            {
                this.flangeWeld.weldType = Weld.WeldType.DoubleFillet;
                this.webWeld.weldType = Weld.WeldType.DoubleFillet;
            }
            else
            {
                this.flangeWeld.weldType = Weld.WeldType.Fillet;
                this.webWeld.weldType = Weld.WeldType.Fillet;
            }
        }
        /// <summary>
        /// In this simple method Welding volume is generated by multipling the weldsurface of a weld 
        /// with the perimeter of the cross-section
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        public static double CalculateWeldVolumeSimplified(ConnectingMember con)
        {
            CrossSection cross = con.element.crossSection;
            double weldVolume = new double();
            if (cross.shape == CrossSection.Shape.CHSsection)
            {
                double radius = 0.5 * cross.height;
                double perimeter = 2 * Math.PI * radius;
                weldVolume = perimeter * Math.Pow(con.webWeld.Size, 2);
            }

            if (cross.shape == CrossSection.Shape.RHSsection)
            {
                double perimeter = 2 * cross.width + 2 * cross.height;
                weldVolume = perimeter * Math.Pow(con.webWeld.Size, 2);
            }
            if (cross.shape == CrossSection.Shape.ISection)
            {
                double weldVolumeWeb = 2 * cross.height * Math.Pow(con.webWeld.Size, 2);
                double weldVolumeFlange = 4 * cross.width * Math.Pow(con.flangeWeld.Size, 2);
                weldVolume = weldVolumeWeb + weldVolumeFlange;
            }
            else
            {
                //TODO: include warning, cross-sections not recognized
            }
            return weldVolume;
        }
        /// <summary>
        /// This method transforms the global eccentricity to a local eccentricity.
        /// The local eccentricity is the shortest distance of the connecting point of a member
        /// to the connection point.
        /// See  p.79, Optimising production costs of steel trusses, R. ajouz 
        /// https://repository.tudelft.nl/islandora/object/uuid%3A8e8835b3-171c-471e-8ff4-e9c3e5c8b148
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        static public double LocalEccentricity(Point c, Point a, Vector dir)
        {
            double numerator = Math.Sqrt(Math.Pow((c.Y - a.Y) * dir.Z - (c.Z - a.Z) * dir.Y, 2) + Math.Pow((c.X - a.X) * dir.Z - (c.Z - a.Z) * dir.X, 2) + Math.Pow((c.X - a.X) * dir.Y - (c.Y - a.Y) * dir.X, 2));
            double denumerator = Math.Sqrt(Math.Pow(dir.X, 2) + Math.Pow(dir.Y, 2) + Math.Pow(dir.Z, 2));

            return numerator / denumerator;
        }
    }


}

