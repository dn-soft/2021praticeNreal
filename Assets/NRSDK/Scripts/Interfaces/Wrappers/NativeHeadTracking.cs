﻿/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary> 6-dof Head Tracking's Native API . </summary>
    public class NativeHeadTracking
    {
        /// <summary> The native interface. </summary>
        private NativeInterface m_NativeInterface;
        /// <summary> Gets the handle of the tracking. </summary>
        /// <value> The tracking handle. </value>
        public UInt64 TrackingHandle
        {
            get
            {
                return m_NativeInterface.TrackingHandle;
            }
        }

        /// <summary> Handle of the head tracking. </summary>
        private UInt64 m_HeadTrackingHandle = 0;
        /// <summary> Gets the handle of the head tracking. </summary>
        /// <value> The head tracking handle. </value>
        public UInt64 HeadTrackingHandle
        {
            get
            {
                return m_HeadTrackingHandle;
            }
        }

        /// <summary> Constructor. </summary>
        /// <param name="nativeInterface"> The native interface.</param>
        public NativeHeadTracking(NativeInterface nativeInterface)
        {
            m_NativeInterface = nativeInterface;
        }

        /// <summary> Creates a new bool. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Create()
        {
            if (m_NativeInterface.TrackingHandle == 0)
            {
                return false;
            }
            var result = NativeApi.NRHeadTrackingCreate(m_NativeInterface.TrackingHandle, ref m_HeadTrackingHandle);
            NativeErrorListener.Check(result, this, "Create");
            return result == NativeResult.Success;
        }

        /// <summary> Gets head pose. </summary>
        /// <param name="pose">      [in,out] The pose.</param>
        /// <param name="timestamp"> (Optional) The timestamp.</param>
        /// <param name="predict">   (Optional) The predict.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool GetHeadPose(ref Pose pose, UInt64 timestamp = 0, UInt64 predict = 0)
        {
            if (m_HeadTrackingHandle == 0)
            {
                pose = Pose.identity;
                return false;
            }
            UInt64 headPoseHandle = 0;
            UInt64 hmd_nanos = 0;
            var result = NativeApi.NRTrackingGetHMDTimeNanos(m_NativeInterface.TrackingHandle, ref hmd_nanos);
            if (result != NativeResult.Success)
            {
                return false;
            }
            if (timestamp != 0)
            {
                hmd_nanos = timestamp;
            }
            else if (predict != 0)
            {
                hmd_nanos -= predict;
            }
            else
            {
                UInt64 predict_time = 0;
                NativeApi.NRHeadTrackingGetRecommendPredictTime(m_NativeInterface.TrackingHandle, m_HeadTrackingHandle, ref predict_time);
                hmd_nanos += predict_time;
            }

            var acquireTrackingPoseResult = NativeApi.NRHeadTrackingAcquireTrackingPose(m_NativeInterface.TrackingHandle, m_HeadTrackingHandle, hmd_nanos, ref headPoseHandle);
            if (acquireTrackingPoseResult != NativeResult.Success)
            {
                return false;
            }

            NativeMat4f headpos_native = new NativeMat4f(Matrix4x4.identity);
            var getPoseResult = NativeApi.NRTrackingPoseGetPose(m_NativeInterface.TrackingHandle, headPoseHandle, ref headpos_native);

            if (getPoseResult != NativeResult.Success)
            {
                return false;
            }
            ConversionUtility.ApiPoseToUnityPose(headpos_native, out pose);
            NativeApi.NRTrackingPoseDestroy(m_NativeInterface.TrackingHandle, headPoseHandle);
            return (acquireTrackingPoseResult == NativeResult.Success) && (getPoseResult == NativeResult.Success);
        }

        /// <summary> Gets tracking lost reason. </summary>
        /// <returns> The tracking lost reason. </returns>
        public LostTrackingReason GetTrackingLostReason()
        {
            if (m_HeadTrackingHandle == 0)
            {
                return LostTrackingReason.INITIALIZING;
            }
            LostTrackingReason lost_tracking_reason = LostTrackingReason.INITIALIZING;
            var result = NativeApi.NRTrackingPoseGetTrackingReason(m_NativeInterface.TrackingHandle, m_HeadTrackingHandle, ref lost_tracking_reason);
            NativeErrorListener.Check(result, this, "GetTrackingLostReason");
            return lost_tracking_reason;
        }

        /// <summary> Destroys this object. </summary>
        public void Destroy()
        {
            if (m_HeadTrackingHandle == 0)
            {
                return;
            }
            var result = NativeApi.NRHeadTrackingDestroy(m_NativeInterface.TrackingHandle, m_HeadTrackingHandle);
            m_HeadTrackingHandle = 0;
            NativeErrorListener.Check(result, this, "Destroy");
        }

        private partial struct NativeApi
        {
            /// <summary> Nr head tracking create. </summary>
            /// <param name="tracking_handle">       Handle of the tracking.</param>
            /// <param name="outHeadTrackingHandle"> [in,out] Handle of the out head tracking.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRHeadTrackingCreate(UInt64 tracking_handle,
                ref UInt64 outHeadTrackingHandle);

            /// <summary> Nr tracking get hmd time nanos. </summary>
            /// <param name="tracking_handle">    Handle of the tracking.</param>
            /// <param name="out_hmd_time_nanos"> [in,out] The out hmd time nanos.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRTrackingGetHMDTimeNanos(UInt64 tracking_handle,
                ref UInt64 out_hmd_time_nanos);

            /// <summary> Nr head tracking get recommend predict time. </summary>
            /// <param name="tracking_handle">        Handle of the tracking.</param>
            /// <param name="head_tracking_handle">   Handle of the head tracking.</param>
            /// <param name="out_predict_time_nanos"> [in,out] The out predict time nanos.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRHeadTrackingGetRecommendPredictTime(
                UInt64 tracking_handle, UInt64 head_tracking_handle, ref UInt64 out_predict_time_nanos);

            /// <summary> Nr head tracking acquire tracking pose. </summary>
            /// <param name="sessionHandle">            Handle of the session.</param>
            /// <param name="head_tracking_handle">     Handle of the head tracking.</param>
            /// <param name="hmd_time_nanos">           The hmd time nanos.</param>
            /// <param name="out_tracking_pose_handle"> [in,out] Handle of the out tracking pose.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRHeadTrackingAcquireTrackingPose(UInt64 sessionHandle,
                UInt64 head_tracking_handle, UInt64 hmd_time_nanos, ref UInt64 out_tracking_pose_handle);

            /// <summary> Nr tracking pose get pose. </summary>
            /// <param name="tracking_handle">      Handle of the tracking.</param>
            /// <param name="tracking_pose_handle"> Handle of the tracking pose.</param>
            /// <param name="out_pose">             [in,out] The out pose.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRTrackingPoseGetPose(UInt64 tracking_handle,
                UInt64 tracking_pose_handle, ref NativeMat4f out_pose);

            /// <summary> Nr tracking pose get tracking reason. </summary>
            /// <param name="tracking_handle">      Handle of the tracking.</param>
            /// <param name="tracking_pose_handle"> Handle of the tracking pose.</param>
            /// <param name="out_tracking_reason">  [in,out] The out tracking reason.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRTrackingPoseGetTrackingReason(UInt64 tracking_handle,
                UInt64 tracking_pose_handle, ref LostTrackingReason out_tracking_reason);

            /// <summary> Nr tracking pose destroy. </summary>
            /// <param name="tracking_handle">      Handle of the tracking.</param>
            /// <param name="tracking_pose_handle"> Handle of the tracking pose.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRTrackingPoseDestroy(UInt64 tracking_handle, UInt64 tracking_pose_handle);

            /// <summary> Nr head tracking destroy. </summary>
            /// <param name="tracking_handle">      Handle of the tracking.</param>
            /// <param name="head_tracking_handle"> Handle of the head tracking.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRHeadTrackingDestroy(UInt64 tracking_handle, UInt64 head_tracking_handle);
        };
    }
}
