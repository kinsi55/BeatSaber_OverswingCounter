using IPA.Utilities;
using UnityEngine;

namespace OverswingCounter.Models {
	public class DataProcessor : ISaberMovementDataProcessor {
		private static FieldAccessor<SaberMovementData, int>.Accessor SaberMovementData_validCount = FieldAccessor<SaberMovementData, int>.GetAccessor("_validCount");
		private static FieldAccessor<SaberMovementData, BladeMovementDataElement[]>.Accessor SaberMovementData_data = FieldAccessor<SaberMovementData, BladeMovementDataElement[]>.GetAccessor("_data");
		private static FieldAccessor<SaberMovementData, int>.Accessor SaberMovementData_nextAddIndex = FieldAccessor<SaberMovementData, int>.GetAccessor("_nextAddIndex");

		private float _cutTime;
		private bool _notePlaneWasCut;

		private Vector3 _cutPlaneNormal;
		private Vector3 _notePlaneCenter;
		private Vector3 _noteForward;
		private Plane _notePlane;

		public CutInfo CutInfo;

		public DataProcessor(CutInfo cutInfo, Vector3 notePosition, Quaternion noteRotation) {
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
		public void ProcessNewData(BladeMovementDataElement newData, BladeMovementDataElement prevData, bool prevDataAreValid) {
			if(newData.time - _cutTime > 0.4f) {
				CutInfo.Finish();
				return;
			}

			if(!prevDataAreValid)
				return;

			if(!_notePlaneWasCut)
				_notePlane = new Plane(Vector3.Cross(_cutPlaneNormal, _noteForward), _notePlaneCenter);

			if(!_notePlaneWasCut && !_notePlane.SameSide(newData.topPos, prevData.topPos)) {
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
			} else {
				var angle = Vector3.Angle(newData.segmentNormal, _cutPlaneNormal);
				if(angle > 90f) {
					CutInfo.Finish();
					return;
				}

				CutInfo.AfterRating += SaberSwingRating.AfterCutStepRating(newData.segmentAngle, angle);
			}
		}

		// Unclamped version of SaberMovementData::ComputeSwingRating(bool, float)
		public float ComputeSwingRating(bool overrideSegmentAngle = false, float overrideValue = 0f) {
			var instance = CutInfo.SaberMovementData;
			var _validCount = SaberMovementData_validCount(ref instance);
			var _data = SaberMovementData_data(ref instance);

			if(_validCount < 2)
				return 0f;

			var len = _data.Length;
			var idx = SaberMovementData_nextAddIndex(ref instance) - 1;
			if(idx < 0)
				idx += len;

			var time = _data[idx].time;
			var earliestProcessedMovementData = time;
			var rating = 0f;
			var segmentNormal = _data[idx].segmentNormal;
			var angleDiff = overrideSegmentAngle ? overrideValue : _data[idx].segmentAngle;
			var minRequiredMovementData = 2;

			rating += SaberSwingRating.BeforeCutStepRating(angleDiff, 0f);
			CutInfo.EndPos = _data[idx].topPos;
			while(time - earliestProcessedMovementData < 0.4f && minRequiredMovementData < _validCount) {
				idx--;
				if(idx < 0)
					idx += len;
				var elem = _data[idx];

				var segmentNormal2 = elem.segmentNormal;
				angleDiff = elem.segmentAngle;

				var angle = Vector3.Angle(segmentNormal2, segmentNormal);
				if(angle > 90f)
					break;
				CutInfo.StartPos = elem.topPos;

				rating += SaberSwingRating.BeforeCutStepRating(angleDiff, angle);
				earliestProcessedMovementData = elem.time;
				minRequiredMovementData++;
			}

			return rating;
		}
	}
}