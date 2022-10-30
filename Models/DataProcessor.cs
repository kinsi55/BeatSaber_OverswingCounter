using IPA.Utilities;
using UnityEngine;

namespace OverswingCounter.Models
{
    public class DataProcessor : ISaberMovementDataProcessor
    {
        private static FieldAccessor<SaberMovementData, int>.Accessor _validCount = FieldAccessor<SaberMovementData, int>.GetAccessor("_validCount");
        private static FieldAccessor<SaberMovementData, BladeMovementDataElement[]>.Accessor _data = FieldAccessor<SaberMovementData, BladeMovementDataElement[]>.GetAccessor("_data");
        private static FieldAccessor<SaberMovementData, int>.Accessor _nextAddIndex = FieldAccessor<SaberMovementData, int>.GetAccessor("_nextAddIndex");

        private float _cutTime;
        private bool _notePlaneWasCut;
        
        private Vector3 _cutPlaneNormal;
        private Vector3 _notePlaneCenter;
        private Vector3 _noteForward;
        private Plane _notePlane;

        public CutInfo CutInfo;

        public DataProcessor(CutInfo cutInfo, Vector3 notePosition, Quaternion noteRotation)
        {
            CutInfo = cutInfo;

            var lastAddedData = cutInfo.SaberMovementData.lastAddedData;
            _cutTime = lastAddedData.time;
            _cutPlaneNormal = lastAddedData.segmentNormal;
            _notePlaneCenter = notePosition;
            _notePlane = new Plane(noteRotation * Vector3.up, _notePlaneCenter);
            _noteForward = noteRotation * Vector3.forward;

            CutInfo.BeforeRating = ComputeSwingRating();
            
            CutInfo.SaberMovementData.AddDataProcessor(this);
        }
        
        // Stripped down, unclamped version of SaberSwingRatingCounter::ProcessNewData
        public void ProcessNewData(BladeMovementDataElement newData, BladeMovementDataElement prevData, bool prevDataAreValid)
        {
            if (newData.time - _cutTime > 0.4f)
            {
                CutInfo.Finish();
                return;
            }

            if (!prevDataAreValid) 
                return;

            if (!_notePlaneWasCut)
                _notePlane = new Plane(Vector3.Cross(_cutPlaneNormal, _noteForward), _notePlaneCenter);

            if (!_notePlaneWasCut && !_notePlane.SameSide(newData.topPos, prevData.topPos))
            {
                var beforeCutTopPos = prevData.topPos;
                var beforeCutBottomPos = prevData.bottomPos;
                var afterCutTopPos = newData.topPos;
                var afterCutBottomPos = newData.bottomPos;
                var ray = new Ray(beforeCutTopPos, afterCutTopPos - beforeCutTopPos);

                _notePlane.Raycast(ray, out var distance);
                var cutTopPos = ray.GetPoint(distance);
                var cutBottomPos = (beforeCutBottomPos + afterCutBottomPos) * 0.5f;

                var overrideSegmentAngle = Vector3.Angle(cutTopPos - cutBottomPos, beforeCutTopPos - beforeCutBottomPos);
                var angleDiff = Vector3.Angle(cutTopPos - cutBottomPos, afterCutTopPos - afterCutBottomPos);
                _cutTime = newData.time;
                _notePlaneWasCut = true;

                CutInfo.BeforeRating = ComputeSwingRating(true, overrideSegmentAngle);
                CutInfo.AfterRating = SaberSwingRating.AfterCutStepRating(angleDiff, 0f);
            }
            else
            {
                var angle = Vector3.Angle(newData.segmentNormal, _cutPlaneNormal);
                if (angle > 90f)
                {
                    CutInfo.Finish();
                    return;
                }

                CutInfo.AfterRating += SaberSwingRating.AfterCutStepRating(newData.segmentAngle, angle);
            }
        }

        // Unclamped version of SaberMovementData::ComputeSwingRating(bool, float)
        public float ComputeSwingRating(bool overrideSegmentAngle = false, float overrideValue = 0f)
        {
            var instance = CutInfo.SaberMovementData;
            if (_validCount(ref instance) < 2)
                return 0f;

            var len = _data(ref instance).Length;
            var idx = _nextAddIndex(ref instance) - 1;
            if (idx < 0) idx += len;

            var time = _data(ref instance)[idx].time;
            var num3 = time; // no idea what to name this
            var rating = 0f;
            var segmentNormal = _data(ref instance)[idx].segmentNormal;
            var angleDiff = overrideSegmentAngle ? overrideValue : _data(ref instance)[idx].segmentAngle;
            var num5 = 2; // same here

            rating += SaberSwingRating.BeforeCutStepRating(angleDiff, 0f);
            CutInfo.EndPos = _data(ref instance)[idx].topPos;
            while (time - num3 < 0.4f && num5 < _validCount(ref instance))
            {
                idx--;
                if (idx < 0) idx += len;
                var elem = _data(ref instance)[idx];
                
                var segmentNormal2 = elem.segmentNormal;
                angleDiff = elem.segmentAngle;

                var angle = Vector3.Angle(segmentNormal2, segmentNormal);
                if (angle > 90f) break;
                CutInfo.StartPos = elem.topPos;

                rating += SaberSwingRating.BeforeCutStepRating(angleDiff, angle);
                num3 = elem.time;
                num5++;
            }

            return rating;
        }
    }
}