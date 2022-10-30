namespace OverswingCounter.Models
{
    public class RollingAverage
    {
        private int _size = 0;
        private int _usedSize = 0;
        private int _currentIndex = 0;
        private float[] _container;
        
        public double Average { get; private set; }
        public double Sum { get; private set; }

        public RollingAverage(int size)
        {
            _size = size;
            _container = new float[size];
        }

        public void Add(float value)
        {
            if (++_currentIndex >= _size)
                _currentIndex = 0;

            Sum -= _container[_currentIndex];
            _container[_currentIndex] = value;
            Sum += value;

            if (_usedSize < _size)
                _usedSize++;

            Average = Sum / _usedSize;
        }
    }
}