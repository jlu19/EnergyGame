﻿/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SWS
{
    /// <summary>
    /// Stores waypoints of a bezier path, accessed by walker objects.
    /// Provides gizmo visualization in the editor.
    /// <summary>
    public class BezierPathManager : PathManager
    {
        /// <summary>
        /// Path points (not waypoints) creating the path.
        /// <summary>
        public Vector3[] pathPoints;

        /// <summary>
        /// List to store bezier points for this path.
        /// <summary>
        public List<BezierPoint> bPoints;

        /// <summary>
        /// Toggles drawing of control point handles.
        /// <summary>
        public bool showHandles = true;

        /// <summary>
        /// Gizmo color for control point handles.
        /// <summary>
        public Color color3 = new Color(108 / 255f, 151 / 255f, 1, 1);

        /// <summary>
        /// Detail value for interpolations when calculating path points.
        /// <summary>
        public float pathDetail = 1;

        /// <summary>
        /// Toggles custom detail for single path segments.
        /// <summary>
        public bool customDetail = false;

        /// <summary>
        /// List of detail values for single path segments, when enabled.
        /// <summary>
        public List<float> segmentDetail = new List<float>();


        //recalculate path points
        void Awake()
        {
            CalculatePath();
        }


        //editor visualization
        void OnDrawGizmos()
        {
            if (bPoints.Count <= 0) return;

            //assign path ends color
            Vector3 start = bPoints[0].wp.position;
            Vector3 end = bPoints[bPoints.Count - 1].wp.position;
            Gizmos.color = color1;
            Gizmos.DrawWireCube(start, size * GetHandleSize(start));
            Gizmos.DrawWireCube(end, size * GetHandleSize(end));

            //assign line and waypoints color
            Gizmos.color = color2;
            for (int i = 1; i < bPoints.Count - 1; i++)
                Gizmos.DrawWireSphere(bPoints[i].wp.position, radius * GetHandleSize(bPoints[i].wp.position));

            //draw linear or curved lines with the same color
            if (drawCurved && bPoints.Count >= 2)
                WaypointManager.DrawCurved(pathPoints);
            else
                WaypointManager.DrawStraight(pathPoints);
        }


        //helper method to get screen based handle sizes
        private float GetHandleSize(Vector3 pos)
        {
            float handleSize = 1f;
            #if UNITY_EDITOR
                handleSize = UnityEditor.HandleUtility.GetHandleSize(pos) * 0.5f;
            #endif
            return handleSize;
        }


        /// <summary>
        /// Returns waypoint positions (path positions) as Vector3 array.
        /// <summary>
        public override Vector3[] GetPathPoints(bool local = false)
        {
            return pathPoints;
        }


        /// <summary>
        /// Recalculates final path points. Can be called at runtime to allow repositioning,
        /// but do consider update rate vs. performance!
        /// <summary>
        public void CalculatePath()
        {
            //temporary list for final points
            List<Vector3> temp = new List<Vector3>();
            //loop over bezier points (segments)
            for (int i = 0; i < bPoints.Count - 1; i++)
            {
                BezierPoint bp = bPoints[i];
                //get path detail value (default or custom)
                float detail = pathDetail;
                if (customDetail)
                    detail = segmentDetail[i];
                //calculate path points on this segment. Parameters:
                //current waypoint position, current right handle position,
                //next left handle position, next waypoint position, path detail
                temp.AddRange(GetPoints(bp.wp.position,
                                bp.cp[1].position,
                                bPoints[i + 1].cp[0].position,
                                bPoints[i + 1].wp.position,
                                detail));
            }
            //remove duplicates after calculation
            pathPoints = temp.Distinct().ToArray();
        }


        //returns all path points on a segment, see CalculatePath() for parameters
        private List<Vector3> GetPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float detail)
        {
            //temporary list for final points on this segment
            List<Vector3> segmentPoints = new List<Vector3>();
            //multiply detail value to have at least 5-10+ iterations
            float iterations = detail * 10f;
            for (int n = 0; n <= iterations; n++)
            {
                //cannot increment i as a float
                float i = (float)n / iterations;
                float rest = (1f - i);
                //bezier formula
                Vector3 newPos = Vector3.zero;
                newPos += p0 * rest * rest * rest;
                newPos += p1 * i * 3f * rest * rest;
                newPos += p2 * 3f * i * i * rest;
                newPos += p3 * i * i * i;
                //add calculated point to segment
                segmentPoints.Add(newPos);
            }
            //return points on this segment
            return segmentPoints;
        }
    }


    /// <summary>
    /// Custom class to store waypoint transforms and control points for bezier paths.
    /// <summary>
    [System.Serializable]
    public class BezierPoint
    {
        /// <summary>
        /// Waypoint transform for this bezier point.
        /// <summary>
        public Transform wp = null;

        /// <summary>
        /// Control points for this bezier point.
        /// [0] = first point (left), [1] = second point (right)
        /// <summary>
        public Transform[] cp = new Transform[2];
    }
}