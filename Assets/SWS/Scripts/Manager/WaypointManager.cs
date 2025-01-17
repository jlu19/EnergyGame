/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace SWS
{
    /// <summary>
    /// The editor part of this class allows you to create paths in 2D or 3D space.
    /// At runtime, it manages path instances for easy lookup of references.
    /// <summary>
    public class WaypointManager : MonoBehaviour
    {
        /// <summary>
        /// Stores all path components in the scene along with their name.
        /// You can access them by calling WaypointManager.Paths["path name"].
        /// <summary>
        public static readonly Dictionary<string, PathManager> Paths = new Dictionary<string, PathManager>();


        //execute this before any other Start() or Update() function
        //since we need the data of all paths before we call them
        void Awake()
        {
            //try to add each child to the waypoint manager
            foreach (Transform path in transform)
            {
                AddPath(path.gameObject);
            }

            //http://dotween.demigiant.com/documentation.php#init
            //initialize DOTween immediately instead than having it being
            //automatically initialized when the first Tweener is created.
            //set up specific settings in the DOTween utility panel!
            DOTween.Init();
        }


        /// <summary>
        /// Adds the gameobject to the path dictionary, if it contains a path component.
        /// <summary>
        public static void AddPath(GameObject path)
        {
            //check if the path has been instantiated,
            //then remove the clone naming scheme
            if (path.name.Contains("(Clone)"))
                path.name = path.name.Replace("(Clone)", "");

            //check if our dictionary already contains this path
            if (Paths.ContainsKey(path.name))
            {
                Debug.LogWarning("Called AddPath() but Scene already contains Path " + path.name + ".");
                return;
            }

            //try to get PathManager component
            PathManager pathMan = path.GetComponentInChildren<PathManager>();

            //try to add path name and its manager reference to our dictionary
            if (pathMan)
                Paths.Add(path.name, pathMan);
            else
            {
                Debug.LogWarning("Called AddPath() but Transform " + path.name + " has no Path Component attached.");
                return;
            }
        }


        //static dictionaries keep their variables between scenes,
        //we don't want that to happen - clear the path dictionary
        //whenever this object gets destroyed (e.g. on scene change)
        void OnDestroy()
        {
            Paths.Clear();
        }


        /// <summary>
        /// Draws straight gizmo lines between waypoints.
        /// <summary>
        public static void DrawStraight(Vector3[] waypoints)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
                Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
        }


        /// <summary>
        /// Draws curved gizmo lines between waypoints, taken and modified from HOTween.
        /// <summary>
        //http://code.google.com/p/hotween/source/browse/trunk/Holoville/HOTween/Core/Path.cs
        public static void DrawCurved(Vector3[] waypoints)
        {
            //helper array for curved paths, includes control points for waypoint array
            Vector3[] gizmoPoints = new Vector3[waypoints.Length + 2];
            waypoints.CopyTo(gizmoPoints, 1);
            gizmoPoints[0] = waypoints[1];
            gizmoPoints[gizmoPoints.Length - 1] = gizmoPoints[gizmoPoints.Length - 2];

            Vector3[] drawPs;
            Vector3 currPt;

            //store draw points
            int subdivisions = gizmoPoints.Length * 10;
            drawPs = new Vector3[subdivisions + 1];
            for (int i = 0; i <= subdivisions; ++i)
            {
                float pm = i / (float)subdivisions;
                currPt = GetPoint(gizmoPoints, pm);
                drawPs[i] = currPt;
            }

            //draw path
            Vector3 prevPt = drawPs[0];
            for (int i = 1; i < drawPs.Length; ++i)
            {
                currPt = drawPs[i];
                Gizmos.DrawLine(currPt, prevPt);
                prevPt = currPt;
            }
        }


        /// <summary>
        /// Gets the point on the curve at a given percentage (0-1). Taken and modified from HOTween.
        /// <summary>
        //http://code.google.com/p/hotween/source/browse/trunk/Holoville/HOTween/Core/Path.cs
        public static Vector3 GetPoint(Vector3[] gizmoPoints, float t)
        {
            int numSections = gizmoPoints.Length - 3;
            int tSec = (int)Mathf.Floor(t * numSections);
            int currPt = numSections - 1;
            if (currPt > tSec)
            {
                currPt = tSec;
            }
            float u = t * numSections - currPt;

            Vector3 a = gizmoPoints[currPt];
            Vector3 b = gizmoPoints[currPt + 1];
            Vector3 c = gizmoPoints[currPt + 2];
            Vector3 d = gizmoPoints[currPt + 3];

            return .5f * (
                           (-a + 3f * b - 3f * c + d) * (u * u * u)
                           + (2f * a - 5f * b + 4f * c - d) * (u * u)
                           + (-a + c) * u
                           + 2f * b
                       );
        }


        /// <summary>
        /// Calculates the total path length.
        /// <summary>
        public static float GetPathLength(Vector3[] waypoints)
        {
            float dist = 0f;
            for (int i = 0; i < waypoints.Length - 1; i++)
                dist += Vector3.Distance(waypoints[i], waypoints[i + 1]);
            return dist;
        }


        /// <summary>
        /// Smoothes a list of Vector3's based on the number of interpolations. Credits to "Codetastic".
        /// <summary>
        //http://answers.unity3d.com/questions/392606/line-drawing-how-can-i-interpolate-between-points.html
        public static List<Vector3> SmoothCurve(List<Vector3> pathToCurve, int interpolations)
        {
            List<Vector3> tempPoints;
            List<Vector3> curvedPoints;
            int pointsLength = 0;
            int curvedLength = 0;

            if (interpolations < 1)
                interpolations = 1;

            pointsLength = pathToCurve.Count;
            curvedLength = (pointsLength * Mathf.RoundToInt(interpolations)) - 1;
            curvedPoints = new List<Vector3>(curvedLength);

            float t = 0.0f;
            for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
            {
                t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);
                tempPoints = new List<Vector3>(pathToCurve);
                for (int j = pointsLength - 1; j > 0; j--)
                {
                    for (int i = 0; i < j; i++)
                    {
                        tempPoints[i] = (1 - t) * tempPoints[i] + t * tempPoints[i + 1];
                    }
                }
                curvedPoints.Add(tempPoints[0]);
            }

            return curvedPoints;
        }
    }
}