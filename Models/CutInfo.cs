using System;
using OverswingCounter.Harmony_Patches;
using UnityEngine;

namespace OverswingCounter.Models
{
    public class CutInfo
    {
        public NoteCutInfo NoteCutInfo;
        public Saber Saber;
        public SaberType SaberType;
        public SaberMovementData SaberMovementData => Saber.movementData;
        public DataProcessor DataProcessor;
        public float CutTime;
        
        public float BeforeRating;
        public float AfterRating;
        
        public float Angle => Mathf.Rad2Deg * Mathf.Atan2(EndPos.y - StartPos.y, EndPos.x - StartPos.x);
        public Vector3 StartPos;
        public Vector3 EndPos;
        public bool IsDownswing => Angle is <= 10f or >= 170f;

        public bool IsPrimary { get; private set; } = false;
        public CutInfo LastFinishedCutToCompareAgainst { get; private set; }
        
        public CutInfo(NoteCutInfo noteCutInfo)
        {
            NoteCutInfo = noteCutInfo;
            Saber = Plugin.SaberManager.SaberForType(NoteCutInfo.saberType);
            SaberType = Saber.saberType;
            DataProcessor = new DataProcessor(this, noteCutInfo.notePosition, noteCutInfo.noteRotation);
            CutTime = Time.realtimeSinceStartup;

            LastFinishedCutToCompareAgainst = CutHandler.LastFinishedCut[SaberType];
            IsPrimary = CutHandler.CurrentPrimaryCut[SaberType] == null;
            if (IsPrimary)
                CutHandler.CurrentPrimaryCut[SaberType] = this;
        }

        public void Finish()
        {
            SaberMovementData.RemoveDataProcessor(DataProcessor);
            CutHandler.ActiveCutInfos.Remove(this);

#if DEBUG
            Plugin.Log.Info($"Finished: {BeforeRating:P2}pre {AfterRating:P2}post {Angle:F1}deg");
#endif

            if (AfterRating < 1f)
            {
                var corrected = (float)Math.Round(AfterRating * SaberSwingRating.kAfterCutAngleFor1Rating) / SaberSwingRating.kAfterCutAngleFor1Rating;
                if (corrected > AfterRating) AfterRating = corrected;
            }
            
            if (BeforeRating < 1f)
            {
                var corrected = (float)Math.Round(BeforeRating * SaberSwingRating.kBeforeCutAngleFor1Rating) / SaberSwingRating.kBeforeCutAngleFor1Rating;
                if (corrected > BeforeRating) BeforeRating = corrected;
            }

            CutHandler.LastFinishedCut[SaberType] = this;
            if (CutHandler.CurrentPrimaryCut[SaberType] == this)
                CutHandler.CurrentPrimaryCut[SaberType] = null;
            
            if (CutHandler.NewCutCompleted != null)
                CutHandler.NewCutCompleted(this);
        }
    }
}