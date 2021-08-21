using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverswingCounter {
	class RollingAverage {
		int size = 0;

		int usedSize = 0;
		int currentIndex = 0;

		float[] container;
		public double average { get; private set; }
		public double sum { get; private set; }


		public RollingAverage(int size) {
			this.size = size;
			container = new float[size];
		}

		public void Add(float value) {
			if(++currentIndex >= size)
				currentIndex = 0;

			sum -= container[currentIndex];

			container[currentIndex] = value;

			sum += value;

			if(usedSize < size)
				usedSize++;

			average = sum / usedSize;
		}
	}
}
